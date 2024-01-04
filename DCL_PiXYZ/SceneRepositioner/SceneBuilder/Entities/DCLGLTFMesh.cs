using System;
using System.Collections.Generic;
using DCL_PiXYZ;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using UnityEngine.Pixyz.API;

namespace AssetBundleConverter.LODs
{
    public class DCLGLTFMesh : DCLMesh
    {

        private string src;

        public DCLGLTFMesh(string src)
        {
            this.src = src;
        }

        public override PXZModel InstantiateMesh(PiXYZAPI pxz, string entityID ,uint parent, uint material,Dictionary<string, string> sceneContent)
        {
            if (sceneContent.TryGetValue(src, out string modelPath))
            {
                try
                {
                    uint importedFileOccurrence = pxz.IO.ImportScene(modelPath);
                    pxz.Scene.SetParent(importedFileOccurrence, parent);
                    return new PXZModel(true, importedFileOccurrence);
                }
                catch(Exception e)
                {
                    Console.WriteLine($"ERROR: Importing GLTF {src} failed with error {e}");
                    return PXYZConstants.EMPTY_MODEL;
                }

            }
            else
            {
                Console.WriteLine($"ERROR: GLTF {src} file not found in sceneContent");
                return PXYZConstants.EMPTY_MODEL;
            }
        }
    }
}
