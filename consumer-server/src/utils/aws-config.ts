import { AppComponents, AwsConfig } from '../types'

export async function buildAWSConfiguration({
  config,
  logs
}: Pick<AppComponents, 'config' | 'logs'>): Promise<AwsConfig | undefined> {
  const logger = logs.getLogger('aws-config')
  const region = await config.getString('AWS_REGION')
  const accessKeyId = await config.getString('AWS_ACCESS_KEY_ID')
  const secretAccessKey = await config.getString('AWS_SECRET_ACCESS_KEY')
  const sqsUrl = await config.getString('QUEUE_URL')

  let awsConfig: AwsConfig | undefined = undefined

  if (!!region && !!accessKeyId && !!secretAccessKey && !!sqsUrl) {
    awsConfig = {
      region,
      credentials: {
        accessKeyId,
        secretAccessKey
      },
      sqsUrl
    }
  } else {
    logger.info('Could not load AWS Configuration')
  }

  return awsConfig
}
