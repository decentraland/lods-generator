import { createLodGeneratorComponent } from "../../src/logic/lod-generator"
import path from 'path'

jest.mock('child_process', () => ({
    exec: jest.fn()
  }))
  
jest.mock('fs', () => ({
existsSync: jest.fn(),
mkdirSync: jest.fn(),
readdirSync: jest.fn()
}))

describe('lod-generator', () => {
    const projectRoot = path.resolve(__dirname, '..', '..', '..')
    const outputPath = path.join(projectRoot, 'built-lods')

    it('should generate output directory when it does not exists while building component', () => {
        const execMock = require('child_process').exec
        execMock.mockImplementation((_, __, callback) => {
          callback(null, '', '') // Simulate successful execution
        })

        const fsMock = require('fs')
        fsMock.existsSync.mockReturnValue(false)
        fsMock.readdirSync.mockReturnValue(['lod1.glb', 'output.txt'])

        createLodGeneratorComponent()
        expect(fsMock.mkdirSync).toHaveBeenCalledWith(outputPath, { recursive: true })
    })

    it('should generate lods and return generated files', async () => {
        const execMock = require('child_process').exec
        execMock.mockImplementation((_, __, callback) => {
          callback(null, '', '') // Simulate successful execution
        })

        const fsMock = require('fs')
        fsMock.existsSync.mockReturnValue(true)
        fsMock.readdirSync.mockReturnValue(['lod1.glb', 'output.txt'])

        const base = "0,0"
        const lodGenerator = createLodGeneratorComponent()
        const result = await lodGenerator.generate(base, 20)

        expect(result.lodsFiles).toEqual([`${outputPath}/${base}/lod1.glb`])
        expect(result.logFile).toEqual(`${outputPath}/${base}/output.txt`)
        expect(result.outputPath).toEqual(`${outputPath}/${base}`)
        expect(result.error).toBeUndefined()
    })

    it('should return a specific error when the files were not generated', async () => {
        const execMock = require('child_process').exec
        execMock.mockImplementation((_, __, callback) => {
          callback(null, '', '') // Simulate successful execution
        })

        const fsMock = require('fs')
        fsMock.existsSync.mockReturnValue(true)
        fsMock.readdirSync.mockReturnValue([])

        const base = "0,0"
        const lodGenerator = createLodGeneratorComponent()
        const result = await lodGenerator.generate(base, 20)

        expect(result.error).toBeDefined()
        expect(result.error.message).toEqual('LODs are not present in output directory')
    })

    it('should return a specific error when output file was not created', async () => {
        const execMock = require('child_process').exec
        execMock.mockImplementation((_, __, callback) => {
          callback(null, '', '') // Simulate successful execution
        })

        const fsMock = require('fs')
        fsMock.existsSync.mockReturnValue(false)
        fsMock.readdirSync.mockReturnValue([])
        fsMock.readdirSync.mockClear()

        const base = "0,0"
        const lodGenerator = createLodGeneratorComponent()
        const result = await lodGenerator.generate(base, 20)

        expect(result.error).toBeDefined()
        expect(result.error.message).toEqual('Output directory do not exists, LODs were not generated')
        expect(fsMock.readdirSync).not.toHaveBeenCalled()
    })

    it('should return the error received on callback when it is present', async () => {
        const execMock = require('child_process').exec
        execMock.mockImplementation((_, __, callback) => {
          callback(new Error('Error executing command'), '', '') // Simulate error
        })

        const base = "0,0"
        const lodGenerator = createLodGeneratorComponent()
        const result = await lodGenerator.generate(base, 20)

        expect(result.error).toBeDefined()
        expect(result.error.message).toEqual('Error executing command')
        expect(result.error.detailedError).toEqual('')
    })
    
    it('should return an error when the process time out', async () => {
        const execMock = require('child_process').exec
        execMock.mockImplementation((_, __, callback) => {
          callback({ killed: true }, '', '') // Simulate timeout
        })

        const base = "0,0"
        const lodGenerator = createLodGeneratorComponent()
        const result = await lodGenerator.generate(base, 20)

        expect(result.error).toBeDefined()
        expect(result.error.message).toEqual('Operation timed out after')
        expect(result.error.detailedError).toEqual('LOD generation process timeout after 20 minutes')
    })

    it('should return an error when the process time out with 60 minutes', async () => {
        const execMock = require('child_process').exec
        execMock.mockImplementation((_, __, callback) => {
          callback({ killed: true }, '', '') // Simulate timeout
        })

        const base = "0,0"
        const lodGenerator = createLodGeneratorComponent()
        const result = await lodGenerator.generate(base, 60)

        expect(result.error).toBeDefined()
        expect(result.error.message).toEqual('Operation timed out after')
        expect(result.error.detailedError).toEqual('LOD generation process timeout after 60 minutes')
    })

    it('should transform the timeout in minutes to be in miliseconds when calling process', async () => {
        const execMock = require('child_process').exec
        execMock.mockImplementation((_, __, callback) => {
          callback(null, '', '') // Simulate successful execution
        })

        const base = "0,0"
        const lodGenerator = createLodGeneratorComponent()
        await lodGenerator.generate(base, 20)

        expect(execMock).toHaveBeenCalledWith(expect.any(String), { timeout: 20 * 60 * 1000 }, expect.any(Function))
    })
})