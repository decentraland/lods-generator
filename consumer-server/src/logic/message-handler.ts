import fs from 'fs'

import { AppComponents, LodGenerationResult, MessageHandlerComponent, QueueMessage } from '../types'

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

    logger.info('Processing scene deployment', { message: JSON.stringify(message) })
    const entityId = message.entity.entityId
    const base = message.entity.metadata.scene.base

    const result: LodGenerationResult = await lodGenerator.generate(base)

    logger.info('LOD generation result', {
      entityId,
      base,
      generatedFiles: result.lodsFiles.map((file) => file.split('/').pop()).join(', ')
    })
    logger.debug('LOD generation log', { logFile: fs.readFileSync(result.logFile, 'utf-8') })

    if (result.error) {
      logger.error('Error while generating LODs', { error: result.error.message || 'Unexpected failure' })
      logger.debug('Details about execution failure', { errorDetails: result.error.detailedError })
    }

    const filesToUpload = result.lodsFiles.concat(result.logFile)
    await storage.storeFiles(filesToUpload, base, message.entity.entityTimestamp.toString())
    fs.rmdirSync(result.outputPath, { recursive: true })
  }

  return { handle }
}
