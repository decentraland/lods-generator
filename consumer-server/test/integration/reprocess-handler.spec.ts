import { test } from '../components'
import sinon from 'sinon'

test('reprocess scenes handler /reprocess', function ({ components, stubComponents }) {
  it('should fail if no pointers are provided', async () => {
    const { localFetch } = components
    const r = await localFetch.fetch(`/reprocess`, {
      method: 'POST',
      body: JSON.stringify({})
    })

    expect(r.status).toEqual(400)
    expect(await r.json()).toEqual({ error: 'A scene pointer must be provided' })
  })

  it('should publish a scene to be reprocessed when a pointer is provided', async () => {
    const { localFetch } = components
    const { queue, sceneFetcher } = stubComponents
    const pointer = '0,0'
    const entity = { id: 'randomId', type: 'scene', timestamp: 0, metadata: { scene: { base: pointer } } }
    sceneFetcher.fetchByPointers.resolves([entity])

    const r = await localFetch.fetch(`/reprocess`, {
      method: 'POST',
      body: JSON.stringify({ pointers: [pointer] })
    })

    sinon.assert.calledWith(
      queue.send,
      sinon.match({
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
      })
    )

    expect(r.status).toEqual(200)
    expect(await r.json()).toEqual({ message: 'Scenes reprocessed', sceneAmount: 1, pointers: [pointer] })
  })

  it('should publish multiple scenes to be reprocessed when multiple pointers are provided', async () => {
    const { localFetch } = components
    const { queue, sceneFetcher } = stubComponents
    const pointers = ['0,0', '0,1', '1,0']
    const entities = pointers.map((p, i) => ({
      id: `randomId${i}`,
      type: 'scene',
      timestamp: 0,
      metadata: { scene: { base: p } }
    }))
    sceneFetcher.fetchByPointers.resolves(entities)

    const r = await localFetch.fetch(`/reprocess`, {
      method: 'POST',
      body: JSON.stringify({ pointers })
    })

    sinon.assert.calledWith(
      queue.send,
      sinon.match({
        entity: {
          entityType: entities[0].type,
          entityId: entities[0].id,
          entityTimestamp: entities[0].timestamp,
          metadata: {
            scene: {
              base: entities[0].metadata.scene.base
            }
          }
        }
      })
    )
    sinon.assert.calledWith(
      queue.send,
      sinon.match({
        entity: {
          entityType: entities[1].type,
          entityId: entities[1].id,
          entityTimestamp: entities[1].timestamp,
          metadata: {
            scene: {
              base: entities[1].metadata.scene.base
            }
          }
        }
      })
    )
    sinon.assert.calledWith(
      queue.send,
      sinon.match({
        entity: {
          entityType: entities[2].type,
          entityId: entities[2].id,
          entityTimestamp: entities[2].timestamp,
          metadata: {
            scene: {
              base: entities[2].metadata.scene.base
            }
          }
        }
      })
    )
    expect(r.status).toEqual(200)
    expect(await r.json()).toEqual({ message: 'Scenes reprocessed', sceneAmount: 3, pointers })
  })
})
