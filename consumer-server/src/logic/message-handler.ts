import fs from 'fs'

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

    const filesToUpload = await lodGenerator.generate(base)

    const resultTxt = filesToUpload.find((file) => file.endsWith('output.txt'))
    if (resultTxt) {
      const lodGenerationResult = fs.readFileSync(resultTxt, 'utf-8')
      logger.info('LOD generation result', { result: lodGenerationResult, entityId })
    }

    if (filesToUpload.length === 0) {
      return
    }

    logger.info('LODs correctly generated', { files: filesToUpload.join(', '), entityId })
    await storage.storeFiles(filesToUpload, base, message.entity.entityTimestamp.toString())
  }

  return { handle }
}
