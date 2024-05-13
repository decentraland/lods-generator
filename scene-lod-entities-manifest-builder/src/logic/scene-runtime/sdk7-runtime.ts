import { LoadableApis } from './apis'

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

  Object.defineProperty(runtime, 'fetch', {
    value: async (url: string, init: any) => {
      console.log({ url, init })
      return { status: 200, json: async () => {{ }}, text: async () => '' }
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
        try {
          await module.exports.onStart()
        } catch (e) {
          console.log('[onStart error]: ', e)
        }
      }
      await runSetImmediate()
    },
    async runUpdate(deltaTime: number) {
      if (module.exports.onUpdate) {
        try {
          await module.exports.onUpdate(deltaTime)
        } catch (e) {
          console.log('[onUpdate error]: ', e)
        }
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
