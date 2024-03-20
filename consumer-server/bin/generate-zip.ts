import { createDotEnvConfigComponent } from '@well-known-components/env-config-provider'
import {
    GetObjectCommand,
    ListObjectsV2Command,
    ListObjectsV2Output,
    ListObjectsV2Request,
    S3Client
  } from '@aws-sdk/client-s3'
import * as fs from 'fs'
import * as path from 'path'
import { Readable } from "stream"
import * as archiver from 'archiver'

const REGION = 'us-east-1'
const downloadPath = path.join(__dirname, 'lods')

async function zipDirectory(s3Client: S3Client, bucket: string, directory: string) {
    const params: ListObjectsV2Request = {
        Bucket: bucket,
        ContinuationToken: undefined,
        Prefix: 'LOD/' + directory + '/'
    }

    const downloadDirectory = path.join(downloadPath, directory)
    if (!fs.existsSync(downloadDirectory)) {
        fs.mkdirSync(downloadDirectory, { recursive: true })
    } else {
        fs.rmSync(downloadDirectory, { recursive: true })
    }

    let objectCount = 0
    let fetched: ListObjectsV2Output
    do {
      const command = new ListObjectsV2Command(params)
      fetched = await s3Client.send(command)
      if (fetched.Contents) {
        objectCount += fetched.Contents.length
        
        for (let object of fetched.Contents) {
          const fileKey = object.Key || 'key-not-found'
          const filePath = path.join(downloadDirectory, path.basename(fileKey));
          
          const data = await s3Client.send(new GetObjectCommand({ Bucket: bucket, Key: fileKey }))
          if (data.Body) {
            const fileStream = fs.createWriteStream(filePath);
            await new Promise((resolve, reject) => {
                fileStream.on('finish', resolve);
                fileStream.on('error', reject);
                (data.Body as Readable).pipe(fileStream);
              });
            console.log(`Downloaded ${fileKey}`);
          }
        }
      }
  

      params.ContinuationToken = fetched.NextContinuationToken
    } while (fetched.IsTruncated)
  
    console.log(`${objectCount} downloaded from bucket ${bucket}`)

    const zipFilePath = path.join(downloadPath, directory + '.zip')
    const output = fs.createWriteStream(zipFilePath)
    const archive = archiver('zip', { zlib: { level: 9 } }) // Compression level
    
    await new Promise((resolve, reject) => {
            output.on('close', () => {
                console.log(`Zip file created with total size ${archive.pointer()} bytes`);
                resolve(undefined)
            })
            archive.on('error', (err) => reject(err))
        
            archive.pipe(output)
            archive.directory(downloadDirectory, false)
            archive.finalize()
    });

    console.log(`objects zipped to ${zipFilePath}`)
}

async function main() {
    // grabs first argument passed from script
    const directory = process.argv[2]

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
    
      await zipDirectory(s3Client, bucket, directory)
}

main().catch(console.error)