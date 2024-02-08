import { exec } from 'child_process'
import path from 'path'
import os from 'os'
import fs from 'fs'

import { LodGeneratorService } from '../types'

export function createLodGeneratorComponent(): LodGeneratorService {
  const projectRoot = path.resolve(__dirname, '..', '..', '..') // project root according to Dockerfile bundling
  const lodGeneratorProgram = path.join(projectRoot, 'api', 'DCL_PiXYZ.exe') // path to the lod generator program

  async function generate(entityId: string, basePointer: string): Promise<string[]> {
    const outputPath = path.join(os.tmpdir(), "built-lods")

    if (!fs.existsSync(outputPath)) {
      fs.mkdirSync(outputPath, { recursive: true })
    }

    const commandToExecute = `${lodGeneratorProgram} "coords" "${basePointer}" "${outputPath}"`
    const files: string[] = await new Promise((resolve, reject) => {
      exec(commandToExecute, (error, _stdout, stderr) => {
        const generatedFiles = fs.readdirSync(outputPath)
        // if files exists return otherwise reject
        if (generatedFiles.length > 0) {
          resolve(generatedFiles)
        } else {
          reject(new Error(`No files were generated. Error: ${error?.message}, Stderr: ${stderr}`))
        }
      })
    })

    fs.rmdirSync(outputPath)
    return files
  }

  return { generate }
}
