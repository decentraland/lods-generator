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
        public abstract void InstantiateMesh(PiXYZAPI pxz, uint parent, uint material,Dictionary<string, string> contentTable);
    }



}
