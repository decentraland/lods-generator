import { createMemoryQueueAdapter } from "../../src/adapters/memory-queue"
import { createMessagesConsumerComponent } from "../../src/logic/message-consumer"
import { BundleTriggererComponent, LodGenerationResult, LodGeneratorComponent, StorageComponent } from "../../src/types"
import { EventEmitter } from "events"
import os from 'os'

describe('message-consumer', () => {
    it('should remove message from queue when it is not a scene', async () => {
        const { eventEmitter, ...components } = getComponentsAndEmitter()
        const spyRemoveMessageFromQueue = jest.spyOn(components.queue, 'deleteMessage')
        const messageConsumer = await createMessagesConsumerComponent(components)
        messageConsumer.start({} as any)

        const receiptId = await components.queue.send({ entity: { entityType: 'notScene', entityId: 'test-id-1' } } as any)

        await new Promise(resolve => {
            eventEmitter.on('messageDeleted', () => {
                expect(spyRemoveMessageFromQueue).toHaveBeenCalledWith(receiptId)
                resolve(undefined)
            })
        })

        await messageConsumer.stop()
    })
    
    it('should remove message from queue at third attempt to generate lods', async () => {
        const { eventEmitter, ...components } = getComponentsAndEmitter()
        const spyRemoveMessageFromQueue = jest.spyOn(components.queue, 'deleteMessage')
        components.lodGenerator.generate = jest.fn().mockResolvedValue(() => Promise.resolve({ lodsFiles: [], logFile: 'test', error: { message: 'test', detailedError: 'unit-test' }, outputPath: os.tmpdir() } as LodGenerationResult))
        
        const messageConsumer = await createMessagesConsumerComponent(components)
        const receiptId = await components.queue.send({ entity: { entityType: 'scene', entityId: 'test-id-1', metadata: { scene: { base: '0,0' }} } } as any)
        const spySendMessage = jest.spyOn(components.queue, 'send')

        messageConsumer.start({} as any)
        
        
        await new Promise(resolve => {
            eventEmitter.on('messageDeleted', () => {
                expect(spySendMessage).toHaveBeenCalledTimes(3)
                expect(spyRemoveMessageFromQueue).toHaveBeenCalledWith(receiptId)
                resolve(undefined)
            })
        })

        await messageConsumer.stop()
    })
})

function getComponentsAndEmitter() {
    const eventEmitter = new EventEmitter()
    const logs = {
        getLogger: jest.fn().mockReturnValue({
            info: jest.fn(),
            error: jest.fn(),
            warn: jest.fn(),
            debug: jest.fn()
        })
    }
    const config = {
        requireString: jest.fn().mockResolvedValue('')
    } as any
    const queue = createMemoryQueueAdapter(eventEmitter)
    const lodGenerator: LodGeneratorComponent = {
        generate: jest.fn().mockResolvedValue({} as any)
    }
    const storage: StorageComponent = {
        storeFiles: jest.fn().mockResolvedValue('')
    }
    const bundleTriggerer: BundleTriggererComponent = {
        queueGeneration: jest.fn().mockResolvedValue('')
    }
    return { logs, config, queue, lodGenerator, storage, bundleTriggerer, eventEmitter }
}