import AWS from 'aws-sdk'
import { createDotEnvConfigComponent } from '@well-known-components/env-config-provider'
import { createLogComponent } from '@well-known-components/logger'
import { createServerComponent, createStatusCheckComponent } from '@well-known-components/http-server'
import { createMetricsComponent, instrumentHttpServerWithMetrics } from '@well-known-components/metrics'

import { AppComponents, GlobalContext } from './types'
import { metricDeclarations } from './metrics'
import { createSqsAdapter } from './adapters/sqs'
import { createRunnerComponent } from './logic/job-runner'

export async function initComponents(): Promise<AppComponents> {
  const config = await createDotEnvConfigComponent({ path: ['.env.default', '.env'] })

  const awsRegion = await config.getString('AWS_REGION')
  if (awsRegion) {
    AWS.config.update({ region: awsRegion })
  }

  const metrics = await createMetricsComponent(metricDeclarations, { config })
  const logs = await createLogComponent({ metrics })
  const server = await createServerComponent<GlobalContext>({ config, logs }, {})
  const statusChecks = await createStatusCheckComponent({ server, config })

  await instrumentHttpServerWithMetrics({ metrics, server, config })

  const sqsQueueUrl = await config.getString('QUEUE_URL')
  const queue = createSqsAdapter<any>(
    { logs, metrics },
    { queueUrl: sqsQueueUrl! }
  )

  const jobRunner = createRunnerComponent()

  return {
    config,
    logs,
    server,
    metrics,
    statusChecks,
    queue,
    jobRunner
  }
}
