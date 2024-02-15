import type {
  IConfigComponent,
  ILoggerComponent,
  IHttpServerComponent,
  IBaseComponent,
  IFetchComponent,
  IMetricsComponent
} from '@well-known-components/interfaces'
import { metricDeclarations } from './metrics'

import { Message } from '@aws-sdk/client-sqs'

export type GlobalContext = {
  components: BaseComponents
}

export type BaseComponents = {
  config: IConfigComponent
  logs: ILoggerComponent
  server: IHttpServerComponent<GlobalContext>
  metrics: IMetricsComponent<keyof typeof metricDeclarations>
  queue: QueueComponent
  messageConsumer: QueueWorker
  lodGenerator: LodGeneratorComponent
  messageHandler: MessageHandlerComponent
  storage: StorageComponent
  fetcher: IFetchComponent
  sceneFetcher: SceneFetcherComponent
}

// components used in runtime
export type AppComponents = BaseComponents & {
  statusChecks: IBaseComponent
}

// components used in tests
export type TestComponents = BaseComponents & {
  // A fetch component that only hits the test server
  localFetch: IFetchComponent
}

// this type simplifies the typings of http handlers
export type HandlerContextWithPath<
  ComponentNames extends keyof AppComponents,
  Path extends string = any
> = IHttpServerComponent.PathAwareContext<
  IHttpServerComponent.DefaultContext<{
    components: Pick<AppComponents, ComponentNames>
  }>,
  Path
>

export type Context<Path extends string = any> = IHttpServerComponent.PathAwareContext<GlobalContext, Path>

export type QueueMessage = {
  entity: {
    entityType: string
    entityId: string
    entityTimestamp: number
    metadata: {
      scene: {
        base: string
      }
    }
  }
}

export type QueueComponent = {
  send(message: QueueMessage): Promise<void>
  receiveSingleMessage(): Promise<Message[]>
  deleteMessage(receiptHandle: string): Promise<void>
}

export type QueueWorker = IBaseComponent

export type LodGeneratorComponent = {
  generate(basePointer: string): Promise<string[] | undefined>
}

export type MessageHandlerComponent = {
  handle(message: QueueMessage): Promise<void>
}

export type StorageComponent = {
  storeFiles(filePaths: string[], basePointer: string, entityTimestamp: string): Promise<boolean>
}

export type SceneFetcherComponent = {
  fetchByPointers(scenePointers: string[]): Promise<any>
}
