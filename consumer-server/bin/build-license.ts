import { buildLicense } from '../src/utils/license-builder'

async function main() {
    const licenseKey = process.argv[2]
    await buildLicense(undefined, licenseKey)
}

main().catch(console.error)