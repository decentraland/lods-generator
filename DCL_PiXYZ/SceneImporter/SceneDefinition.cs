using System;

namespace SceneImporter
{
    [Serializable]
    public class SceneDefinition
    {
        public Content[] content;
        public string[] pointers;
        public string id;
        public Metadata metadata;
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
    }
}