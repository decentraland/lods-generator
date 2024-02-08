import { createCatalystClient } from 'dcl-catalyst-client'

import { AppComponents, SceneFetcherComponent } from '../types'

export async function createEntityFetcherComponent({
  config,
  fetcher
}: Pick<AppComponents, 'config' | 'fetcher'>): Promise<SceneFetcherComponent> {
  const catalystUrl = await config.getString('CATALYST_URL')

  if (!catalystUrl) {
    throw new Error('Failed while bootstraping entity fetcher component: CATALYST_URL is not defined')
  }

  const contentClient = await createCatalystClient({ url: catalystUrl, fetcher }).getContentClient()

  async function fetchByPointers(scenePointers: string[]) {
    return await contentClient.fetchEntitiesByPointers(scenePointers)
  }

  return { fetchByPointers }
}
