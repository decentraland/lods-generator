using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DCL_PiXYZ
{
    public struct PXZParams
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

    public struct SceneConversionInfo
    {
        public string SceneType { get; }
        public string ConversionType { get; }
        public string Scenes { get; }
        public string DecimationType { get; }
        public string DecimationValues { get; }
        public string SceneManifestDirectory { get; }
        public string ScenePositionJsonDirectory { get; }
        public string OutputDirectory { get; }
        public List<string> ScenesToAnalyze { get; set; }
        public List<double> DecimationToAnalyze { get; set; }

        public SceneConversionInfo(string[] args)
        {
            SceneType = args[0];
            ConversionType = args[1];
            Scenes = args[2];
            DecimationType = args[3];
            DecimationValues = args[4];
            SceneManifestDirectory = Path.Combine(Directory.GetCurrentDirectory(), "scene-lod-entities-manifest-builder");
            ScenePositionJsonDirectory = Path.Combine(SceneManifestDirectory, "output-manifests/");
            OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "built-lods");
            ScenesToAnalyze = new List<string>();
            DecimationToAnalyze = new List<double>();
            
            GetScenesToAnalyzeList(ConversionType, Scenes);
            GetDecimationValues(DecimationType, DecimationValues);
        }

        private void GetDecimationValues(string decimationType, string decimationValues)
        {
            if (decimationType.Equals("triangle") || decimationType.Equals("ratio"))
                try
                {
                    DecimationToAnalyze = decimationValues.Split(';').Select(double.Parse).ToList();
                }catch(Exception e)
                {
                    PXZEntryPoint.CloseApplication($"Error: Wrong decimation value param {decimationValues}");
                }
            else 
                PXZEntryPoint.CloseApplication($"Error: Wrong decimation type param {decimationType}");
        }
        
        private void GetScenesToAnalyzeList(string conversionType, string sceneParam)
        {
            if (conversionType.Equals("single"))
                ScenesToAnalyze = sceneParam.Split(';').ToList();
            else if(conversionType.Equals("bulk"))
            {
                if (int.TryParse(sceneParam, out int limitInt))
                {
                    for (int i = -limitInt; i <= limitInt; i++)
                    {
                        for(int j = -limitInt; j <= limitInt; j++)
                        {
                            ScenesToAnalyze.Add($"{i},{j}");
                        }
                    }
                }
                else
                    PXZEntryPoint.CloseApplication($"Error: Wrong scene type param {sceneParam}");
            }
            else
                PXZEntryPoint.CloseApplication($"Error: Wrong conversion type param {conversionType}");
        }
    }
}