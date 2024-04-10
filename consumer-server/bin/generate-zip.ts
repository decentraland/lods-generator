import { createDotEnvConfigComponent } from "@well-known-components/env-config-provider"
import { createCatalystClient } from 'dcl-catalyst-client'
import { createFetchComponent } from '@well-known-components/fetch-component'
import {
  GetObjectCommand,
  ListObjectsV2Command,
  ListObjectsV2Output,
  ListObjectsV2Request,
  S3Client,
} from "@aws-sdk/client-s3"
import * as fs from "fs"
import * as path from "path"
import { Readable } from "stream"
import archiver from "archiver"

const REGION = "us-east-1"
const downloadPath = path.join(__dirname, "output-lods")

async function isActiveScene(fileName: string): Promise<boolean> {
  const fetcher = createFetchComponent()
  const catalyst = await createCatalystClient({ fetcher, url: 'https://peer.decentraland.org' }).getContentClient()
  
  const regexToExtractHash = /LOD\/\d\/([a-z0-9]+)_\d_(mac|windows)$/
  const match = fileName.match(regexToExtractHash)
  if (!match) {
    console.log(`Could not extract hash from ${fileName}, rejecting scene inclusion`)
    return false
  }
  
  const hash = match[1]

  if (hash.startsWith('qm')) {
    return true
  }

  try {
    // throws if the entity is not found
    await catalyst.fetchEntityById(hash)
    return true
  } catch {
    return false
  }
}

async function downloadLODDirectoryAndCompressIt(
  s3Client: S3Client,
  bucket: string,
  directory: string
) {
  const params: ListObjectsV2Request = {
    Bucket: bucket,
    ContinuationToken: undefined,
    Prefix: "LOD/" + directory + "/",
  }

  const downloadDirectory = path.join(downloadPath, directory)
  if (!fs.existsSync(downloadDirectory) || !fs.existsSync(path.join(downloadDirectory, "MAC")) || !fs.existsSync(path.join(downloadDirectory, "WINDOWS")) || !fs.existsSync(path.join(downloadDirectory, "GLB"))){
    // create directories for each platform
    fs.mkdirSync(downloadDirectory, { recursive: true })
    fs.mkdirSync(path.join(downloadDirectory, "MAC"), { recursive: true })
    fs.mkdirSync(path.join(downloadDirectory, "WINDOWS"), { recursive: true })
    fs.mkdirSync(path.join(downloadDirectory, "GLB"), { recursive: true })
  }

  let objectCount = 0
  let fetched: ListObjectsV2Output
  do {
    const command = new ListObjectsV2Command(params)
    fetched = await s3Client.send(command)
    if (fetched.Contents) {
      objectCount += fetched.Contents.length

      for (let object of fetched.Contents) {
        const fileKey = object.Key || "key-not-found"
        let filePath: string
        if (fileKey.endsWith(".br")) {
          continue
        }

        if (!await isActiveScene(fileKey)) {
          console.log('Excluding scene from ZIP because it is not active', fileKey)
          continue
        }

        if (fileKey.toLocaleLowerCase().includes("mac")) {
          filePath = path.join(
            downloadDirectory,
            "MAC",
            path.basename(fileKey)
          )
        } else if (fileKey.toLocaleLowerCase().includes("windows")) {
          filePath = path.join(
            downloadDirectory,
            "WINDOWS",
            path.basename(fileKey)
          )
        } else {
          filePath = path.join(
            downloadDirectory,
            "GLB",
            path.basename(fileKey)
          )
        }

        if (fs.existsSync(filePath)) {
          console.log(`File ${filePath} already exists, skipping`)
          continue
        }

        const data = await s3Client.send(
          new GetObjectCommand({ Bucket: bucket, Key: fileKey })
        )
        if (data.Body) {
          const fileStream = fs.createWriteStream(filePath)
          await new Promise((resolve, reject) => {
            fileStream.on("finish", resolve)
            fileStream.on("error", reject);
            (data.Body as Readable).pipe(fileStream)
          })
          console.log(`Downloaded ${fileKey}`)
        }
      }
    }

    params.ContinuationToken = fetched.NextContinuationToken
  } while (fetched.IsTruncated)

  console.log(`${objectCount} downloaded from bucket ${bucket}`)

  // create three zip files, one for each platform
  await zipFiles(directory, downloadDirectory)
}

async function zipFiles(directory: string, downloadDirectory: string) {
  const platforms = ["MAC", "WINDOWS", "GLB"]
  for (let platform of platforms) {
    const platformDirectory = path.join(downloadDirectory, platform)
    const zipFilePath = path.join(
      downloadPath,
      directory + "_" + platform + ".zip"
    )
    const output = fs.createWriteStream(zipFilePath)
    const archive = archiver("zip", { zlib: { level: 9 } }) // Compression level

    await new Promise((resolve, reject) => {
      output.on("close", () => {
        console.log(
          `Zip file created with total size ${archive.pointer()} bytes`
        )
        resolve(undefined)
      })
      archive.on("error", (err: any) => reject(err))

      archive.pipe(output)
      archive.directory(platformDirectory, false)
      archive.finalize()
    })

    console.log(`objects zipped to ${zipFilePath}`)
  }
}

async function main() {
  // grabs first argument passed from script
  const directory = process.argv[2]

  const config = await createDotEnvConfigComponent({
    path: [".env.default", ".env", ".env.admin"],
  })

  const [user, secret, bucket] = await Promise.all([
    config.requireString("AWS_USER"),
    config.requireString("AWS_SECRET"),
    config.requireString("S3_BUCKET"),
  ])

  const s3Client = new S3Client({
    region: REGION,
    credentials: {
      secretAccessKey: secret,
      accessKeyId: user,
    },
  })

  await downloadLODDirectoryAndCompressIt(s3Client, bucket, directory)
}

main().catch(console.error)
