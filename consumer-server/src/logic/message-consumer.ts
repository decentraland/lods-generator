import { AppComponents, QueueWorker } from '../types'
import { sleep } from '../utils/timer'

export async function createMessagesConsumerComponent({
  logs,
  queue,
  messageHandler
}: Pick<AppComponents, 'logs' | 'queue' | 'messageHandler'>): Promise<QueueWorker> {
  const logger = logs.getLogger('messages-consumer')
  let isRunning = false

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
        try {
          const parsedMessage: { Message: string } = JSON.parse(Body!)
          logger.debug('Message received', {
            id: MessageId!,
            message: parsedMessage.Message
          })
          await messageHandler.handle(JSON.parse(parsedMessage.Message))
        } catch (error: any) {
          logger.error('Failed while handling message from queue', {
            id: MessageId!,
            error
          })
        } finally {
          await queue.deleteMessage(ReceiptHandle!)
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
