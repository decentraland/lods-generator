import { S3Client } from '@aws-sdk/client-s3'
import { Upload } from '@aws-sdk/lib-storage'
import fs from 'fs/promises'

import { AppComponents } from '../types'

export async function createStorageComponent({ config, logs }: Pick<AppComponents, 'config' | 'logs'>) {
  const logger = logs.getLogger('storage')
  const bucket = await config.getString('BUCKET')
  const region = (await config.getString('AWS_REGION')) || 'us-east-1'
  const s3 = new S3Client({ region })

  async function store(key: string, content: Buffer, contentType: string): Promise<void> {
    const upload = new Upload({
      client: s3,
      params: {
        Bucket: bucket,
        Key: `${key}`,
        Body: content,
        ContentType: contentType
      }
    })
    await upload.done()
  }

  async function storeFiles(filePaths: string[], basePointer: string, entityTimestamp: string): Promise<boolean> {
    try {
      const files = await Promise.all(filePaths.map((filePath) => fs.readFile(filePath)))
      await Promise.all(
        files.map((file, index) =>
          store(`${basePointer}/LOD/Sources/${entityTimestamp}/${index}.glb`, file, 'model/gltf-binary')
        )
      )
      return true
    } catch (error: any) {
      logger.error('Failed while storing files', { error })
      return false
    }
  }

  return { storeFiles }
}
