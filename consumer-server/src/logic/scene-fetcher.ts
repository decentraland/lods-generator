import { createCatalystClient } from 'dcl-catalyst-client'

import { AppComponents, SceneFetcherComponent } from '../types'

export async function createEntityFetcherComponent({
  fetcher
}: Pick<AppComponents, 'fetcher'>): Promise<SceneFetcherComponent> {
  const contentClient = await createCatalystClient({ url: 'https://peer.decentraland.org', fetcher }).getContentClient()

  async function fetchByPointers(scenePointers: string[]) {
    return await contentClient.fetchEntitiesByPointers(scenePointers)
  }

  return { fetchByPointers }
}
