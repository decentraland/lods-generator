import fs from 'fs'

import { AppComponents, MessageProcessorComponent, QueueMessage } from '../types'
import { sleep } from '../utils/timer'

export async function createMessageProcesorComponent({
  logs,
  config,
  queue,
  lodGenerator,
  storage,
  bundleTriggerer
}: Pick<
  AppComponents,
  'logs' | 'config' | 'queue' | 'lodGenerator' | 'storage' | 'bundleTriggerer'
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

  async function process(message: QueueMessage, receiptMessageHandle: string): Promise<void> {
    const retry = message._retry || 0
    let outputPath: string | undefined
    try {
      if (message.entity.entityType !== 'scene') {
        logger.debug(`Entity is not a scene, will not be processed`, {
          entityType: message.entity.entityType,
          entityId: message.entity.entityId
        })
        await queue.deleteMessage(receiptMessageHandle)
        return
      }

      const entityId = message.entity.entityId
      const base = message.entity.metadata.scene.base
      logger.info('Processing scene deployment', {
        entityId,
        base
      })

      const timeoutInMinutes = (retry + 1) * 20
      const lodGenerationResult = await lodGenerator.generate(base, timeoutInMinutes)
      outputPath = lodGenerationResult.outputPath

      if (lodGenerationResult.error) {
        logger.error('Error while generating LOD', {
          entityId,
          base,
          error: lodGenerationResult?.error?.message.replace(/\n|\r\n/g, ' ') || 'Check log bucket for more details'
        })

        if (lodGenerationResult.error.message.toLowerCase().includes('license')) {
          logger.warn('License server error detected, it will not recover itself. Manual action is required.')
          logger.info('Retrying message in 1 minute.')
          await sleep(60 * 1000)
          return
        }

        if (retry < 3) {
          await reQueue(message)
        } else {
          logger.warn('Max attempts reached, moving to error bucket', {
            entityId,
            base,
            attempt: retry + 1
          })
          await storage.storeFiles([lodGenerationResult.logFile], `failures/${base}`)
        }

        await queue.deleteMessage(receiptMessageHandle)

        return
      }

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
    } finally {
      if (outputPath && fs.existsSync(outputPath)) {
        fs.rmSync(outputPath, { recursive: true, force: true })
      }
    }
  }

  return { process }
}
