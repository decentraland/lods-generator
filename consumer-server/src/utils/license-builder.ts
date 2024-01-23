import path from 'path'
import fs from 'fs'

import { AppComponents } from '../types'

export async function buildLicense({ config, logs }: Pick<AppComponents, 'config' | 'logs'>): Promise<void> {
    const logger = logs.getLogger('license-builder')
    const licenseKey = await config.getString('LODS_GENERATOR_LICENSE') || '' // this is a SSM parameter pulled from AWS
    const projectRoot = path.resolve(__dirname, '..', '..', '..')
    
    try {
        const licenseKeyPath = path.resolve(projectRoot, 'pixyzsdk-29022024.lic')
        const licenseKeyFile = fs.readFileSync(licenseKeyPath, 'utf8')
        const replacedLicenseKey = licenseKeyFile.replace('{LICENSE_KEY}', licenseKey)
    
        fs.writeFileSync(licenseKeyPath, replacedLicenseKey, 'utf8')
        logger.info('PiXYZ license built correctly')
    } catch (err: any) {
        logger.error('Could not build PiXYZ license', {  err: (err.message).replace(licenseKey, '****') || '' })
        throw err
    }
}