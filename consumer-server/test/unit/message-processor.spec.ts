import { createMessageProcesorComponent } from '../../src/logic/message-processor'

describe('message-processor', () => {
  it('should not process an entity that is not a scene', async () => {
    const components = getMessageProcessorMockComponents()
    const messageProcessor = await createMessageProcesorComponent(components)
    const message = {
      entity: {
        entityType: 'profile',
        entityId: 'randomId',
        entityTimestamp: 0,
        metadata: {
          scene: {
            base: '0,0'
          }
        }
      }
    }

    await messageProcessor.process(message, 'receiptHandle-1')
    
    expect(components.queue.deleteMessage).toHaveBeenCalledWith('receiptHandle-1')
    expect(components.queue.send).not.toHaveBeenCalled()
  })

  it('should retry processing a message when lod generation fails (currentAttempt = 1)', async () => {
    const components = getMessageProcessorMockComponents()
    components.lodGenerator.generate.mockResolvedValue({
        lodsFiles: [],
        logFile: '',
        error: {
            message: 'Error message',
            detailedError: 'Detailed error message'
        },
        outputPath: ''})
    const messageProcessor = await createMessageProcesorComponent(components)
    const message = {
      entity: {
        entityType: 'scene',
        entityId: 'randomId',
        entityTimestamp: 0,
        metadata: {
          scene: {
            base: '0,0'
          }
        }
      }
    }

    await messageProcessor.process(message, 'receiptHandle-2')

    expect(components.storage.storeFiles).not.toHaveBeenCalled()
    expect(components.queue.deleteMessage).toHaveBeenCalledWith('receiptHandle-2')
    expect(components.queue.send).toHaveBeenCalledWith({
      ...message,
      _retry: 1
    })
  })

  it('should not retry processing a message when lod generation fails (currentAttempt = 3) but store log file', async () => {
    const components = getMessageProcessorMockComponents()
    components.lodGenerator.generate.mockResolvedValue({
        lodsFiles: [],
        logFile: 'somefile',
        error: {
            message: 'Error message',
            detailedError: 'Detailed error message'
        },
        outputPath: ''})
    const messageProcessor = await createMessageProcesorComponent(components)
    const message = {
      entity: {
        entityType: 'scene',
        entityId: 'randomId',
        entityTimestamp: 0,
        metadata: {
          scene: {
            base: '0,0'
          }
        }
      },
      _retry: 3
    }

    await messageProcessor.process(message, 'receiptHandle-3')

    expect(components.queue.deleteMessage).toHaveBeenCalledWith('receiptHandle-3')
    expect(components.storage.storeFiles).toHaveBeenCalledWith(['somefile'], 'failures/0,0')
    expect(components.queue.send).not.toHaveBeenCalled()
  })

  it('should remove output directory when lods generation fails', async () => {
    const components = getMessageProcessorMockComponents()
    components.lodGenerator.generate.mockResolvedValue({
        lodsFiles: [],
        logFile: '',
        error: {
            message: 'Error message',
            detailedError: 'Detailed error message'
        },
        outputPath: 'outputPath'})
    // mock fs
    const fs = require('fs')
    fs.rmSync = jest.fn()
    fs.existsSync = jest.fn().mockReturnValue(true)
    const messageProcessor = await createMessageProcesorComponent(components)
    const message = {
      entity: {
        entityType: 'scene',
        entityId: 'randomId',
        entityTimestamp: 0,
        metadata: {
          scene: {
            base: '0,0'
          }
        }
      },
      _retry: 1
    }

    await messageProcessor.process(message, 'receiptHandle-4')

    expect(components.queue.deleteMessage).toHaveBeenCalledWith('receiptHandle-4')
    expect(components.queue.send).toHaveBeenCalled()
    expect(fs.rmSync).toHaveBeenCalledWith('outputPath', { recursive: true, force: true })
  })

  it('should remove output directory when lods generation fails (currentAttempt = 2)', async () => {
    const components = getMessageProcessorMockComponents()
    components.lodGenerator.generate.mockResolvedValue({
        lodsFiles: ['afile.txt'],
        logFile: '',
        outputPath: 'outputPath'})
    components.storage.storeFiles.mockRejectedValue(new Error('Error storing files'))
    
        // mock fs
    const fs = require('fs')
    fs.rmSync = jest.fn()
    fs.existsSync = jest.fn().mockReturnValue(true)
    const messageProcessor = await createMessageProcesorComponent(components)
    const message = {
      entity: {
        entityType: 'scene',
        entityId: 'randomId',
        entityTimestamp: 0,
        metadata: {
          scene: {
            base: '0,0'
          }
        }
      },
      _retry: 1
    }

    await messageProcessor.process(message, 'receiptHandle-4')

    expect(components.queue.deleteMessage).toHaveBeenCalledWith('receiptHandle-4')
    expect(components.queue.send).toHaveBeenCalled()
    expect(fs.rmSync).toHaveBeenCalledWith('outputPath', { recursive: true, force: true })
  })

  it('should remove output directory when lods generation fails (currentAttempt = 3)', async () => {
    const components = getMessageProcessorMockComponents()
    components.lodGenerator.generate.mockResolvedValue({
        lodsFiles: ['afile.txt'],
        logFile: '',
        outputPath: 'outputPath'})
    components.storage.storeFiles.mockRejectedValue(new Error('Error storing files'))
    
    // mock fs
    const fs = require('fs')
    fs.rmSync = jest.fn()
    fs.existsSync = jest.fn().mockReturnValue(true)
    const messageProcessor = await createMessageProcesorComponent(components)
    const message = {
      entity: {
        entityType: 'scene',
        entityId: 'randomId',
        entityTimestamp: 0,
        metadata: {
          scene: {
            base: '0,0'
          }
        }
      },
      _retry: 3
    }

    await messageProcessor.process(message, 'receiptHandle-4')

    expect(components.queue.deleteMessage).toHaveBeenCalledWith('receiptHandle-4')
    expect(components.queue.send).not.toHaveBeenCalled()
    expect(fs.rmSync).toHaveBeenCalledWith('outputPath', { recursive: true, force: true })
  })

  it('should store log file when lod generation fails on third attempt', async () => {
    const components = getMessageProcessorMockComponents()
    components.lodGenerator.generate.mockResolvedValue({
        lodsFiles: [],
        logFile: 'logFile.txt',
        error: {
            message: 'Error message',
            detailedError: 'Detailed error message'
        },
        outputPath: ''})
    const messageProcessor = await createMessageProcesorComponent(components)
    const message = {
      entity: {
        entityType: 'scene',
        entityId: 'randomId',
        entityTimestamp: 0,
        metadata: {
          scene: {
            base: '0,0'
          }
        }
      },
      _retry: 3
    }

    await messageProcessor.process(message, 'receiptHandle-5')

    expect(components.queue.deleteMessage).toHaveBeenCalledWith('receiptHandle-5')
    expect(components.storage.storeFiles).toHaveBeenCalledWith(['logFile.txt'], 'failures/0,0')
    expect(components.queue.send).not.toHaveBeenCalled()
  })
})

function getMessageProcessorMockComponents() {
  return {
    metrics: {
      increment: jest.fn(),
      observe: jest.fn(),
    } as any,
    logs: {
      getLogger: jest.fn().mockReturnValue({
        info: jest.fn(),
        warn: jest.fn(),
        debug: jest.fn(),
        error: jest.fn()
      })
    },
    config: {
      requireString: jest.fn().mockResolvedValue('.;.'),
      getString: jest.fn()
    } as any,
    queue: {
      send: jest.fn(),
      receiveSingleMessage: jest.fn(),
      deleteMessage: jest.fn()
    },
    lodGenerator: {
      generate: jest.fn()
    },
    storage: {
      storeFiles: jest.fn(),
      getFiles: jest.fn().mockResolvedValue([]),
      deleteFailureDirectory: jest.fn()
    },
    bundleTriggerer: {
      queueGeneration: jest.fn()
    }
  }
}