import { AsyncQueue } from '@well-known-components/pushable-channel'
import { AppComponents, QueueComponent } from '../types'

export function createMemoryQueueAdapter({ logs }: Pick<AppComponents, 'logs'>): QueueComponent {
  const logger = logs.getLogger('memory-queue')
  const queue = new AsyncQueue((_) => void 0)

  logger.info('Initializing memory queue adapter')

  async function send(message: any) {
    queue.enqueue(message)
  }

  async function receiveSingleMessage() {
    const message = (await queue.next()).value
    return message ? [message] : []
  }

  async function deleteMessage() {
    // noop
  }

  return {
    send,
    receiveSingleMessage,
    deleteMessage
  }
}
