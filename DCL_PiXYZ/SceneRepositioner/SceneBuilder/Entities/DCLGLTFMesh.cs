using System.Collections.Generic;
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

        public override void InstantiateMesh(PiXYZAPI pxz, string entityID ,uint parent, uint material,Dictionary<string, string> sceneContent)
        {
            if (sceneContent.TryGetValue(src, out string modelPath))
            {
                uint baseOccurrence = pxz.Scene.CreateOccurrence($"{src}_BaseTransform", parent); //# set baseOccurrence parent to rootOccurrence
                uint importedFileOccurrence = pxz.IO.ImportScene(modelPath);
                pxz.Scene.SetParent(importedFileOccurrence, baseOccurrence);
            }
        }
    }
}
