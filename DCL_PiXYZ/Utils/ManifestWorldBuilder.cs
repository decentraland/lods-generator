using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SceneImporter;

namespace DCL_PiXYZ.Utils
{
    public class ManifestWorldBuilder
    {

        private string activeEntitiesURL;
        private string sceneManifestDirectory;
        private ManifestWorldBuilderResult result;

        public ManifestWorldBuilder()
        {
            sceneManifestDirectory = "../../../../scene-lod-entities-manifest-builder/";
            activeEntitiesURL = "https://peer.decentraland.org/content/entities/active";
        }
        
        public async Task Run()
        {
            List<ManifestWorldBuilderResult> results = new List<ManifestWorldBuilderResult>();
            Console.WriteLine("BUILDING ARRAY");
            List<SceneDefinition> filteredScenes = await BuildSceneArray();
            Console.WriteLine("ARRAY BUILT PROCESSING " + filteredScenes.Count);
            
            //UNCOMMENT IF YOU NEED TO INSTALL
            //NPMUtils.DoNPMInstall(sceneManifestDirectory);
            foreach (var sceneDefinition in filteredScenes)
            {
                bool isSDK7 = false;
                if (!string.IsNullOrEmpty(sceneDefinition.metadata.runtimeVersion))
                {
                    isSDK7  = sceneDefinition.metadata.runtimeVersion.Equals("7");
                }
                Console.WriteLine("ANALYZING COORD " + sceneDefinition.pointers[0]);
                string possibleException  = NPMUtils.RunNPMToolAndReturnExceptionIfPresent(sceneManifestDirectory, sceneDefinition.pointers[0]);
                Console.WriteLine("FINISHED ANALYZING COORD " + sceneDefinition.pointers[0]);
                results.Add(new ManifestWorldBuilderResult(sceneDefinition.pointers[0], !string.IsNullOrEmpty(possibleException), !string.IsNullOrEmpty(possibleException) ? possibleException : "NO ERROR", isSDK7));
            }
            Console.WriteLine($"RESULTS PROCESSED SAVING TO FILE {Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, "manifest-world-builder-results.txt")}");
            
            SaveResultsToFile(results, Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, "manifest-world-builder-results.txt"));
            Console.WriteLine($"RESULTS SAVED");
        }
        
        
        public async Task<List<SceneDefinition>> BuildSceneArray()
        {
            WebRequestsHandler webRequestsHandler = new WebRequestsHandler();

            List<string> allCoords = new List<string>();
            allCoords.Add("-43,-8");
            //for (int i = -150; i <= 150; i++)
            //    for (int j = -150; j <= 150; j++)
            //        allCoords.Add($"{i},{j}");

            List<SceneDefinition> filteredScenes = new List<SceneDefinition>();
            List<string> ignoreScene = new List<string>();

            
            foreach (var coord in allCoords)
            {
                if (ignoreScene.Contains(coord))
                    continue;
                
                string rawSceneDefinition = 
                    await webRequestsHandler.PostRequest(activeEntitiesURL, "{\"pointers\":[\"" + coord + "\"]}");
                List<SceneDefinition> sceneDefinitions
                    = JsonConvert.DeserializeObject<List<SceneDefinition>>(rawSceneDefinition);

                if (sceneDefinitions.Count > 1)
                {
                    filteredScenes.Add(sceneDefinitions[0]);
                    foreach (var sceneDefinitionPointer in sceneDefinitions[0].pointers)
                    {
                        ignoreScene.Add(sceneDefinitionPointer);
                    }
                }
            }
            return filteredScenes;
        }
        
        private void SaveResultsToFile(List<ManifestWorldBuilderResult> results, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var result in results)
                {
                    writer.WriteLine(result.ToString());
                }
            }
        }


        public struct ManifestWorldBuilderResult
        {
            public string pointer;
            public bool isSdk7;
            public bool passed;
            public string exception;

            public ManifestWorldBuilderResult(string pointer, bool passed, string exception, bool isSdk7)
            {
                this.pointer = pointer;
                this.passed = passed;
                this.exception = exception;
                this.isSdk7 = isSdk7;
            }
            
            public override string ToString()
            {
                return $"{pointer}\t{isSdk7}\t{passed}\t{exception}";
            }
        }
    }
}