import { AppComponents, MessageHandler } from '../types'

export function createMessageHandlerComponent({
  logs,
  lodGenerator,
  storage
}: Pick<AppComponents, 'logs' | 'lodGenerator' | 'storage'>): MessageHandler {
  const logger = logs.getLogger('message-handler')

  async function handle(message: any): Promise<void> {
    console.log({ message })
    logger.info('Handling', { type: message.type, id: message.entity.entityId })

    if (message.entity.entityType !== 'scene') {
        return
    }
    logger.info('Handling message', { message })

    const entityId = message.entity.entityId
    const base = message.entity.metadata.scene.base

    try {
      const filesToUpload = await lodGenerator.generate(entityId, base)
      logger.info('LODs correctly generated', { files: filesToUpload.join(', '), entityId })
    //   await storage.storeFiles(filesToUpload, base, parsedMessage.entity.entityTimestamp.toString())
    } catch (error: any) {
      logger.error('Failed while generating LODs', { error, entityId })
    }
  }

  return { handle }
}
