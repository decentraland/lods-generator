import { AppComponents, BundleTriggererComponent } from '../types'
import { AuthLinkType } from '@dcl/schemas'
import { Response } from '@well-known-components/interfaces'

export async function createBundleTriggererComponent({
  fetcher,
  config
}: Pick<AppComponents, 'fetcher' | 'config'>): Promise<BundleTriggererComponent> {
  const abServer = await config.requireString('AB_SERVER')
  const abToken = (await config.requireString('AB_TOKEN'))

  async function queueGeneration(entityId: string, lods: string[]): Promise<Response> {
    const body = JSON.stringify({
      lods: lods.map((lod) => lod.replace('%2C', ',')),
      entity: {
        entityId,
        authChain: [
          {
            type: AuthLinkType.SIGNER,
            payload: '0x0000000000000000000000000000000000000000',
            signature: ''
          }
        ]
      }
    })
    const headers = { 'Content-Type': 'application/json', Authorization: abToken }

    return await fetcher.fetch(`${abServer}/queue-task`, { method: 'POST', body, headers })
  }

  return { queueGeneration }
}
