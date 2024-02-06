import { createCatalystClient } from 'dcl-catalyst-client'

import { AppComponents, EntityFetcherComponent } from '../types'

export async function createEntityFetcherComponent({
  logs,
  fetcher
}: Pick<AppComponents, 'logs' | 'fetcher'>): Promise<EntityFetcherComponent> {
  const logger = logs.getLogger('entity-fetcher')
  const contentClient = await createCatalystClient({ url: 'https://peer.decentraland.org', fetcher }).getContentClient()

  async function fetchEntities(entityIds: string[]) {
    try {
      const entities = await contentClient.fetchEntitiesByIds(entityIds)
      return entities
    } catch (error: any) {
      logger.error('Failed while fetching entity', { error })
      return null
    }
  }

  return { fetchEntities }
}
