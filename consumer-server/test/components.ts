// This file is the "test-environment" analogous for src/components.ts
// Here we define the test components to be used in the testing environment
import { createLocalFetchCompoment, createRunner } from '@well-known-components/test-helpers'
import { createInMemoryStorage } from '@dcl/catalyst-storage'

import { main } from '../src/service'
import { TestComponents } from '../src/types'
import { initComponents as originalInitComponents } from '../src/components'

import { createTestMetricsComponent } from '@well-known-components/metrics'
import { metricDeclarations } from '../src/metrics'
import { createDotEnvConfigComponent } from '@well-known-components/env-config-provider'
import { createInMemoryStorageAdapter } from './mocks/storage'

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
        QUEUE_URL: ''
    }
  )

  const metrics = createTestMetricsComponent(metricDeclarations)

  const inMemoryStorage = createInMemoryStorage()
  const storage = createInMemoryStorageAdapter(inMemoryStorage)

  console.log('Test components')
  return {
    ...components,
    localFetch: await createLocalFetchCompoment(config),
    storage,
    metrics
  }
}