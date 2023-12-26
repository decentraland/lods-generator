using System;

namespace SceneImporter
{
    [Serializable]
    public class SceneDefinition
    {
        public Content[] content;
    }
    
    [Serializable]
    public class Content
    {
        public string file;
        public string hash;
    }
}