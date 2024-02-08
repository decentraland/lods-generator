import { Message } from '@aws-sdk/client-sqs'
import { randomUUID } from 'node:crypto'

import { QueueComponent, QueueMessage } from '../types'

export function createMemoryQueueAdapter(): QueueComponent {
  const queue: Map<string, Message> = new Map()

  async function send(message: QueueMessage): Promise<void> {
    const receiptHandle = randomUUID().toString()
    queue.set(receiptHandle, {
      MessageId: randomUUID().toString(),
      ReceiptHandle: receiptHandle,
      Body: JSON.stringify({ Message: JSON.stringify(message) })
    })

    return
  }

  async function receiveSingleMessage(): Promise<Message[]> {
    return queue.size > 0 ? [queue.values().next().value] : []
  }

  async function deleteMessage(receiptHandle: string): Promise<void> {
    queue.delete(receiptHandle)
  }

  return { send, receiveSingleMessage, deleteMessage }
}
