using System;
using System.Collections.Generic;
using System.Linq;

namespace DCL_PiXYZ.Utils
{
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
}