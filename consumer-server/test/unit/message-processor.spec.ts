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
      },
      _retry: 3
    }

    await messageProcessor.process(message, 'receiptHandle-3')

    expect(components.queue.deleteMessage).toHaveBeenCalledWith('receiptHandle-3')
    expect(components.storage.storeFiles).toHaveBeenCalledWith([''], 'failures/0,0')
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

  it('should call lod generator with a timeout of 20 minutes on first attempt', async () => {
    const components = getMessageProcessorMockComponents()
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

    await messageProcessor.process(message, 'receiptHandle-6')

    expect(components.lodGenerator.generate).toHaveBeenCalledWith('0,0', 20)
  })
  
  it('should call lod generator with a timeout of 40 minutes on second attempt', async () => {
    const components = getMessageProcessorMockComponents()
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

    await messageProcessor.process(message, 'receiptHandle-7')

    expect(components.lodGenerator.generate).toHaveBeenCalledWith('0,0', 40)
  })

  it('should call lod generator with a timeout of 60 minutes on third attempt', async () => {
    const components = getMessageProcessorMockComponents()
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
      _retry: 2
    }

    await messageProcessor.process(message, 'receiptHandle-7')

    expect(components.lodGenerator.generate).toHaveBeenCalledWith('0,0', 60)
  })

  it('should call lod generator with a timeout of 80 minutes on fourth attempt', async () => {
    const components = getMessageProcessorMockComponents()
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

    await messageProcessor.process(message, 'receiptHandle-7')

    expect(components.lodGenerator.generate).toHaveBeenCalledWith('0,0', 80)
  })
})

function getMessageProcessorMockComponents() {
  return {
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
      storeFiles: jest.fn()
    },
    bundleTriggerer: {
      queueGeneration: jest.fn()
    }
  }
}