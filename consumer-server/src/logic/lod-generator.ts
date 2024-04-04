import { exec } from 'child_process'
import path from 'path'
import fs from 'fs'

import { LodGenerationResult, LodGeneratorComponent } from '../types'
import { parseMultilineText } from '../utils/text-parser'

export function createLodGeneratorComponent(): LodGeneratorComponent {
  const projectRoot = path.resolve(__dirname, '..', '..', '..') // project root according to Dockerfile bundling
  const lodGeneratorProgram = path.join(projectRoot, 'api', 'DCL_PiXYZ.exe') // path to the lod generator program
  const sceneLodEntitiesManifestBuilder = path.join(projectRoot, 'scene-lod') // path to the scene lod entities manifest builder
  const outputPath = path.join(projectRoot, 'built-lods')

  if (!fs.existsSync(outputPath)) {
    fs.mkdirSync(outputPath, { recursive: true })
  }

  async function generate(basePointer: string, timeoutInMinutes: number): Promise<LodGenerationResult> {
    const processOutput = `${outputPath}/${basePointer}`
    let result: LodGenerationResult = {
      error: undefined,
      lodsFiles: [],
      logFile: '',
      outputPath: processOutput
    }

    const commandToExecute = `${lodGeneratorProgram} "coords" "${basePointer}" "${outputPath}" "${sceneLodEntitiesManifestBuilder}" "false" "false" "500" "3"`

    result = await new Promise((resolve, _) => {
      exec(commandToExecute, { timeout: timeoutInMinutes * 60 * 1000 }, (error, _, stderr) => {
        if (error) {
          if (error.killed) {
            resolve({
              error: {
                message: 'Operation timed out after',
                detailedError: 'LOD generation process timeout after ' + timeoutInMinutes + ' minutes'
              },
              lodsFiles: [],
              logFile: '',
              outputPath: processOutput
            })
          } else {
            resolve({
              error: {
                message: error?.message || 'Unexpected error',
                detailedError: parseMultilineText(stderr)
              },
              lodsFiles: [],
              logFile: '',
              outputPath: processOutput
            })
          }
        }

        if (!fs.existsSync(processOutput)) {
          resolve({
            error: {
              message: 'Output directory do not exists, LODs were not generated',
              detailedError: parseMultilineText(stderr)
            },
            lodsFiles: [],
            logFile: '',
            outputPath: processOutput
          })
        } else {
          const generatedFiles = fs.readdirSync(processOutput)
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
              message: 'LODs are not present in output directory',
              detailedError: parsedResult.logFile ? parseMultilineText(fs.readFileSync(parsedResult.logFile, 'utf8')) : parseMultilineText(stderr)
            }
          }

          resolve(parsedResult)
        }
      })
    })

    return result
  }

  return { generate }
}
