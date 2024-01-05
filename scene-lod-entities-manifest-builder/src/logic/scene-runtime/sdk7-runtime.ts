import { LoadableApis } from './apis'

const sceneDebug = true

type GenericRpcModule = Record<string, (...args: any) => Promise<unknown>>

type SceneInterface = {
  onUpdate(dt: number): Promise<void>
  onStart(): Promise<void>
}

type SDK7Module = {
  readonly exports: Partial<SceneInterface>
  runStart(): Promise<void>
  runUpdate(deltaTime: number): Promise<void>
}

// TODO: are we going to provide a WS & Fetch connection for the server scene?
export function createWsFetchRuntime(runtime: Record<string, any>) {
  const restrictedWebSocket = () => {
    throw new Error('No WS')
  }

  const restrictedFetch = () => {
    throw new Error('No fetch')
  }

  Object.defineProperty(runtime, 'WebSocket', {
    configurable: false,
    value: restrictedWebSocket
  })

  Object.defineProperty(runtime, 'fetch', {
    configurable: false,
    value: restrictedFetch
  })
}

export function createModuleRuntime(runtime: Record<string, any>): SDK7Module {
  const exports: Partial<SceneInterface> = {}

  const module = { exports }

  Object.defineProperty(runtime, 'module', {
    configurable: false,
    get() {
      return module
    }
  })

  Object.defineProperty(runtime, 'exports', {
    configurable: false,
    get() {
      return module.exports
    }
  })

  // We don't want to log the scene logs
  Object.defineProperty(runtime, 'console', {
    value: {
      log: () => {},
      info: () => {},
      debug: () => {},
      trace: () => {},
      warning: () => {},
      error: () => {}
    }
  })

  const loadedModules: Record<string, GenericRpcModule> = {}

  Object.defineProperty(runtime, 'require', {
    configurable: false,
    value: (moduleName: string) => {
      if (moduleName in loadedModules) return loadedModules[moduleName]
      const module = loadSceneModule(moduleName)
      loadedModules[moduleName] = module
      return module
    }
  })

  const setImmediateList: Array<() => Promise<void>> = []

  Object.defineProperty(runtime, 'setImmediate', {
    configurable: false,
    value: (fn: () => Promise<void>) => {
      setImmediateList.push(fn)
    }
  })

  async function runSetImmediate(): Promise<void> {
    if (setImmediateList.length) {
      for (const fn of setImmediateList) {
        try {
          await fn()
        } catch (err: any) {
          console.error(err)
        }
      }
      setImmediateList.length = 0
    }
  }

  return {
    get exports() {
      return module.exports
    },
    async runStart() {
      if (module.exports.onStart) {
        await module.exports.onStart()
      }
      await runSetImmediate()
    },
    async runUpdate(deltaTime: number) {
      if (module.exports.onUpdate) {
        await module.exports.onUpdate(deltaTime)
      }
      await runSetImmediate()
    }
  }
}

function loadSceneModule(moduleName: string): GenericRpcModule {
  const moduleToLoad = moduleName.replace(/^~system\//, '')
  if (moduleToLoad in LoadableApis) {
    return (LoadableApis as any)[moduleToLoad]
  } else {
    throw new Error(`Unknown module ${moduleName}`)
  }
}
