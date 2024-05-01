import {
  DeleteMessageCommand,
  Message,
  ReceiveMessageCommand,
  SQSClient,
  SendMessageCommand,
  SetQueueAttributesCommand
} from '@aws-sdk/client-sqs'

import { QueueMessage, QueueComponent } from '../types'

export async function createSqsAdapter(endpoint: string): Promise<QueueComponent> {
  const client = new SQSClient({ endpoint })

  // ensure 14 days of retention
  await client.send(
    new SetQueueAttributesCommand({
      QueueUrl: endpoint,
      Attributes: {
        MessageRetentionPeriod: '1209600'
      }
    })
  )

  async function send(message: QueueMessage): Promise<void> {
    const sendCommand = new SendMessageCommand({
      QueueUrl: endpoint,
      MessageBody: JSON.stringify({ Message: JSON.stringify(message) })
    })
    await client.send(sendCommand)
  }

  async function receiveSingleMessage(): Promise<Message[]> {
    const receiveCommand = new ReceiveMessageCommand({
      QueueUrl: endpoint,
      MaxNumberOfMessages: 1,
      VisibilityTimeout: 3600 // 1 hour
    })
    const { Messages = [] } = await client.send(receiveCommand)

    return Messages
  }

  async function deleteMessage(receiptHandle: string): Promise<void> {
    const deleteCommand = new DeleteMessageCommand({
      QueueUrl: endpoint,
      ReceiptHandle: receiptHandle
    })
    await client.send(deleteCommand)
  }

  return {
    send,
    receiveSingleMessage,
    deleteMessage
  }
}
