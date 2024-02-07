import { IHttpServerComponent } from '@well-known-components/interfaces'
import { HandlerContextWithPath } from '../../types'

export async function healthHandler(
  context: Pick<HandlerContextWithPath<'logs', '/health'>, 'url' | 'components'>
): Promise<IHttpServerComponent.IResponse> {
  const {
    components: { logs }
  } = context

  logs.getLogger('health-handler').info('Health check received')

  return {
    body: { alive: true }
  }
}
