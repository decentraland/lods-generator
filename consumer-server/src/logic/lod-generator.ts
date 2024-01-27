import { exec } from 'child_process'
import path from 'path'
import os from 'os'
import fs from 'fs'

import { LodGeneratorService } from '../types'

export function createLodGeneratorComponent(): LodGeneratorService {
  const projectRoot = path.resolve(__dirname, '..', '..', '..') // project root according to Dockerfile bundling
  const lodGeneratorProgram = path.join(projectRoot, 'api', 'DCL_PiXYZ.exe') // path to the lod generator program
  const sceneLodEntitiesManifestBuilder = path.join(projectRoot, 'scene-lod') // path to the scene lod entities manifest builder

  async function generate(entityId: string, basePointer: string): Promise<string[]> {
    const outputPath = path.join(os.tmpdir(), entityId)

    if (!fs.existsSync(outputPath)) {
      fs.mkdirSync(outputPath, { recursive: true })
    }

    const commandToExecute = `${lodGeneratorProgram} "coords" "${basePointer}" ${sceneLodEntitiesManifestBuilder} "${outputPath}"`
    const files: string[] = await new Promise((resolve, reject) => {
      exec(commandToExecute, (_error, _stdout, _stderr) => {
        const generatedFiles = fs.readdirSync(outputPath)
        // if files exists return otherwise reject
        if (generatedFiles.length > 0) {
          resolve(generatedFiles)
        } else {
          reject('Could not generate LODs')
        }
      })
    })

    fs.rmdirSync(outputPath)
    return files
  }

  return { generate }
}
