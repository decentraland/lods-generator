import { exec } from 'child_process'
import path from 'path'
import fs from 'fs'

import { LodGenerationResult, LodGeneratorComponent } from '../types'

export function createLodGeneratorComponent(): LodGeneratorComponent {
  const projectRoot = path.resolve(__dirname, '..', '..', '..') // project root according to Dockerfile bundling
  const lodGeneratorProgram = path.join(projectRoot, 'api', 'DCL_PiXYZ.exe') // path to the lod generator program
  const sceneLodEntitiesManifestBuilder = path.join(projectRoot, 'scene-lod') // path to the scene lod entities manifest builder
  const outputPath = path.join(projectRoot, 'built-lods')

  if (!fs.existsSync(outputPath)) {
    fs.mkdirSync(outputPath, { recursive: true })
  }

  async function generate(basePointer: string): Promise<LodGenerationResult> {
    let result: LodGenerationResult = {
      error: undefined,
      lodsFiles: [],
      logFile: ''
    }

    const commandToExecute = `${lodGeneratorProgram} "coords" "${basePointer}" "${outputPath}" "${sceneLodEntitiesManifestBuilder}"`
    result = await new Promise((resolve, _) => {
      exec(commandToExecute, (error, _, stderr) => {
        if (error || stderr) {
          result.error = {
            message: error?.message || 'Unexpected error',
            detailedError: ((stderr as string) || '').replace('\n', ' ')
          }
        }

        const processOutput = `${outputPath}/${basePointer}`
        if (fs.existsSync(processOutput)) {
          const generatedFiles = fs.readdirSync(processOutput)
          result = generatedFiles.reduce((acc, file) => {
            if (file.endsWith('output.txt')) {
              acc.logFile = `${processOutput}/${file}`
            } else {
              acc.lodsFiles.push(`${processOutput}/${file}`)
            }
            return acc
          }, result)

          resolve(result)
        } else {
          resolve(result)
        }
      })
    })

    return result
  }

  return { generate }
}
