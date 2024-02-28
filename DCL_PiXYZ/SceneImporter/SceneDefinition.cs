using System;
using Newtonsoft.Json;

namespace SceneImporter
{
    [Serializable]
    public class SceneDefinition
    {
        public Content[] content;
        public string[] pointers;
        public string id;
        public Metadata metadata;

        public SceneDefinition()
        {
            pointers = Array.Empty<string>();
        }
    }
    
    [Serializable]
    public class Content
    {
        public string file;
        public string hash;
    }
    
    [Serializable]
    public class Metadata
    {
        public string runtimeVersion;
        public Scene scene;

    }

    [Serializable]
    public class Scene
    {
        public string[] parcels;

        [JsonProperty("base")]
        public string baseParcel;
    }
}