import { SQS } from 'aws-sdk'

import { AppComponents } from '../types'
import { sleep, timeout } from '../utils/timer'

export interface TaskQueueMessage {
  id: string
}

export interface ITaskQueue<T> {
  /**
   * Pulls a message from a queue and executes the taskRunner function
   *
   * @template R
   * @param {(job: T, message: TaskQueueMessage) => Promise<R>} taskRunner
   * @return {*}  {(Promise<{ result: R | undefined }>)}
   * @memberof ITaskQueue
   */
  pullMessage<R>(taskRunner: (job: T, message: TaskQueueMessage) => Promise<R>): Promise<{ result: R | undefined }>
}

export function createSqsAdapter<T>(
  components: Pick<AppComponents, 'logs' | 'metrics'>,
  options: { queueUrl: string; queueRegion?: string }
): any {
  const { logs } = components

  const logger = logs.getLogger('sqs-adapter')

  const sqs = new SQS({ apiVersion: 'latest', region: options.queueRegion })

  return {
    async pullMessage(jobToExecute: any) {
      const params: AWS.SQS.ReceiveMessageRequest = {
        AttributeNames: ['SentTimestamp'],
        MaxNumberOfMessages: 1,
        MessageAttributeNames: ['All'],
        QueueUrl: options.queueUrl,
        WaitTimeSeconds: 15,
        VisibilityTimeout: 3 * 3600 // 3 hours
      }

      while (true) {
        const response = await Promise.race([
          sqs.receiveMessage(params).promise(),
          timeout(30 * 60 * 1000, 'Timed-out while pulling SQS message')
        ]).catch((error: any) => {
          logger.error('Failed while pulling a message from SQS', error)
          return { Messages: [] }
        })

        if (response.Messages && !!response.Messages.length) {
          const { MessageId, Body } = response.Messages[0]

          try {
            const parsedMessage: { Message: string } = JSON.parse(Body!)
            logger.info('Handling message from queue', { id: MessageId!, message: parsedMessage.Message })
            const result = await jobToExecute(JSON.parse(parsedMessage.Message), MessageId!)
            return { result, id: MessageId! }
          } catch (error: any) {
            logger.error('Failed while handling a message from SQS', error)
            return { result: undefined, id: MessageId! }
          } finally {
            await sqs
              .deleteMessage({ QueueUrl: options.queueUrl, ReceiptHandle: response.Messages[0].ReceiptHandle! })
              .promise()
          }
        } else {
          logger.info('No new messages in queue. Retrying for 15 seconds')
          await sleep(15 * 1000)
        }
      }
    }
  }
}
