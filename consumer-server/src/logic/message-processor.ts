import fs from 'fs'

import { AppComponents, HealthState, MessageProcessorComponent, QueueMessage } from '../types'
import { parseMultilineText } from '../utils/text-parser'
import RoadCoordinates from '../../../SingleParcelRoadCoordinates.json'

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

  function isRelatedToAssetBundlePublish(errorMessage: string | undefined): boolean {
    return !!errorMessage && abServers.some((abServer) => errorMessage.includes(abServer))
  }

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
    return message.entity.entityType !== 'scene' || !message.entity.metadata?.scene?.base || !message.entity.entityId
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
      if (RoadCoordinates.includes(base)) {
        logger.debug('Skipping process since it is a road', {
          entityId,
          base
        })
        await queue.deleteMessage(receiptMessageHandle)
        return
      }

      const alreadyUploadedFiles = await storage.getFiles(`${base}/LOD/Sources/${message.entity.entityTimestamp.toString()}`)
      
      if (!!alreadyUploadedFiles.length) {
          const lastUploadDate = alreadyUploadedFiles.reduce((acc, file) => {
            if (!file.lastModified) return acc
            return file.lastModified > acc ? file.lastModified : acc
          }, new Date(0))

          const currentDate = new Date()
          const diff = currentDate.getTime() - lastUploadDate.getTime()
          const diffDays = diff / (1000 * 3600 * 24)
          if (diffDays < 3) {
            logger.debug('Skipping process since it was already processed within the last 3 days', {
              entityId,
              base,
              lastUploadDate: lastUploadDate.toISOString(),
              currentDate: currentDate.toISOString()
            })
            await queue.deleteMessage(receiptMessageHandle)
            return
          }
      }

      logger.info('Processing scene deployment', {
        entityId,
        base,
        attempt: retry + 1
      })

      const generationProcessStartTime = Date.now()
      const lodGenerationResult = await lodGenerator.generate(base)
      const generationProcessDuration = Date.now() - generationProcessStartTime
      outputPath = lodGenerationResult.outputPath

      if (lodGenerationResult.error) {
        logger.warn('Error while generating LOD', {
          entityId,
          base,
          error: lodGenerationResult.error?.message
            ? parseMultilineText(lodGenerationResult.error?.message)
            : 'Check log bucket for more details',
          detailedError: lodGenerationResult.error?.detailedError
            ? parseMultilineText(lodGenerationResult.error?.detailedError)
            : 'No details found'
        })

        if (retry < 3) {
          await reQueue(message)
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

      metrics.observe('license_server_health', {}, HealthState.Unused)
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
      await storage.deleteFailureDirectory(base)
    } catch (error: any) {
      logger.error('Unexpected failure while handling message from queue', {
        entityId: message.entity.entityId,
        base: message.entity.metadata.scene.base,
        attempt: retry + 1,
        error: error.message
      })

      if (isRelatedToAssetBundlePublish(error?.message)) {
        await reQueue(message)
      } else {
        if (retry < 3) {
          await reQueue(message)
        } else {
          logger.warn('Max attempts reached, message will not be retried', {
            entityId: message.entity.entityId,
            base: message.entity.metadata.scene.base,
            attempt: retry
          })
          metrics.increment('lod_generation_count', { status: 'failed' }, 1)
        }
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
