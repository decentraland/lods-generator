import fs from 'fs'

import { AppComponents, LodGenerationResult, QueueWorker } from '../types'
import { sleep } from '../utils/timer'

export async function createMessagesConsumerComponent({
  logs,
  queue,
  lodGenerator,
  storage
}: Pick<AppComponents, 'logs' | 'queue' | 'lodGenerator' | 'storage'>): Promise<QueueWorker> {
  const logger = logs.getLogger('messages-consumer')
  let isRunning = false

  async function removeMessageFromQueue(messageHandle: string, entityId: string) {
    logger.info('Removing message from queue', { messageHandle, entityId })
    await queue.deleteMessage(messageHandle)
  }

  async function start() {
    logger.info('Starting to listen messages from queue')
    isRunning = true
    while (isRunning) {
      const messages = await queue.receiveSingleMessage()

      if (messages.length === 0) {
        await sleep(20 * 1000)
        continue
      }

      for (const message of messages) {
        const { MessageId, Body, ReceiptHandle } = message
        let parsedMessage = undefined

        try {
          parsedMessage = JSON.parse(JSON.parse(Body!).Message)
        } catch (error: any) {
          logger.error('Failed while parsing message from queue', {
            messageHandle: ReceiptHandle!,
            error: error?.message || 'Unexpected failure'
          })
          await removeMessageFromQueue(ReceiptHandle!, 'unknown')
          continue
        }

        try {
          if (parsedMessage.entity.entityType !== 'scene') {
            logger.debug(`Message received but it does not correspond to a scene and will not be processed`, {
              entityType: parsedMessage.entity.entityType,
              entityId: parsedMessage.entity.entityId
            })
            continue
          }

          const entityId = parsedMessage.entity.entityId
          const base = parsedMessage.entity.metadata.scene.base
          logger.info('Processing scene deployment', {
            entityId,
            messageHandle: ReceiptHandle!,
            message: JSON.stringify(message)
          })

          const result: LodGenerationResult = await lodGenerator.generate(base)

          if (result.error) {
            logger.error('LOD generation failed', {
              entityId,
              messageHandle: ReceiptHandle!,
              error: result.error.message?.replace(/\n|\r\n/g, '') || 'Unexpected failure'
            })
            await storage.storeFiles([result.logFile], `Failures/${entityId}`)
            continue
          }

          await storage.storeFiles(
            result.lodsFiles,
            `${base}/LOD/Sources/${parsedMessage.entity.entityTimestamp.toString()}`
          )
          
          fs.rmSync(result.outputPath, { recursive: true, force: true })
        } catch (error: any) {
          logger.error('Failed while handling message from queue', {
            messageHandle: ReceiptHandle!,
            entityId: parsedMessage?.entity?.entityId || 'unknown',
            error: error.message
          })
        } finally {
          logger.info('Message processed, removing it from the queue', {
            entityId: parsedMessage.entity.entityId,
            id: MessageId!
          })
          await removeMessageFromQueue(ReceiptHandle!, parsedMessage.entity.entityId)
        }
      }
    }
  }

  async function stop() {
    isRunning = false
  }

  return {
    start,
    stop
  }
}
