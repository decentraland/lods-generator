import { AppComponents, MessageHandlerComponent, QueueMessage } from '../types'

export function createMessageHandlerComponent({
  logs,
  lodGenerator,
  storage
}: Pick<AppComponents, 'logs' | 'lodGenerator' | 'storage'>): MessageHandlerComponent {
  const logger = logs.getLogger('message-handler')

  async function handle(message: QueueMessage): Promise<void> {
    if (message.entity.entityType !== 'scene') {
      return
    }

    logger.debug('Processing scene deployment', { message: JSON.stringify(message) })
    const entityId = message.entity.entityId
    const base = message.entity.metadata.scene.base

    const filesToUpload = await lodGenerator.generate(entityId, base)
    logger.info('LODs correctly generated', { files: filesToUpload.join(', '), entityId })
    await storage.storeFiles(filesToUpload, base, message.entity.entityTimestamp.toString())
  }

  return { handle }
}
