import { exec } from 'child_process'
import path from 'path'
import fs from 'fs'
import os from 'os'

import { AppComponents } from '../types'

const projectRoot = path.resolve(__dirname, '..', '..', '..')
const lodGeneratorProgram = path.join(projectRoot, 'api', 'DCL_PiXYZ.exe')
const sceneLodEntitiesManifestBuilder = path.join(projectRoot, 'scene-lod')

export async function validate({ logs }: Pick<AppComponents, 'logs'>): Promise<void> {
  const logger = logs.getLogger('boot-validator')

  logger.info('Validating boot...')

  logger.info('Checking if PiXYZ is present at', { lodGeneratorProgram })
  if (fs.existsSync(lodGeneratorProgram)) {
    logger.info('PiXYZ program found')
  } else {
    logger.error('PiXYZ program not found')
  }

  if (fs.existsSync(sceneLodEntitiesManifestBuilder)) {
    logger.info('Scene LOD Entities directory found')
  } else {
    logger.error('Scene LOD Entities directory not found')
  }

  if (fs.existsSync(lodGeneratorProgram) && fs.existsSync(sceneLodEntitiesManifestBuilder)) {
    logger.info('Executing test case...')
    const outputPath = path.join(os.tmpdir(), 'output');
    if (!fs.existsSync(outputPath)) {
        fs.mkdirSync(outputPath);
    }

    const command = `${lodGeneratorProgram} "coords" "-129,-77" "50" ${sceneLodEntitiesManifestBuilder} "${outputPath}"`
    // trigger exec asynchronously
    await Promise.all([
      new Promise((resolve, _) => {
        logger.info('Triggering command')
        return exec(command, (error, stdout, stderr) => {
          if (error) {
            logger.error(`exec error: ${error}`)
            resolve(false)
          }
          logger.info(`stdout: ${stdout}`)
          logger.error(`stderr: ${stderr}`)
          resolve(true)
        })
      })
    ])

    //   await new Promise((resolve, reject) => {
    //     exec(command, (error, stdout, stderr) => {
    //       if (error) {
    //         logger.error(`exec error: ${error}`)
    //         resolve(false)
    //       }
    //       logger.info(`stdout: ${stdout}`)
    //       logger.error(`stderr: ${stderr}`)
    //       resolve(true)
    //     })
    //   })
    // list files from outputPath
    const files = fs.readdirSync(outputPath)
    logger.info('Output files:', { files: JSON.stringify(files, null, 2) })
  }
    process.exit(0)
}