import fs from 'fs/promises'

import { StorageComponent } from '../../src/types'

export function createInMemoryStorageAdapter(inMemoryStorage: any): StorageComponent {
    async function storeFiles(filePaths: string[], basePointer: string, entityTimestamp: string): Promise<boolean> {
        const files = await Promise.all(filePaths.map((filePath) => fs.readFile(filePath)))
        await Promise.all(
            files.map((file, index) =>
              inMemoryStorage.storeStream(`${basePointer}/LOD/Sources/${entityTimestamp}/${index}.glb`, file)
            )
          )
        
        return true
    }

    return { storeFiles }
}