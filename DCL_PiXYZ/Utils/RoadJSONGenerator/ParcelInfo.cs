namespace DCL_PiXYZ.Utils
{
    using System.Collections.Generic;

    public class ParcelInfo
    {
        public string Type { get; set; }
    }

    public class AtlasJSONResponse
    {
        public bool Ok { get; set; }
        public Dictionary<string, ParcelInfo> Data { get; set; }
    }
}