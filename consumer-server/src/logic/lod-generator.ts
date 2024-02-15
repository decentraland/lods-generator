import { exec } from 'child_process'
import path from 'path'
import os from 'os'
import fs from 'fs'

import { AppComponents, LodGeneratorComponent } from '../types'

export function createLodGeneratorComponent({ logs }: Pick<AppComponents, 'logs'>): LodGeneratorComponent {
  const logger = logs.getLogger('lod-generator')
  const projectRoot = path.resolve(__dirname, '..', '..', '..') // project root according to Dockerfile bundling
  const lodGeneratorProgram = path.join(projectRoot, 'api', 'DCL_PiXYZ.exe') // path to the lod generator program
  const sceneLodEntitiesManifestBuilder = path.join(projectRoot, 'scene-lod') // path to the scene lod entities manifest builder
  const outputPath = path.join(os.tmpdir(), 'built-lods')

  async function generate(basePointer: string): Promise<string[] | undefined> {
    let files: string[] = []
    if (!fs.existsSync(outputPath)) {
      fs.mkdirSync(outputPath, { recursive: true })
    }

    try {
      const commandToExecute = `${lodGeneratorProgram} "coords" "${basePointer}" "${outputPath}" "${sceneLodEntitiesManifestBuilder}"`
      console.log({ commandToExecute })
      files = await new Promise((resolve, reject) => {
        exec(commandToExecute, (error, _stdout, stderr) => {
          const processOutput = `${outputPath}/${basePointer}`
          if (fs.existsSync(processOutput)) {
            const generatedFiles = fs.readdirSync(processOutput)
            // if files exists return otherwise reject
            if (generatedFiles.length > 0) {
              resolve(generatedFiles.map((file) => `${processOutput}/${file}`))
            } else {
              resolve([])
            }
          }else{
            console.log("I FAILED")
            resolve([])
          }
        })
      })
    } catch (error: any) {
      logger.error('Failed while generating LODs', {
        error: error.message
      })
      return undefined
    }

    return files
  }

  return { generate }
}
