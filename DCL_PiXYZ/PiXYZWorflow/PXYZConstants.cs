using System.IO;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;

namespace DCL_PiXYZ
{
    public class PXYZConstants
    {
        public static string RESOURCES_DIRECTORY = Path.Combine(Directory.GetCurrentDirectory(), "Resources");

        public static PXZModel EMPTY_MODEL = new PXZModel(false, 500000);

        public static string COORDS_PARAM = "coords";
        
        public static string HASH_PARAM = "hash";
        
        public static string CUSTOM_MATERIAL_CONVERTED = "CUSTOM_MATERIAL";


    }
}