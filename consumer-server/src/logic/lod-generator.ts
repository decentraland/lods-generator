import { exec } from 'child_process'
import path from 'path'
import os from 'os'
import fs from 'fs'

import { AppComponents, LodGeneratorComponent } from '../types'

export function createLodGeneratorComponent({ logs }: Pick<AppComponents, 'logs'>): LodGeneratorComponent {
  const logger = logs.getLogger('lod-generator')
  const projectRoot = path.resolve(__dirname, '..', '..', '..') // project root according to Dockerfile bundling
  const lodGeneratorProgram = path.join(projectRoot, 'api', 'DCL_PiXYZ.exe') // path to the lod generator program
  const outputPath = path.join(os.tmpdir(), "built-lods")

  async function generate(basePointer: string): Promise<string[]> {

    if (!fs.existsSync(outputPath)) {
      fs.mkdirSync(outputPath, { recursive: true })
    }

    const commandToExecute = `${lodGeneratorProgram} "coords" "${basePointer}" "${outputPath}"`
    const files: string[] = await new Promise((resolve, reject) => {
      exec(commandToExecute, (error, _stdout, stderr) => {
        const processOutput = `${outputPath}/${basePointer}`
        if(!fs.existsSync(processOutput)) {
          logger.error(`No files were generated. Error: ${stderr}`)
          reject(new Error(`No files were generated. Error: ${error?.message}`))
        }

        const generatedFiles = fs.readdirSync(processOutput)
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
