import { createCatalystClient } from 'dcl-catalyst-client'

import { AppComponents, SceneFetcherComponent } from '../types'

export async function createEntityFetcherComponent({
  config,
  fetcher
}: Pick<AppComponents, 'config' | 'fetcher'>): Promise<SceneFetcherComponent> {
  const catalystUrl = await config.requireString('CATALYST_URL')

  const contentClient = await createCatalystClient({ url: catalystUrl, fetcher }).getContentClient()

  async function fetchByPointers(scenePointers: string[]) {
    return await contentClient.fetchEntitiesByPointers(scenePointers)
  }

  return { fetchByPointers }
}
