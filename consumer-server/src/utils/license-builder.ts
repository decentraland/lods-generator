import * as path from 'path'
import * as fs from 'fs'

import { IConfigComponent } from '@well-known-components/interfaces'

export async function buildLicense(config: IConfigComponent | undefined, token: string | undefined): Promise<void> {
  const projectRoot = path.resolve(__dirname, '..', '..', '..')
  let licenseKey: string = ''

  if (!!config && !token) {
      licenseKey = await config.requireString('LODS_GENERATOR_LICENSE')
  } else if (!!token) {
        licenseKey = token
  }

  try {
    const licenseKeyPath = path.resolve(projectRoot, 'pixyzsdk-15042024.lic')
    const licenseKeyFile = fs.readFileSync(licenseKeyPath, 'utf8')
    const replacedLicenseKey = licenseKeyFile.replace('{LICENSE_KEY}', licenseKey)

    fs.writeFileSync(licenseKeyPath, replacedLicenseKey, 'utf8')
  } catch (error: any) {
    throw error
  }
}