import {
  ChangeMessageVisibilityCommand,
  DeleteMessageCommand,
  Message,
  ReceiveMessageCommand,
  SQSClient,
  SendMessageCommand,
  SetQueueAttributesCommand
} from '@aws-sdk/client-sqs'

import { QueueMessage, QueueComponent } from '../types'
import { Events } from '@dcl/schemas'

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
      MessageBody: JSON.stringify({ Message: JSON.stringify(message) }),
      MessageAttributes: {
        type: {
          DataType: 'String',
          StringValue: Events.Type.CATALYST_DEPLOYMENT
        },
        subType: {
          DataType: 'String',
          StringValue: message.entity.entityType
        },
      }
    })
    await client.send(sendCommand)
  }

  async function receiveSingleMessage(): Promise<Message[]> {
    const receiveCommand = new ReceiveMessageCommand({
      QueueUrl: endpoint,
      MaxNumberOfMessages: 1,
      VisibilityTimeout: 7200 // 2 hours: allow long-processing time
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

  async function increaseMessageVisibility(receiptHandle: string): Promise<void> {
    await client.send(
      new ChangeMessageVisibilityCommand({
        QueueUrl: endpoint,
        ReceiptHandle: receiptHandle,
        VisibilityTimeout: 120
      })
    )
  }

  return {
    send,
    receiveSingleMessage,
    deleteMessage,
    increaseMessageVisibility
  }
}
