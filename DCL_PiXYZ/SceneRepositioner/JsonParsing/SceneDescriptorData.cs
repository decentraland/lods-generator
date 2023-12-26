using System;
using System.Collections.Generic;
using DCL_PiXYZ.SceneRepositioner.JsonParsing.Parsers;
using Newtonsoft.Json;

namespace DCL_PiXYZ.SceneRepositioner.JsonParsing
{
    public class SceneDescriptorData
    {
        [JsonProperty("scene-coords")]
        public List<int> SceneCoords;

        [JsonProperty("rendereable-entities")]
        public List<RenderableEntity> RenderableEntities;
    }

    [Serializable]
    [JsonConverter(typeof(RenderableEntityDataConverter))]
    public class RenderableEntity
    {
        public int entityId;
        public int componentId;
        public string componentName;
        public ComponentData data;
    }

    [Serializable]
    public abstract class ComponentData
    {

    }
}
