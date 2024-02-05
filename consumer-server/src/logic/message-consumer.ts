import { AppComponents, QueueWorker } from '../types'

export async function createMessagesConsumerComponent({
  logs,
  queue,
  messageHandler
}: Pick<AppComponents, 'logs' | 'queue' | 'messageHandler'>): Promise<QueueWorker> {
  const logger = logs.getLogger('messages-consumer')

  async function start() {
    logger.info('Starting to listen messages from queue')
    while (true) {
      const messages = await queue.receiveSingleMessage()
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
          console.log({ error })
          logger.error('Failed while handling message from queue', {
            id: MessageId!
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
