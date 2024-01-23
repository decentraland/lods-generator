import {
  DeleteMessageCommand,
  Message,
  ReceiveMessageCommand,
  SQSClient,
  SendMessageCommand
} from '@aws-sdk/client-sqs'

import { AppComponents, QueueMessage, QueueService } from '../types'

export async function createSqsAdapter({
  config
}: Pick<AppComponents, | 'config'>): Promise<QueueService> {
  const endpoint = await config.getString('QUEUE_URL')
  const client = new SQSClient({ endpoint })

  async function send(message: QueueMessage): Promise<void> {
    const sendCommand = new SendMessageCommand({
      QueueUrl: endpoint,
      MessageBody: JSON.stringify(message)
    })
    await client.send(sendCommand)
  }

  async function receiveSingleMessage(): Promise<Message[]> {
    const receiveCommand = new ReceiveMessageCommand({
      QueueUrl: endpoint,
      MaxNumberOfMessages: 1,
      WaitTimeSeconds: 15
    })
    const { Messages = [] } = await client.send(receiveCommand)

    return Messages
  }

  async function deleteMessage(receiptHandle: string) {
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
