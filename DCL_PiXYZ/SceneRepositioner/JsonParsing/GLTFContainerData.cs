// unset:none

using System;
using AssetBundleConverter.LODs;

namespace DCL_PiXYZ.SceneRepositioner.JsonParsing
{
    [Serializable]
    public class GLTFContainerData : ComponentData
    {
        public DCLGLTFMesh mesh;

        public GLTFContainerData(DCLGLTFMesh mesh)
        {
            this.mesh = mesh;
        }

    }
}
