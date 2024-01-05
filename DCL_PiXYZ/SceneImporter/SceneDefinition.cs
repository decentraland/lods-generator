using System;

namespace SceneImporter
{
    [Serializable]
    public class SceneDefinition
    {
        public Content[] content;
        public string[] pointers;
        public string id;
    }
    
    [Serializable]
    public class Content
    {
        public string file;
        public string hash;
    }
}