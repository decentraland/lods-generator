import { serializeCrdtMessages } from './logger'
import { contentFetchBaseUrl, mainCrdt, sceneId, sdk6FetchComponent, sdk6SceneContent } from "../sceneFetcher";
import { writeFile, mkdir } from 'fs'
import { engine, Entity, PutComponentOperation, Transform } from '@dcl/ecs/dist-cjs'
import { ReadWriteByteBuffer } from '@dcl/ecs/dist-cjs/serialization/ByteBuffer'
import {FRAMES_TO_RUN, framesCount} from "../../adapters/scene";

export const manifestFileDir = 'output-manifests'
export const manifestFileNameEnd = '-lod-manifest.json'
let savedManifest = false

let savedData: Uint8Array  = new Uint8Array(0)
function addPlayerEntityTransform() {
  const buffer = new ReadWriteByteBuffer()
  const transform = Transform.create(engine.PlayerEntity)
  Transform.schema.serialize(transform, buffer)
  const transformData = buffer.toCopiedBinary()
  buffer.resetBuffer()
  PutComponentOperation.write(1 as Entity, 1, Transform.componentId, transformData, buffer)
  
  return buffer.toBinary()
}

export const LoadableApis = {

  // Emulating old EnvironmentAPI from browser-interface/kernel at https://github.com/decentraland/unity-renderer/blob/dev/browser-interface/packages/shared/apis/host/EnvironmentAPI.ts#L29%60L77
  // to avoid compilation errors on very old sdk6 scenes when running their eval to generate the manifest.
  EnvironmentApi: {
    isPreviewMode: async () => ({ isPreview: false }),

    getBootstrapData: async () => ({ }),

    getPlatform: async () => ({ }),

    areUnsafeRequestAllowed: async () => ({ }),

    getCurrentRealm: async () => ({ }),

    getExplorerConfiguration: async () => ({ }),

    getDecentralandTime: async () => ({ })
  },
  CommsApi: {
    registerCommsApiServiceServerImplementation: async () => ({})
  },
  EthereumController: {
    registerEthereumControllerServiceServerImplementation: async () => ({})
  },
  EngineApi: {
    sendBatch: async () => ({ events: [] }),

    crdtGetState: async () => ({ hasEntities: mainCrdt !== undefined, data: [addPlayerEntityTransform(), mainCrdt] }),

    crdtSendToRenderer: async ({ data }: { data: Uint8Array }) => {
      if (mainCrdt) {
        data = joinBuffers(mainCrdt, data)
      }
      savedData = joinBuffers(savedData, data)
      if( framesCount < FRAMES_TO_RUN - 1) return;

      if (savedData.length == 0) return

      const outputJSONManifest = JSON.stringify([...serializeCrdtMessages('[msg]: ', savedData)], null, 2)

      mkdir(manifestFileDir, { recursive: true },
          err => { if (err) console.log(err) })

      writeFile(`${manifestFileDir}/${sceneId}${manifestFileNameEnd}`, outputJSONManifest,
          err => { if (err) console.log(err) })
      console.log(outputJSONManifest)
      return { data: data }
    },
    isServer: async () => ({ isServer: true }),
  },
  UserIdentity: {
    async getUserData()   {
      return {
          displayName: "empty",
          publicKey: "empty",
          hasConnectedWeb3: true,
          userId: "empty",
          version: 0,
          avatar: {
            wearables: [],
          }
      }
    },
    getUserPublicKey: async () => ({})
  },
  SignedFetch: {
    getHeaders: async () => ({})
  },
  Runtime: {
    getRealm: () => {
      return { realmInfo: { isPreview: false } }
    },
    // readFile is needed for the adaption-layer bridge to run SDK6 scenes as an SDK7 scene
    readFile: async ({ fileName }: { fileName: String }) => {
      const fileHash = sdk6SceneContent.find(({ file }: any) => file === fileName).hash
      const res = await sdk6FetchComponent.fetch(`${contentFetchBaseUrl}${fileHash}`)
      return {
        content: await res.arrayBuffer()
      }
    },
    async getSceneInformation() {
      return {
        urn: "https://none",
        baseUrl: "https://none",
        content: "https://none",
        metadataJson: JSON.stringify({
          "display":{
            "title":"",
            "favicon":""
          },
          "owner":"",
          "contact":{
            "name":"",
            "email":""
          },
          "main":"bin/game.js",
          "tags":[],
          "scene":{
            "parcels":["-,-"],
            "base":"-,-"
          }
        })
      }
    }
  },
  RestrictedActions: {
    async triggerEmote() {},
    async movePlayerTo() {},
    async changeRealm() {},
    async openExternalUrl() {},
    async openNftDialog() {},
    async setCommunicationsAdapter() {},
    async teleportTo() {},
    async triggerSceneEmote() {}
  },
  CommunicationsController: {
    async send() {},
    async sendBinary() {}
  },
  PortableExperiences: {
    async exit() {},
    async getPortableExperiencesLoaded() {},
    async kill() {},
    async spawn() {}
  },
  UserActionModule: {
    async requestTeleport() {}
  },
  ParcelIdentity: {
    registerParcelIdentityServiceServerImplementation: async () => ({}),
    async getParcel(_req: any, ctx: any) {
      return {
        land: {
          sceneId: '',
          sceneJsonData: '{}',
          baseUrl: '',
          baseUrlBundles: '',
          mappingsResponse: {
            parcelId: '',
            rootCid: '',
            contents: []
          }
        },
        cid: ''
      }
    }
  },
  Players: {
    async getPlayerData(body: any) {
      return {
        avatar: null,
        displayName: "ManifestBuilder",
        hasConnectedWeb3: false,
        publicKey: null,
        userId: 123,
        version: 123
      }
    },
    async getConnectedPlayers() {
      return [{userId: 123}]
    }
  }
}

function joinBuffers(...buffers: ArrayBuffer[]) {
  const finalLength = buffers.reduce((a, b) => a + b.byteLength, 0)
  const tmp = new Uint8Array(finalLength)
  let start = 0
  for (const buffer of buffers) {
    tmp.set(new Uint8Array(buffer), start)
    start += buffer.byteLength
  }
  return tmp
}