import { Message } from '@aws-sdk/client-sqs'
import { randomUUID } from 'node:crypto'
import { EventEmitter } from 'events'

import { QueueComponent, QueueMessage } from '../types'

export function createMemoryQueueAdapter(eventEmitter: EventEmitter = new EventEmitter()): QueueComponent {
  const queue: Map<string, Message> = new Map()

  async function send(message: QueueMessage): Promise<string> {
    const receiptHandle = randomUUID().toString()
    queue.set(receiptHandle, {
      MessageId: randomUUID().toString(),
      ReceiptHandle: receiptHandle,
      Body: JSON.stringify({ Message: JSON.stringify(message) })
    })

    return receiptHandle
  }

  async function receiveSingleMessage(): Promise<Message[]> {
    return queue.size > 0 ? [queue.values().next().value] : []
  }

  async function deleteMessage(receiptHandle: string): Promise<void> {
    console.log('Check')
    queue.delete(receiptHandle)
    console.log('Check 2', { eventEmitter })
    eventEmitter.emit('messageDeleted', receiptHandle)
    console.log('Check 3')
  }

  return { send, receiveSingleMessage, deleteMessage }
}
