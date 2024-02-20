// unset:none

using System;
using System.Collections.Generic;
using AssetBundleConverter.LODs;
using DCL_PiXYZ.SceneRepositioner.JsonParsing.Parsers;
using Newtonsoft.Json;
using UnityEngine.Pixyz.API;

namespace DCL_PiXYZ.SceneRepositioner.JsonParsing
{
    [Serializable]
    public class MeshRendererData : ComponentData
    {
        public DCLMesh mesh;
    }

    [JsonConverter(typeof(MeshRendererDataConverter))]
    [Serializable]
    public abstract class DCLMesh
    {
        public abstract PXZModel InstantiateMesh(PiXYZAPI pxz, string entityID,uint parent, uint material,Dictionary<string, string> contentTable, SceneConversionDebugInfo debugInfo);
    }

    public struct PXZModel
    {
        public bool needsRepair;
        public uint modelOcurrence;

        public PXZModel(bool needsRepair, uint modelOcurrence)
        {
            this.needsRepair = needsRepair;
            this.modelOcurrence = modelOcurrence;
        }
    }



}
