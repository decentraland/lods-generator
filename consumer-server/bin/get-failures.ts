import { createDotEnvConfigComponent } from "@well-known-components/env-config-provider"
import {
  GetObjectCommand,
  ListObjectsV2Command,
  ListObjectsV2Output,
  ListObjectsV2Request,
  S3Client,
} from "@aws-sdk/client-s3"
import { Readable } from "stream"
import { promises as fs } from 'fs';

const REGION = "us-east-1"

function parseKey(key: string): { pointer: string, file: string } {
    const [_failures, pointer, file] = key.split("/")
    return { pointer, file }
}

async function main() {
    const config = await createDotEnvConfigComponent({
        path: [".env.admin"],
    })

    const [user, secret, bucket, bucketEndpoint] = await Promise.all([
        config.requireString("AWS_USER"),
        config.requireString("AWS_SECRET"),
        config.requireString("S3_BUCKET"),
        config.getString("S3_BUCKET_ENDPOINT")
    ])

    console.log('secrets read', { user, secret, bucket })

    const s3Client = new S3Client({
        region: REGION,
        credentials: {
          secretAccessKey: secret,
          accessKeyId: user,
        },
        endpoint: bucketEndpoint,
    })

    let logContent = ""
    let response: ListObjectsV2Output
    do {
        const params: ListObjectsV2Request = {
            Bucket: bucket,
            Prefix: "failures",
        }
    
        console.log('Listing objects')
        const command = new ListObjectsV2Command(params)
        response = await s3Client.send(command)

        if (response.Contents) {
            // download all objects
            for (const content of response.Contents) {
                const key = content.Key
                let parsedKey: {
                    pointer: string;
                    file: string;
                } | undefined = undefined
    
                if (!key) continue
    
                if (content.LastModified) {
                    const lastModified = new Date(content.LastModified)
                    const now = new Date()
                    const diff = now.getTime() - lastModified.getTime()
                    const days = diff / (1000 * 60 * 60 * 24)
                    console.log('Object modified days ago', days)
                    if (days > 5) continue
                }
                
                parsedKey = parseKey(key)
    
                const getObjectCommand = new GetObjectCommand({
                    Bucket: bucket,
                    Key: key,
                })
    
                // log
                console.log(`Downloading ${key}`)
                const object = await s3Client.send(getObjectCommand)
                let data: any[] = [];
                for await (const chunk of object.Body as Readable) {
                    data.push(chunk);
                }
                const buffer = Buffer.concat(data)
                const failureContent = new TextDecoder().decode(buffer)
                console.log(`Adding ${key} to content`)
                logContent += `${parsedKey.pointer}\t${failureContent}\n\n`
            }
        }

        params.ContinuationToken = response.NextContinuationToken
    } while (response.IsTruncated)

    
    console.log('writing failures.log')
    await fs.writeFile('failures.log', logContent)
}

main().catch(console.error)