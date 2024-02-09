import { S3Client } from '@aws-sdk/client-s3'
import { Upload } from '@aws-sdk/lib-storage'
import fs from 'fs/promises'
import mime from 'mime-types'

import { AppComponents, StorageComponent } from '../types'

export async function createCloudStorageAdapter({
  config,
  logs
}: Pick<AppComponents, 'config' | 'logs'>): Promise<StorageComponent> {
  const logger = logs.getLogger('storage')
  const bucket = await config.getString('BUCKET')
  const bucketEndpoint = (await config.getString('BUCKET_ENDPOINT')) || 'https://s3.amazonaws.com'
  const region = (await config.getString('AWS_REGION')) || 'us-east-1'
  
  const s3 = new S3Client({ region, endpoint: bucketEndpoint })

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
      const files = await Promise.all(filePaths.map(async (filePath) => {
        const buffer = await fs.readFile(filePath)
        const name = filePath.split('/').pop()
        const contentType = mime.contentType(name!) || 'application/octet-stream'
      
        return { buffer, name, contentType }
      }))

      await Promise.all(
        files.map((file) =>
          store(`${basePointer}/LOD/Sources/${entityTimestamp}/${file.name}`, file.buffer, file.contentType)
        )
      )
      return true
    } catch (error: any) {
      logger.error('Failed while storing files', { error: error.message })
      return false
    }
  }

  return { storeFiles }
}
