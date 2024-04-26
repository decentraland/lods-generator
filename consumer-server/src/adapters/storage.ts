import { DeleteObjectsCommand, ListObjectsV2Command, S3Client } from '@aws-sdk/client-s3'
import { Upload } from '@aws-sdk/lib-storage'
import fs from 'fs/promises'
import mime from 'mime-types'

import { AppComponents, StorageComponent } from '../types'

export async function createCloudStorageAdapter({ config }: Pick<AppComponents, 'config'>): Promise<StorageComponent> {
  const bucket = await config.requireString('BUCKET')
  const bucketEndpoint = (await config.getString('BUCKET_ENDPOINT')) || 'https://s3.amazonaws.com'
  const region = (await config.getString('AWS_REGION')) || 'us-east-1'

  const s3 = new S3Client({ region, endpoint: bucketEndpoint })

  async function getFiles(prefix: string): Promise<string[]> {
    const list = await s3.send(
      new ListObjectsV2Command({
        Bucket: bucket,
        Prefix: prefix
      })
    )

    return (
      list.Contents?.filter((file) => !file.Key?.endsWith('output.txt')).map(
        (file) => `${bucketEndpoint}/${bucket}/${file.Key}`
      ) || []
    )
  }

  async function storeFiles(filePaths: string[], prefix: string): Promise<string[]> {
    const files = await Promise.all(
      filePaths.map(async (filePath) => {
        const buffer = await fs.readFile(filePath)
        const name = filePath.split('/').pop()
        const contentType = mime.contentType(name!) || 'application/octet-stream'

        return { buffer, name, contentType }
      })
    )

    const uploadedFiles = await Promise.all(
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
        return await upload.done().then(() => `${bucketEndpoint}/${bucket}/${prefix}/${file.name}`)
      })
    )

    return uploadedFiles
  }

  async function deleteFailureDirectory(pointer: string): Promise<void> {
    const listParams = {
      Bucket: bucket,
      Prefix: `failures/${pointer}`
    }

    const listedObjects = await s3.send(new ListObjectsV2Command(listParams))

    if (!listedObjects.Contents || listedObjects.Contents.length === 0) return

    const deleteParams = {
      Bucket: bucket,
      Delete: { Objects: [] as { Key: string }[] }
    }

    listedObjects.Contents.forEach(({ Key }) => {
      if (Key) {
        deleteParams.Delete.Objects.push({ Key })
      }
    })

    await s3.send(new DeleteObjectsCommand(deleteParams))

    if (listedObjects.IsTruncated) await deleteFailureDirectory(pointer)
  }

  return { storeFiles, getFiles, deleteFailureDirectory }
}
