import { Router } from '@well-known-components/http-server'
import { bearerTokenMiddleware, errorHandler } from '@dcl/platform-server-commons'

import { GlobalContext } from '../types'
import { healthHandler } from './handlers/health-handler'
import { reprocessHandler } from './handlers/reprocess-handler'

// We return the entire router because it will be easier to test than a whole server
export async function setupRouter(globalContext: GlobalContext): Promise<Router<GlobalContext>> {
  const router = new Router<GlobalContext>()

  router.use(errorHandler)

  router.get('/health', healthHandler)

  // administrative endpoints
  const secret = await globalContext.components.config.requireString('AUTH_SECRET')
  if (secret) {
    router.post('/reprocess', bearerTokenMiddleware(secret), reprocessHandler)
  }

  return router
}
