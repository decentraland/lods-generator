import { Lifecycle } from '@well-known-components/interfaces'
import { AppComponents, GlobalContext, TestComponents } from './types'
import { setupRouter } from './controllers/routes'

export async function main(program: Lifecycle.EntryPointParameters<AppComponents | TestComponents>) {
  const { components, startComponents } = program
  const globalContext: GlobalContext = {
    components
  }

  const router = await setupRouter(globalContext)
  // register routes middleware
  components.server.use(router.middleware())
  // register not implemented/method not allowed/cors responses middleware
  components.server.use(router.allowedMethods())
  // set the context to be passed to the handlers
  components.server.setContext(globalContext)

  await startComponents()

  components.jobRunner.runTask(async ({ isRunning }) => {
    while (isRunning) {
      await components.queue.pullMessage(async (message, messageId): Promise<void> => {
        console.log('Message received: ', message, messageId)
      })
    }
  })
}
