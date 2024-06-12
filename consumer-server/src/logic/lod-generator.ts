import { spawn } from 'child_process'
import path from 'path'
import fs from 'fs'

import { AppComponents, LodGenerationResult, LodGeneratorComponent } from '../types'
import { parseMultilineText } from '../utils/text-parser'

export function createLodGeneratorComponent({ logs }: Pick<AppComponents, 'logs'>): LodGeneratorComponent {
  const logger = logs.getLogger('lod-generator')
  const projectRoot = path.resolve(__dirname, '..', '..', '..') // project root according to Dockerfile bundling
  const lodGeneratorProgram = path.join(projectRoot, 'api', 'DCL_PiXYZ.exe') // path to the lod generator program
  const sceneLodEntitiesManifestBuilder = path.join(projectRoot, 'scene-lod') // path to the scene lod entities manifest builder
  const outputPath = path.join(projectRoot, 'built-lods')

  if (!fs.existsSync(outputPath)) {
    fs.mkdirSync(outputPath, { recursive: true })
  }

  async function generate(basePointer: string): Promise<LodGenerationResult> {
    const processOutput = `${outputPath}/${basePointer}`
    let result: LodGenerationResult = {
      error: undefined,
      lodsFiles: [],
      logFile: '',
      outputPath: processOutput
    }

    const commandParts = [
      lodGeneratorProgram,
      "--sceneToConvert", basePointer,
      "--defaultOutputPath", outputPath,
      "--defaultSceneLodManifestDirectory", sceneLodEntitiesManifestBuilder,
      "--decimationValues", "7000;500",
      "--startingLODLevel", "0"
    ]
    
    const childProcess = spawn(commandParts[0], commandParts.slice(1))

    result = await new Promise((resolve, _) => {
      childProcess.on('error', (error) => {
        const logFile = fs.existsSync(processOutput) ? fs.readdirSync(processOutput).find((file) => file.endsWith('output.txt')) || '' : ''

        resolve({
          error: {
            message: parseMultilineText(error?.message || 'Unexpected error from LOD process'),
            detailedError: ''
          },
          lodsFiles: [],
          logFile,
          outputPath: processOutput
        })
      })

      childProcess.on('exit', (code) => {
        if (!fs.existsSync(processOutput)) {
          resolve({
            error: {
              message: 'Output directory does not exist',
              detailedError: `Directory ${processOutput} was not created`
            },
            lodsFiles: [],
            logFile: '',
            outputPath: processOutput
          })

          return
        }

        const generatedFiles = fs.readdirSync(processOutput)
        const logFile = generatedFiles.find((file) => file.endsWith('output.txt')) || ''
        if (code !== 0) {
          resolve({
            error: {
              message: 'LOD process finished with failures',
              detailedError: `Code received from process ${code}`
            },
            lodsFiles: [],
            logFile,
            outputPath: processOutput
          })

          return
        }

        const parsedResult = generatedFiles.reduce((acc, file) => {
          if (file.endsWith('output.txt')) {
            acc.logFile = `${processOutput}/${file}`
          } else {
            acc.lodsFiles.push(`${processOutput}/${file}`)
          }
          return acc
        }, result)

        if (parsedResult.lodsFiles.length === 0) {
          parsedResult.error = {
            message: 'LOD process finished but files are not present in output directory',
            detailedError: parsedResult.logFile ? parseMultilineText(fs.readFileSync(parsedResult.logFile, 'utf8')) : ''
          }
        }

        resolve(parsedResult)
      })

      childProcess.stderr.on('data', (data) => {
        logger.warn(`Error received on LOD process`, {
          error: parseMultilineText(data.toString())
        })
      })

      childProcess.stdout.on('data', (data) => {
        logger.info('LOD process output', {
          output: parseMultilineText(data.toString())
        })
      })
    })

    return result
  }

  return { generate }
}
