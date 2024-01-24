import { AppComponents, QueueWorker } from '../types'

export async function createMessagesConsumerComponent({
  logs,
  queue
}: Pick<AppComponents, 'logs' | 'queue'>): Promise<QueueWorker> {
  const logger = logs.getLogger('messages-consumer')

  async function start() {
    logger.info('Starting to listen messages from queue')
    while (true) {
      const messages = await queue.receiveSingleMessage()
      for (const message of messages) {
        const { MessageId, Body, ReceiptHandle } = message

        try {
          const parsedMessage: { Message: string } = JSON.parse(Body!)
          logger.info('Handling message from queue', {
            id: MessageId!,
            message: parsedMessage.Message
          })
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

  return {
    start
  }
}
