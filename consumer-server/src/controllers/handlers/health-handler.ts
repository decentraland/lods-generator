import { HandlerContextWithPath } from '../../types'

export async function healthHandler(
  context: Pick<HandlerContextWithPath<'logs', '/health'>, 'url' | 'components'>
): Promise<{ body: { alive: boolean } }> {
  const {
    components: { logs }
  } = context

  logs.getLogger('health-handler').info('Health check received')

  return {
    body: { alive: true }
  }
}
