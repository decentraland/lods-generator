using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;

namespace DCL_PiXYZ
{
    
 
    
    public struct PXZConversionParams
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
        public string OutputPath;
        public string DownloadPath;
        public string ManifestOutputJsonFile;
        public string ManifestOutputJsonDirectory;
        public string ManifestProjectDirectory;

        private readonly string DefaultOutputPath;

        private bool isDebug;

        public SceneConversionPathHandler(bool isDebug, string defaultOutputPath, string manifestProjectDirectory)
        {
            DefaultOutputPath = defaultOutputPath;
            ManifestProjectDirectory = manifestProjectDirectory;
            DownloadPath = PXZConstants.RESOURCES_DIRECTORY;
            this.isDebug = isDebug;

            OutputPath = "";
            ManifestOutputJsonFile = "";
            ManifestOutputJsonDirectory = "";
            
            SuccessFile = "";
            FailFile = "";
            PolygonCountFile = "";
            FailGLBImporterFile = "";
        }

        public void SetOutputPath(SceneImporter sceneSceneImporter)
        {
            string sceneBasePointer = sceneSceneImporter.GetSceneBasePointer();
            DownloadPath = Path.Combine(PXZConstants.RESOURCES_DIRECTORY, sceneBasePointer);
            OutputPath = Path.Combine(DefaultOutputPath, sceneBasePointer);
            ManifestOutputJsonDirectory = Path.Combine(ManifestProjectDirectory, "output-manifests");
            ManifestOutputJsonFile = Path.Combine(ManifestOutputJsonDirectory, sceneSceneImporter.GetSceneHash() + "-lod-manifest.json");

            if (isDebug)
            {
                Directory.CreateDirectory(DefaultOutputPath);
                SuccessFile = Path.Combine(DefaultOutputPath, "SuccessScenes.txt");
                FailFile = Path.Combine(DefaultOutputPath, "FailScenes.txt");
                PolygonCountFile =  Path.Combine(DefaultOutputPath, "PolygonCount.txt");
                FailGLBImporterFile = Path.Combine(DefaultOutputPath, "FailedGLBImport.txt");
            }
            else
            {
                //TODO: Clean this directory issue
                Directory.CreateDirectory(Path.Combine(DefaultOutputPath, sceneBasePointer));
                string pathWithBasePointer = $"{sceneBasePointer}/output.txt";
                SuccessFile = Path.Combine(DefaultOutputPath, pathWithBasePointer);
                FailFile = Path.Combine(DefaultOutputPath, pathWithBasePointer);
                PolygonCountFile =  Path.Combine(DefaultOutputPath, pathWithBasePointer);
                FailGLBImporterFile =  Path.Combine(DefaultOutputPath, pathWithBasePointer);
            }

            Directory.CreateDirectory(DownloadPath);
            Directory.CreateDirectory(DefaultOutputPath);
            Directory.CreateDirectory(OutputPath);
        }
        
    }
}