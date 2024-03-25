import { AppComponents, QueueWorker } from '../types'
import { sleep } from '../utils/timer'

export async function createMessagesConsumerComponent({
  logs,
  queue,
  messageProcessor
}: Pick<AppComponents, 'logs' | 'queue' | 'messageProcessor'>): Promise<QueueWorker> {
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
        logger.info('No messages found in queue, waiting 20 seconds to check again')
        await sleep(20 * 1000)
        continue
      }

      for (const message of messages) {
        const { Body, ReceiptHandle } = message
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

        await messageProcessor.process(parsedMessage, ReceiptHandle!)
        // sleep 3 seconds to release license server
        await sleep(3000)
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
