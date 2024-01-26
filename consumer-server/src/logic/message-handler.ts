import { AppComponents, MessageHandler } from '../types'

export function createMessageHandlerComponent({
  logs,
  lodGenerator,
  storage
}: Pick<AppComponents, 'logs' | 'lodGenerator' | 'storage'>): MessageHandler {
  const logger = logs.getLogger('message-handler')

  async function handle(message: { Message: string }): Promise<void> {
    const parsedMessage = JSON.parse(message.Message)

    logger.info('Handling message', { parsedMessage })
    if (parsedMessage.entity.entityType !== 'scene') {
      return
    }

    const entityId = parsedMessage.entity.entityId
    const base = parsedMessage.entity.metadata.scene.base

    try {
      const filesToUpload = await lodGenerator.generate(entityId, base)
      logger.info('LODs correctly generated', { files: filesToUpload.join(', '), entityId })
      await storage.storeFiles(filesToUpload, base, parsedMessage.entity.entityTimestamp.toString())
    } catch (error: any) {
      logger.error('Failed while generating LODs', { error, entityId })
    }
  }

  return { handle }
}
