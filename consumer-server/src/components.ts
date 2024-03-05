import { createDotEnvConfigComponent } from '@well-known-components/env-config-provider'
import { createLogComponent } from '@well-known-components/logger'
import { createServerComponent, createStatusCheckComponent } from '@well-known-components/http-server'
import { createMetricsComponent, instrumentHttpServerWithMetrics } from '@well-known-components/metrics'
import { createFetchComponent } from '@well-known-components/fetch-component'

import { AppComponents, GlobalContext } from './types'
import { metricDeclarations } from './metrics'
import { createSqsAdapter } from './adapters/sqs'
import { createMessagesConsumerComponent } from './logic/message-consumer'
import { createLodGeneratorComponent } from './logic/lod-generator'
import { createCloudStorageAdapter } from './adapters/storage'
import { createEntityFetcherComponent } from './logic/scene-fetcher'
import { createMemoryQueueAdapter } from './adapters/memory-queue'
import { createBundleTriggererComponent } from './logic/bundle-triggerer'

export async function initComponents(): Promise<AppComponents> {
  const config = await createDotEnvConfigComponent(
    { path: ['.env.default', '.env'] },
    {
      HTTP_SERVER_PORT: '3000',
      HTTP_SERVER_HOST: '0.0.0.0'
    }
  )

  const metrics = await createMetricsComponent(metricDeclarations, { config })
  const logs = await createLogComponent({ metrics })
  const server = await createServerComponent<GlobalContext>({ config, logs }, {})
  const statusChecks = await createStatusCheckComponent({ server, config })
  const fetcher = createFetchComponent({ defaultHeaders: { Origin: 'lods-generator' } })

  await instrumentHttpServerWithMetrics({ metrics, server, config })

  const sceneFetcher = await createEntityFetcherComponent({ config, fetcher })
  const sqsEndpoint = await config.getString('QUEUE_URL')
  const queue = sqsEndpoint ? await createSqsAdapter(sqsEndpoint) : createMemoryQueueAdapter()
  const lodGenerator = createLodGeneratorComponent()
  const storage = await createCloudStorageAdapter({ config })
  const bundleTriggerer = await createBundleTriggererComponent({ fetcher, config })

  const messageConsumer = await createMessagesConsumerComponent({ logs, queue, lodGenerator, storage })

  return {
    config,
    logs,
    server,
    metrics,
    statusChecks,
    queue,
    messageConsumer,
    lodGenerator,
    bundleTriggerer,
    storage,
    fetcher,
    sceneFetcher
  }
}
