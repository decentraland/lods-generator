import fs from 'fs'

import { AppComponents, HealthState, MessageProcessorComponent, QueueMessage } from '../types'
import { sleep } from '../utils/timer'

export async function createMessageProcesorComponent({
  logs,
  config,
  metrics,
  queue,
  lodGenerator,
  storage,
  bundleTriggerer
}: Pick<
  AppComponents,
  'logs' | 'config' | 'metrics' | 'queue' | 'lodGenerator' | 'storage' | 'bundleTriggerer'
>): Promise<MessageProcessorComponent> {
  const logger = logs.getLogger('message-procesor')
  const abServers = (await config.requireString('AB_SERVERS')).split(';')

  async function reQueue(message: QueueMessage): Promise<void> {    
    const retry = (message._retry || 0) + 1
    logger.info('Re-queuing message', {
      entityId: message.entity.entityId,
      base: message.entity.metadata.scene.base,
      retry
    })
    await queue.send({
      ...message,
      _retry: retry
    })

    return
  }

  function isInvalid(message: QueueMessage): boolean {
    return message.entity.entityType !== 'scene' && !message.entity.metadata?.scene?.base && !message.entity.entityId
  }

  async function process(message: QueueMessage, receiptMessageHandle: string): Promise<void> {
    const retry = message._retry || 0
    let outputPath: string | undefined
    try {
      if (isInvalid(message)) {
        logger.debug(`Discarding message since it is not a valid scene`, {
          message: JSON.stringify(message)
        })
        await queue.deleteMessage(receiptMessageHandle)
        return
      }

      
      const entityId = message.entity.entityId
      const base = message.entity.metadata.scene.base
      logger.info('Processing scene deployment', {
        entityId,
        base,
        attempt: retry + 1
      })
      
      const timeoutInMinutes = (retry + 1) * 20
      const generationProcessStartTime = Date.now()
      const lodGenerationResult = await lodGenerator.generate(base, timeoutInMinutes)
      const generationProcessDuration = Date.now() - generationProcessStartTime
      outputPath = lodGenerationResult.outputPath

      if (lodGenerationResult.error) {
        if (lodGenerationResult.error.message.toLowerCase().includes('license')) {
          logger.warn('License server error detected, it will not recover itself. Manual action is required.')
          logger.info('Retrying message in 1 minute.')
          metrics.observe('license_server_health', {}, HealthState.Unhealthy)
          await sleep(60 * 1000)
          return
        }

        metrics.observe('license_server_health', {}, HealthState.Healthy)

        logger.error('Error while generating LOD', {
          entityId,
          base,
          error: lodGenerationResult?.error?.message.replace(/\n|\r\n/g, ' ') || 'Check log bucket for more details'
        })

        if (retry < 3) {
          await reQueue(message)
          metrics.increment('lod_generation_count', { status: 'retryable' }, 1)
        } else {
          logger.warn('Max attempts reached, moving to error bucket', {
            entityId,
            base,
            attempt: retry + 1
          })
          await storage.storeFiles([lodGenerationResult.logFile], `failures/${base}`)
          metrics.increment('lod_generation_count', { status: 'failed' }, 1)
        }

        await queue.deleteMessage(receiptMessageHandle)

        return
      }

      metrics.observe('license_server_health', {}, HealthState.Healthy)
      metrics.observe('lod_generation_duration_minutes', {}, generationProcessDuration / 1000 / 60)
      logger.info('Uploading files to bucket', {
        entityId,
        base,
        files: lodGenerationResult.lodsFiles.map((filePath) => filePath.split('/').pop()).join(', ')
      })

      const uploadedFiles = await storage.storeFiles(
        lodGenerationResult.lodsFiles,
        `${base}/LOD/Sources/${message.entity.entityTimestamp.toString()}`
      )

      logger.info('Publishing message to AssetBundle converter', { entityId, base })
      await Promise.all(abServers.map((abServer) => bundleTriggerer.queueGeneration(entityId, uploadedFiles, abServer)))
      await queue.deleteMessage(receiptMessageHandle)
      metrics.increment('lod_generation_count', { status: 'succeed' }, 1)
    } catch (error: any) {
      logger.error('Unexpected failure while handling message from queue', {
        entityId: message.entity.entityId,
        base: message.entity.metadata.scene.base,
        attempt: retry + 1,
        error: error.message
      })
      if (retry < 3) {
        await reQueue(message)
      }
      await queue.deleteMessage(receiptMessageHandle)
      metrics.increment('lod_generation_count', { status: 'failed' }, 1)
    } finally {
      if (outputPath && fs.existsSync(outputPath)) {
        fs.rmSync(outputPath, { recursive: true, force: true })
      }
    }
  }

  return { process }
}
