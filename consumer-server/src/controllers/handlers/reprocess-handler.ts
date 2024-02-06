import { IHttpServerComponent } from '@well-known-components/interfaces'
import { HandlerContextWithPath } from '../../types'

export async function reprocessHandler(
  context: HandlerContextWithPath<'logs' | 'entityFetcher' | 'queue', '/reprocess'>
): Promise<IHttpServerComponent.IResponse> {
  const {
    components: { logs, entityFetcher, queue }
  } = context

  const logger = logs.getLogger('reprocess-handler')

  const body = await context.request.json()
  const pointers = (body.pointers as string[]) || []

  if (!pointers.length) {
    return {
      status: 400,
      body: { error: 'A scene pointer must be provided' }
    }
  }

  const entities = await entityFetcher.fetchEntities(pointers)

  logger.info('Reprocessing pointers', { pointers: pointers.join(', '), entitiesAmount: entities.length })

  for (const entity of entities) {
    const message = { ...entity, content: [], pointers: [], spawnPoints: [] }
    logger.debug('Publishing message to queue', { message: JSON.stringify(message) })
    await queue.send(message)
  }

  return {
    status: 200,
    body: { message: 'Scenes reprocessed', sceneAmount: entities.length, pointers }
  }
}
