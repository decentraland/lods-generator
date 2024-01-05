import type { IFetchComponent } from '@well-known-components/http-server'
import type { IConfigComponent } from '@well-known-components/interfaces'

// components used in every environment
export type BaseComponents = {
  config: IConfigComponent
  fetch: IFetchComponent
}
