import { IHttpServerComponent } from '@well-known-components/interfaces'
import { HandlerContextWithPath, QueueMessage } from '../../types'
import { InvalidRequestError } from '@dcl/platform-server-commons'
import { Events } from '@dcl/schemas'

function validatePointers(pointers: string[]) {
  if (!pointers.length) {
    throw new InvalidRequestError('No pointers provided')
  }

  const pointerRegex = /^-?\d+,-?\d+$/
  for (const pointer of pointers) {
    if (!pointerRegex.test(pointer)) {
      throw new InvalidRequestError(`Invalid pointer: ${pointer}`)
    }
  }
}

export async function reprocessHandler(
  context: Pick<HandlerContextWithPath<'logs' | 'config' | 'sceneFetcher' | 'queue', '/reprocess'>, 'components' | 'request'>
): Promise<IHttpServerComponent.IResponse> {
  const {
    components: { logs, config, sceneFetcher, queue },
    request
  } = context

  const logger = logs.getLogger('reprocess-handler')

  const body = await request.json()
  const pointers = (body.pointers as string[]) || []

  validatePointers(pointers)

  const catalystUrl = await config.requireString('CATALYST_URL')

  try {
    const entities = await sceneFetcher.fetchByPointers(pointers)

    logger.info('Reprocessing pointers', { pointers: pointers.join(', '), entitiesAmount: entities.length })

    for (const entity of entities) {
      const message: QueueMessage = {
        type: Events.Type.CATALYST_DEPLOYMENT,
        subType: entity.type,
        key: entity.id,
        timestamp: entity.timestamp,
        contentServerUrls: entity.contentServerUrls || [catalystUrl],
        entity
      }

      logger.debug('Publishing message to queue', { message: JSON.stringify(message) })
      await queue.send(message)
    }

    return {
      status: 200,
      body: { message: 'Scenes reprocessed', sceneAmount: entities.length, pointers }
    }
  } catch (error: any) {
    logger.error('Failed while republishing scenes to be reprocessed', { error: error.message })
    throw new Error('Failed while republishing scenes to be reprocessed')
  }
}
