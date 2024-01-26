import { AppComponents, MessageHandler } from '../types'

export function createMessageHandlerComponent({
  logs,
  lodGenerator
}: Pick<AppComponents, 'logs' | 'lodGenerator'>): MessageHandler {
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
      const result = await lodGenerator.generate(entityId, base)
      logger.info('LODs correctly generated', { files: result.join(', '), entityId })
    } catch (error: any) {
      logger.error('Failed while generating LODs', { error: error.message, entityId })
    }
  }

  return { handle }
}
