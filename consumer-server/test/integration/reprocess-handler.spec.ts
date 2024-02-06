import { test } from '../components'

test('reprocess scenes handler /reprocess', function ({ components }) { 
    it('should fail if no pointers are provided', async () => {
        console.log('reprocess scenes handler /reprocess')
        const { localFetch } = components
        const r = await localFetch.fetch(`/reprocess`, {
            method: 'POST',
            body: JSON.stringify({})
        })

        expect(r.status).toEqual(400)
        expect(await r.json()).toEqual({ error: 'A scene pointer must be provided' })
    })
})