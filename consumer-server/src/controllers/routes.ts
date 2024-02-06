import { Router } from '@well-known-components/http-server'
import { GlobalContext } from '../types'
import { healthHandler } from './handlers/health-handler'
import { reprocessHandler } from './handlers/reprocess-handler'

// We return the entire router because it will be easier to test than a whole server
export async function setupRouter(_: GlobalContext): Promise<Router<GlobalContext>> {
  const router = new Router<GlobalContext>()

  router.get('/health', healthHandler)
  router.post('/reprocess', reprocessHandler)

  return router
}
