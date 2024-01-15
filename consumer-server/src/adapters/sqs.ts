import {
  DeleteMessageCommand,
  Message,
  ReceiveMessageCommand,
  SQSClient,
  SendMessageCommand
} from '@aws-sdk/client-sqs'

import { AppComponents, QueueMessage, QueueService } from '../types'

export async function createSqsAdapter({
  awsConfig
}: Pick<AppComponents, 'awsConfig' | 'logs'>): Promise<QueueService> {
  const client = new SQSClient({ endpoint: awsConfig!.sqsUrl })

  async function send(message: QueueMessage): Promise<void> {
    const sendCommand = new SendMessageCommand({
      QueueUrl: awsConfig!.sqsUrl,
      MessageBody: JSON.stringify(message)
    })
    await client.send(sendCommand)
  }

  async function receiveSingleMessage(): Promise<Message[]> {
    const receiveCommand = new ReceiveMessageCommand({
      QueueUrl: awsConfig!.sqsUrl,
      MaxNumberOfMessages: 1,
      WaitTimeSeconds: 15
    })
    const { Messages = [] } = await client.send(receiveCommand)

    return Messages
  }

  async function deleteMessage(receiptHandle: string) {
    const deleteCommand = new DeleteMessageCommand({
      QueueUrl: awsConfig!.sqsUrl,
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
