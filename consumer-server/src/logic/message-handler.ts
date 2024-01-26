import { AppComponents, MessageHandler } from '../types'

export function createMessageHandlerComponent({
  logs,
  lodGenerator
}: Pick<AppComponents, 'logs' | 'lodGenerator'>): MessageHandler {
  const logger = logs.getLogger('message-handler')

  async function handle(message: { Message: string }): Promise<void> {
    const parsedMessage = JSON.parse(message.Message)

    if (parsedMessage.entityType !== 'scene') {
      return
    }

    const { base, entityId } = parsedMessage

    try {
      const result = await lodGenerator.generate(entityId, base)
      logger.info('LODs correctly generated', { files: result.join(', '), entityId })
    } catch (error: any) {
      logger.error('Failed while generating LODs', { error: error.message, entityId })
    }
  }

  return { handle }
}
