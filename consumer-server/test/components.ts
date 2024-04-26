// This file is the "test-environment" analogous for src/components.ts
// Here we define the test components to be used in the testing environment
import { createLocalFetchCompoment, createRunner } from '@well-known-components/test-helpers'

import { main } from '../src/service'
import { SceneFetcherComponent, QueueComponent, QueueWorker, TestComponents, StorageComponent } from '../src/types'
import { initComponents as originalInitComponents } from '../src/components'

import { createTestMetricsComponent } from '@well-known-components/metrics'
import { metricDeclarations } from '../src/metrics'
import { createDotEnvConfigComponent } from '@well-known-components/env-config-provider'

/**
 * Behaves like Jest "describe" function, used to describe a test for a
 * use case; it creates a whole new program and components to run an
 * isolated test.
 *
 * State is persistent within the steps of the test.
 */
export const test = createRunner<TestComponents>({
  main,
  initComponents
})

async function initComponents(): Promise<TestComponents> {
  const components = await originalInitComponents()

  const config = await createDotEnvConfigComponent(
    { path: ['.env.default', '.env'] },
    {
        HTTP_SERVER_PORT: '3000',
        HTTP_SERVER_HOST: '0.0.0.0',
        QUEUE_URL: undefined
    }
  )

  const metrics = createTestMetricsComponent(metricDeclarations)

  const storage: StorageComponent = {
    storeFiles: jest.fn(),
    getFiles: jest.fn(),
    deleteFailureDirectory: jest.fn()
  }
  const queue: QueueComponent = {
    deleteMessage: jest.fn(),
    receiveSingleMessage: jest.fn(),
    send: jest.fn()
  }
  const messageConsumer: QueueWorker = {
    start: jest.fn(),
    stop: jest.fn()
  }

  const sceneFetcher: SceneFetcherComponent = {
    fetchByPointers: jest.fn()
  } 

  return {
    ...components,
    localFetch: await createLocalFetchCompoment(config),
    storage,
    metrics,
    queue,
    messageConsumer,
    sceneFetcher
  }
}