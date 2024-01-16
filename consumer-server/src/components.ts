import { createDotEnvConfigComponent } from '@well-known-components/env-config-provider'
import { createLogComponent } from '@well-known-components/logger'
import { createServerComponent, createStatusCheckComponent } from '@well-known-components/http-server'
import { createMetricsComponent, instrumentHttpServerWithMetrics } from '@well-known-components/metrics'

import { AppComponents, GlobalContext } from './types'
import { metricDeclarations } from './metrics'
import { createSqsAdapter } from './adapters/sqs'
import { createMessagesConsumerComponent } from './logic/message-consumer'
import { buildAWSConfiguration } from './utils/aws-config'

export async function initComponents(): Promise<AppComponents> {
  const config = await createDotEnvConfigComponent({ path: ['.env.default', '.env'] })

  const metrics = await createMetricsComponent(metricDeclarations, { config })
  const logs = await createLogComponent({ metrics })
  const server = await createServerComponent<GlobalContext>({ config, logs }, {})
  const statusChecks = await createStatusCheckComponent({ server, config })

  await instrumentHttpServerWithMetrics({ metrics, server, config })

  const awsConfig = await buildAWSConfiguration({ config, logs })
  const queue = await createSqsAdapter({ logs, awsConfig: awsConfig! })
  const messageConsumer = await createMessagesConsumerComponent({ logs, queue })

  return {
    config,
    awsConfig,
    logs,
    server,
    metrics,
    statusChecks,
    queue,
    messageConsumer
  }
}