import * as path from 'path'
import * as fs from 'fs'
import { glob } from 'glob'

import { IConfigComponent } from '@well-known-components/interfaces'

export async function buildLicense(config: IConfigComponent | undefined, token: string | undefined): Promise<void> {
  const projectRoot = path.resolve(__dirname, '..', '..', '..', '..')
  let licenseKey: string = ''

  if (!!config && !token) {
    licenseKey = (await config.getString('LODS_GENERATOR_LICENSE')) || ''
  } else if (!!token) {
    licenseKey = token
  }

  const filesPath = (await glob("**/pixyzsdk-15042024.lic", { root: projectRoot }))

  if (filesPath.length === 0) {
    throw new Error('License file not found')
  }

  const licenseKeyFile = fs.readFileSync(filesPath[0], 'utf8')
  const replacedLicenseKey = licenseKeyFile.replace('{LICENSE_KEY}', licenseKey)
  fs.writeFileSync(filesPath[0], replacedLicenseKey, 'utf8')
}