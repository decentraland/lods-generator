import fs from 'fs'

import { AppComponents, MessageProcessorComponent, QueueMessage } from '../types'

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
    const currentAttempt = (message._retry || 0) + 1
    logger.info('Re-queuing message', {
      entityId: message.entity.entityId,
      base: message.entity.metadata.scene.base,
      currentAttempt
    })
    await queue.send({
      ...message,
      /* if it is the first try:
            /* currentAttempt = 1 
            /* which is the value desired for the _retry property on next iteration
            /* (meaning that it will be the first retry) */
      _retry: currentAttempt
    })

    return
  }

  async function process(message: QueueMessage, receiptMessageHandle: string): Promise<void> {
    const retry = (message._retry || 0)
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

      const lodGenerationResult = await lodGenerator.generate(base)
      outputPath = lodGenerationResult.outputPath

      if (lodGenerationResult.error) {
        logger.error('Error while generating LOD', {
          entityId,
          base,
          error: lodGenerationResult?.error?.message.replace(/\n|\r\n/g, '') || 'Check log bucket for more details'
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
