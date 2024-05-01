import { createDotEnvConfigComponent } from "@well-known-components/env-config-provider"

async function fetchWorldManifest(worldManifestUrl: string): Promise<{ roads: string[]; occupied: string[]; empty: string[] }> {
    const response = await fetch(worldManifestUrl)

    return await response.json()
}

async function queuePointersForLodGeneration(lodServer: string, lodAuthToken: string, batchToPublish: string[]) {
    try {
        const response = await fetch(`${lodServer}/reprocess`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + lodAuthToken
            },
            body: JSON.stringify({ pointers: batchToPublish.map((pointer) => pointer.trim().replace(' ', '')) })
        })

        if (!response.ok) {
            console.error(`Failed to publish batch to LOD server: ${response.statusText} | ${await response.text()}`)

            if (response.statusText === 'Bad Request') {
                console.error('Body failed', { body: JSON.stringify({ pointers: batchToPublish }) })
            }

            throw new Error('Retry')
        }
    } catch (error) {
        console.error(error)
        // sleep 3 seconds
        await new Promise(resolve => setTimeout(resolve, 3000));
        await queuePointersForLodGeneration(lodServer, lodAuthToken, batchToPublish)
    }
}

async function publishToLodServer(lodServer: string, lodAuthToken: string, occupiedPointersSet: Set<string>) {
    // publish on batch of 20 pointers
    const pointers = Array.from(occupiedPointersSet)
    const batchSize = 75
    for (let i = 0; i < pointers.length; i += batchSize) {
        const until = i + batchSize > pointers.length ? pointers.length : i + batchSize
        const batch: string[] = pointers.slice(i, until)
        await queuePointersForLodGeneration(lodServer, lodAuthToken, batch)

        const batchNumber: string = i === 0 ? '1' : (until === pointers.length ? 'Last' : (i / batchSize).toString())
        console.log(`Batch ${batchNumber} published to LOD server`)
    }   
}

async function main() {
    const config = await createDotEnvConfigComponent({
        path: [".env.default", ".env", ".env.admin"],
        })

    const [worldManifestUrl, lodServer, lodAuthToken] = await Promise.all([
        config.requireString("WORLD_MANIFEST_URL"),
        config.requireString("LOD_SERVER"),
        config.requireString("LOD_AUTH_TOKEN")
    ])

    const worldManifest = await fetchWorldManifest(worldManifestUrl)
    const occupiedPointersSet = new Set(worldManifest.occupied)
    await publishToLodServer(lodServer, lodAuthToken, occupiedPointersSet)

    console.log('Script finished')
}

main().catch(console.error)