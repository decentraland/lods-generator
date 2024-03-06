import { S3Client } from '@aws-sdk/client-s3'
import { Upload } from '@aws-sdk/lib-storage'
import fs from 'fs/promises'
import mime from 'mime-types'

import { AppComponents, StorageComponent } from '../types'

export async function createCloudStorageAdapter({ config }: Pick<AppComponents, 'config'>): Promise<StorageComponent> {
  const bucket = await config.getString('BUCKET')
  const bucketEndpoint = (await config.getString('BUCKET_ENDPOINT')) || 'https://s3.amazonaws.com'
  const region = (await config.getString('AWS_REGION')) || 'us-east-1'

  const s3 = new S3Client({ region, endpoint: bucketEndpoint })

  async function storeFiles(filePaths: string[], prefix: string): Promise<void> {
    const files = await Promise.all(
      filePaths.map(async (filePath) => {
        const buffer = await fs.readFile(filePath)
        const name = filePath.split('/').pop()
        const contentType = mime.contentType(name!) || 'application/octet-stream'

        return { buffer, name, contentType }
      })
    )

    await Promise.all(
      files.map(async (file) => {
        const upload = new Upload({
          client: s3,
          params: {
            Bucket: bucket,
            Key: `${prefix}/${file.name}`,
            Body: file.buffer,
            ContentType: file.contentType
          }
        })
        return upload.done()
      })
    )
  }

  return { storeFiles }
}
