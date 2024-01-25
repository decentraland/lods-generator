import { createDotEnvConfigComponent } from '@well-known-components/env-config-provider'
import { createLogComponent } from '@well-known-components/logger'
import { createServerComponent, createStatusCheckComponent } from '@well-known-components/http-server'
import { createMetricsComponent, instrumentHttpServerWithMetrics } from '@well-known-components/metrics'

import { AppComponents, GlobalContext } from './types'
import { metricDeclarations } from './metrics'
import { createSqsAdapter } from './adapters/sqs'
import { createMessagesConsumerComponent } from './logic/message-consumer'
import { buildLicense } from './utils/license-builder'

export async function initComponents(): Promise<AppComponents> {
  const config = await createDotEnvConfigComponent({ path: ['.env.default', '.env'] }, {
    HTTP_SERVER_PORT: '3000',
    HTTP_SERVER_HOST: '0.0.0.0'
  })

  const metrics = await createMetricsComponent(metricDeclarations, { config })
  const logs = await createLogComponent({ metrics })
  const server = await createServerComponent<GlobalContext>({ config, logs }, {})
  const statusChecks = await createStatusCheckComponent({ server, config })

  await instrumentHttpServerWithMetrics({ metrics, server, config })

  const queue = await createSqsAdapter({ config })
  const messageConsumer = await createMessagesConsumerComponent({ logs, queue })

  await buildLicense({ config, logs })

  return {
    config,
    logs,
    server,
    metrics,
    statusChecks,
    queue,
    messageConsumer
  }
}
