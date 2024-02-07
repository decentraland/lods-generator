import { IHttpServerComponent } from '@well-known-components/interfaces'
import { HandlerContextWithPath } from '../../types'

export async function reprocessHandler(
  context: Pick<HandlerContextWithPath<'logs' | 'sceneFetcher' | 'queue', '/reprocess'>, 'components' | 'request'>
): Promise<IHttpServerComponent.IResponse> {
  const {
    components: { logs, sceneFetcher, queue },
    request
  } = context

  const logger = logs.getLogger('reprocess-handler')

  const body = await request.json()
  const pointers = (body.pointers as string[]) || []

  if (!pointers.length) {
    return {
      status: 400,
      body: { error: 'A scene pointer must be provided' }
    }
  }
  try {
    const entities = await sceneFetcher.fetchByPointers(pointers)

    logger.info('Reprocessing pointers', { pointers: pointers.join(', '), entitiesAmount: entities.length })

    for (const entity of entities) {
      const message = {
        entity: {
          entityType: entity.type,
          entityId: entity.id,
          entityTimestamp: entity.timestamp,
          metadata: {
            scene: {
              base: entity.metadata.scene.base
            }
          }
        }
      }
      logger.debug('Publishing message to queue', { message: JSON.stringify(message) })
      await queue.send(message)
    }

    return {
      status: 200,
      body: { message: 'Scenes reprocessed', sceneAmount: entities.length, pointers }
    }
  } catch (error: any) {
    logger.error('Failed while republishing scenes to be reporcessed', { error })
    return {
      status: 500,
      body: { error: 'Failed while republishing scenes to be reporcessed' }
    }
  }
}
