import { exec } from 'child_process'
import path from 'path'
import os from 'os'
import fs from 'fs'

import { LodGeneratorComponent } from '../types'

export function createLodGeneratorComponent(): LodGeneratorComponent {
  const projectRoot = path.resolve(__dirname, '..', '..', '..') // project root according to Dockerfile bundling
  const lodGeneratorProgram = path.join(projectRoot, 'api', 'DCL_PiXYZ.exe') // path to the lod generator program
  const sceneLodEntitiesManifestBuilder = path.join(projectRoot, 'scene-lod') // path to the scene lod entities manifest builder
  const outputPath = path.join(os.tmpdir(), 'built-lods')

  async function generate(basePointer: string): Promise<string[]> {
    let files: string[] = []
    if (!fs.existsSync(outputPath)) {
      fs.mkdirSync(outputPath, { recursive: true })
    }

    const commandToExecute = `${lodGeneratorProgram} "coords" "${basePointer}" "${outputPath}" "${sceneLodEntitiesManifestBuilder}"`
    files = await new Promise((resolve, _) => {
      exec(commandToExecute, (_error, _stdout, _stderr) => {
        const processOutput = `${outputPath}/${basePointer}`
        if (fs.existsSync(processOutput)) {
          const generatedFiles = fs.readdirSync(processOutput)
          // if files exists return otherwise reject
          resolve(generatedFiles.map((file) => `${processOutput}/${file}`))
        } else {
          resolve([])
        }
      })
    })

    return files
  }

  return { generate }
}
