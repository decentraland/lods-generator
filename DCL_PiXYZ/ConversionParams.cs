using System.Collections.Generic;

namespace DCL_PiXYZ
{
    public struct ConversionParams
    {
        public string SceneHash { get; set; }
        public string ScenePointer { get; set; }
        public Dictionary<string, string> SceneContent { get; set; }
        public int ParcelAmount { get; set; }
        public string ManifestDirectory { get; set; }
        public double DecimationValue { get; set; }
        public int LodLevel { get; set; }
        public string OutputDirectory { get; set; }
        public string DecimationType { get; set; }  
    }
}