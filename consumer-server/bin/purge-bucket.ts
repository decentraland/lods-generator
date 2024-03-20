import { createDotEnvConfigComponent } from '@well-known-components/env-config-provider'
import {
    DeleteObjectsCommand,
    ListObjectsV2Command,
    ListObjectsV2Output,
    ListObjectsV2Request,
    S3Client
  } from '@aws-sdk/client-s3'

const REGION = 'us-east-1'

async function purge(s3Client: S3Client, bucket: string) {
    const params: ListObjectsV2Request = {
      Bucket: bucket,
      ContinuationToken: undefined,
      Prefix: 'LOD/',
      Delimiter: '/'
    }

    let objectCount = 0
    let fetched: ListObjectsV2Output
    do {
      const command = new ListObjectsV2Command(params)
      fetched = await s3Client.send(command)
      if (fetched.Contents) {
        objectCount += fetched.Contents.length
      }
  
      const keys = fetched.Contents?.map((o: any) => o.Key) || []
      console.log(`About to delete ${keys.length} objects`)

      await s3Client.send(
        new DeleteObjectsCommand({
          Bucket: bucket,
          Delete: {
            Objects: keys.map((Key: string) => ({ Key }))
          }
        })
      )
      params.ContinuationToken = fetched.NextContinuationToken
    } while (fetched.IsTruncated)
  
    console.log(`${objectCount} keys deleted from bucket ${bucket}`)
  }

async function main() {
    const config = await createDotEnvConfigComponent({
      path: ['.env.default', '.env', '.env.admin']
    })
  
    const [user, secret, bucket] = await Promise.all([
      config.requireString('AWS_USER'),
      config.requireString('AWS_SECRET'),
      config.requireString('S3_BUCKET')
    ])
  
    const s3Client = new S3Client({
      region: REGION,
      credentials: {
        secretAccessKey: secret,
        accessKeyId: user
      }
    })
  
    await purge(s3Client, bucket)
  }
  
  main().catch(console.error)