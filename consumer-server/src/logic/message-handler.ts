import { AppComponents, MessageHandler } from '../types'

export function createMessageHandlerComponent({
  logs,
  lodGenerator,
  storage
}: Pick<AppComponents, 'logs' | 'lodGenerator' | 'storage'>): MessageHandler {
  const logger = logs.getLogger('message-handler')

  async function handle(message: any): Promise<void> {
    if (message.entity.entityType !== 'scene') {
      return
    }

    logger.debug('Processing scene deployment', { message })
    const entityId = message.entity.entityId
    const base = message.entity.metadata.scene.base

    // try {
    const filesToUpload = await lodGenerator.generate(entityId, base)
    logger.info('LODs correctly generated', { files: filesToUpload.join(', '), entityId })
    await storage.storeFiles(filesToUpload, base, message.entity.entityTimestamp.toString())
    // } catch (error: any) {
    //   // TODO: dlq/retry queue
    //   throw error
    // }
  }

  return { handle }
}
