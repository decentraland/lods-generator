import { AppComponents, BundleTriggererComponent, QueueMessage } from '../types'
import { Response } from '@well-known-components/interfaces'

export async function createBundleTriggererComponent({
  fetcher,
  config
}: Pick<AppComponents, 'fetcher' | 'config'>): Promise<BundleTriggererComponent> {
  const abToken = await config.requireString('AB_TOKEN')

  async function queueGeneration(message: QueueMessage, lods: string[], abServer: string): Promise<Response> {
    const body = JSON.stringify({
      lods: lods.map((lod) => lod.replace('%2C', ',')),
      ...message
    })
    const headers = { 'Content-Type': 'application/json', Authorization: abToken }

    return await fetcher.fetch(`${abServer}/queue-task`, {
      method: 'POST',
      body,
      headers,
      attempts: 3,
      retryDelay: 1000
    })
  }

  return { queueGeneration }
}
