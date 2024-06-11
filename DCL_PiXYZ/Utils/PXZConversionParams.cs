using System.Collections.Generic;

namespace DCL_PiXYZ.Utils
{
    public struct PXZConversionParams
    {
        public Dictionary<string, string> SceneContent { get; set; }
        public int ParcelAmount { get; set; }
        public double DecimationValue { get; set; }
        public int LodLevel { get; set; }
        public string DecimationType { get; set; }  
    }
}