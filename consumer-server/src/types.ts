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
import { Response } from '@well-known-components/interfaces'

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
  messageProcessor: MessageProcessorComponent
  lodGenerator: LodGeneratorComponent
  storage: StorageComponent
  fetcher: IFetchComponent
  sceneFetcher: SceneFetcherComponent
  bundleTriggerer: BundleTriggererComponent
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
  /**
   * This metadata property keeps track of the number of times the same message has been retried.
   * 'undefined' means that the message was not retried yet.
   * @type {(number | undefined)}
   */
  _retry?: number
}

export type QueueComponent = {
  send(message: QueueMessage): Promise<void>
  receiveSingleMessage(): Promise<Message[]>
  deleteMessage(receiptHandle: string): Promise<void>
}

export type QueueWorker = IBaseComponent

export type LodGenerationResult = {
  lodsFiles: string[]
  logFile: string
  error: { message: string; detailedError: string } | undefined
  outputPath: string
}

export type LodGeneratorComponent = {
  generate(basePointer: string): Promise<LodGenerationResult>
}

export type BundleTriggererComponent = {
  queueGeneration(entityId: string, lods: string[], abServer: string): Promise<Response>
}

export type MessageProcessorComponent = {
  process(message: QueueMessage, receiptMessageHandle: string): Promise<void>
}

export type StorageComponent = {
  storeFiles(filePaths: string[], prefix: string): Promise<string[]>
  getFiles(prefix: string): Promise<string[]>
  deleteFailureDirectory(pointer: string): Promise<void>
}

export type SceneFetcherComponent = {
  fetchByPointers(scenePointers: string[]): Promise<any>
}

export enum HealthState {
  Healthy = 1,
  Unhealthy = 2,
  Unused = 3
}
