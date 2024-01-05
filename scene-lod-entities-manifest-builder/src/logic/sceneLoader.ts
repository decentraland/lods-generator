import { createSceneComponent } from '../adapters/scene'
import {getGameDataFromLocalScene, getGameDataFromRemoteSceneByCoords, getGameDataFromRemoteSceneByID} from './sceneFetcher'
import { BaseComponents } from '../types'

export async function loadOrReload({ config, fetch }: BaseComponents, loadingType: string, targetScene: string, doneBySceneID : boolean) {
  let hash: string
  let sourceCode: string
  if (loadingType === 'localScene') {
    sourceCode = await getGameDataFromLocalScene(targetScene)
    hash = 'localScene'
  } else {
    if(doneBySceneID){
      sourceCode = await getGameDataFromRemoteSceneByID(fetch, targetScene)
    }else{
      sourceCode = await getGameDataFromRemoteSceneByCoords(fetch, targetScene)
    }
    hash = 'remoteScene'
  }

  const scene = await createSceneComponent()
  console.log(`${loadingType} source code loaded, starting scene`)

  scene.start(hash, sourceCode).catch(console.error)
}
