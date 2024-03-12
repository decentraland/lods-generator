using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SceneImporter;

namespace DCL_PiXYZ
{
    public struct PXZParams
    {
        public Dictionary<string, string> SceneContent { get; set; }
        public int ParcelAmount { get; set; }
        public double DecimationValue { get; set; }
        public int LodLevel { get; set; }
        public string DecimationType { get; set; }  
    }

    public struct SceneConversionInfo
    {
        public string SceneType { get; }
        public string ConversionType { get; }
        public string Scene { get; }
        public string DecimationType { get; }
        public string DecimationValues { get; }
        public List<string> ScenesToAnalyze { get; set; }
        public List<double> DecimationToAnalyze { get; set; }

        public SceneImporter SceneImporter;

        public WebRequestsHandler WebRequestsHandler;

        public SceneConversionInfo(string decimationValues, string decimationType, string sceneType, string conversionType, string scene)
        {
            SceneType = sceneType;
            ConversionType = conversionType;
            Scene = scene;
            DecimationType = decimationType;
            DecimationValues = decimationValues;
            ScenesToAnalyze = new List<string>();
            DecimationToAnalyze = new List<double>();
            SceneImporter = null;
            WebRequestsHandler = new WebRequestsHandler();
            
            GetScenesToAnalyzeList(ConversionType, Scene);
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

    public struct SceneConversionPathHandler
    {
        public string SuccessFile;
        public string FailFile;
        public string PolygonCountFile;
        public string FailGLBImporterFile;
        public string OutputPathWithBasePointer;
        public string OutputPathWithAssetBundle;
        public string ManifestOutputJsonFile;
        public string ManifestOutputJsonDirectory;
        public string ManifestProjectDirectory;

        private readonly string DefaultOutputPath;


        public SceneConversionPathHandler(bool isDebug, string defaultOutputPath, string manifestProjectDirectory, string successFile, string failFile, string vertexCountFile, string failGlbImporterFile, string scene)
        {
            DefaultOutputPath = defaultOutputPath;
            ManifestProjectDirectory = manifestProjectDirectory;
            OutputPathWithAssetBundle = Path.Combine(DefaultOutputPath, "built-lods-ab");
            if (isDebug)
            {
                SuccessFile = Path.Combine(defaultOutputPath, successFile);
                FailFile = Path.Combine(defaultOutputPath, failFile);
                PolygonCountFile =  Path.Combine(defaultOutputPath, vertexCountFile);
                FailGLBImporterFile = Path.Combine(defaultOutputPath, failGlbImporterFile);
            }
            else
            {
                string pathWithBasePointer = $"{scene}/output.txt";
                SuccessFile = Path.Combine(defaultOutputPath, pathWithBasePointer);
                FailFile = Path.Combine(defaultOutputPath, pathWithBasePointer);
                PolygonCountFile =  Path.Combine(defaultOutputPath, pathWithBasePointer);
                FailGLBImporterFile =  Path.Combine(defaultOutputPath, pathWithBasePointer);
            }

            OutputPathWithBasePointer = "";
            ManifestOutputJsonFile = "";
            ManifestOutputJsonDirectory = "";
        }

        public void SetOutputPath(SceneImporter sceneSceneImporter)
        {
            OutputPathWithBasePointer = Path.Combine(DefaultOutputPath, sceneSceneImporter.GetSceneBasePointer());
            ManifestOutputJsonDirectory = Path.Combine(ManifestProjectDirectory, "output-manifests");
            ManifestOutputJsonFile = Path.Combine(ManifestOutputJsonDirectory, sceneSceneImporter.GetSceneHash() + "-lod-manifest.json");

            Directory.CreateDirectory(DefaultOutputPath);
            Directory.CreateDirectory(OutputPathWithBasePointer);
        }
        
    }
}