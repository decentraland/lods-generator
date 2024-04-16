using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            //UNCOMMENT IF YOU NEED TO INSTALL
            //NPMUtils.DoNPMInstall(sceneManifestDirectory);
            
            string sourcePath = Path.Combine(PXZConstants.RESOURCES_DIRECTORY, "non-empty-scenes.txt"); // Replace with the path to your source file
            string destinationPath = Path.Combine(PXZConstants.RESOURCES_DIRECTORY, "manifest-world-builder-results.txt"); // Replace with the path to your destination file
            
            if (!File.Exists(destinationPath))
            {
                File.Create(destinationPath).Close();
            }

            List<string> analyzedCoords = new List<string>();
            using (StreamReader reader = new StreamReader(sourcePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    analyzedCoords.Add(line);
                }
            }

            string exception;
            string result;
            
            FileWriter.WriteToConsole("About to analyze " + analyzedCoords.Count);

            RandomizeList(analyzedCoords, 12345);

            for (var i = 0; i < analyzedCoords.Count; i++)
            {
                /*(string,bool) npmTask  = await NPMUtils.RunNPMToolAndReturnExceptionIfPresent(sceneManifestDirectory, analyzedCoords[i], 5000);

                if (npmTask.Item2)
                {
                    result = $"{analyzedCoords[i]}\t{true}\tREADER TIMEOUT\tREADER TIMEOUT";
                    FileWriter.WriteToConsole("FINISHED ANALYZING COORD " + i + " READER TIMEOUT");
                }
                else
                {
                    exception = "NO ERROR";
                    if (!string.IsNullOrEmpty(npmTask.Item1))
                        exception = npmTask.Item1;
                    result = $"{analyzedCoords[i]}\t{true}\t{!exception.Equals("NO ERROR")}\t{exception}";
                    FileWriter.WriteToConsole("FINISHED ANALYZING COORD " + i);
                }

                using (StreamWriter writer = new StreamWriter(destinationPath, true))
                    writer.WriteLine(result);

                //Lets collect garbage once in a while
                if (i % 500 == 0)
                    GC.Collect();
                    */
            }

            /*foreach (var sceneDefinition in filteredScenes)
            {
                bool isSDK7 = false;
                if (!string.IsNullOrEmpty(sceneDefinition.metadata.runtimeVersion))
                    isSDK7  = sceneDefinition.metadata.runtimeVersion.Equals("7");
                string possibleException  = NPMUtils.RunNPMToolAndReturnExceptionIfPresent(sceneManifestDirectory, sceneDefinition.pointers[0]);
                FileWriter.WriteToConsole("FINISHED ANALYZING COORD " + sceneDefinition.pointers[0]);
                results.Add(new ManifestWorldBuilderResult(sceneDefinition.pointers[0], !string.IsNullOrEmpty(possibleException), !string.IsNullOrEmpty(possibleException) ? possibleException : "NO ERROR", isSDK7));
            }
            FileWriter.WriteToConsole($"RESULTS PROCESSED SAVING TO FILE {Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, "manifest-world-builder-results.txt")}");

            SaveResultsToFile(results, Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, "manifest-world-builder-results.txt"));
            FileWriter.WriteToConsole($"RESULTS SAVED");*/
        }
        
        private void RandomizeList(List<string> list, int seed)
        {
            Random rng = new Random(seed);

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        public async Task<List<SceneDefinition>> BuildManifestWorldBuilderResult()
        {
            return new List<SceneDefinition>();
            //UNCOMMENT IF YOU NEED TO GENERATE THE LIST
            /*List<ManifestWorldBuilderResult> results = new List<ManifestWorldBuilderResult>();
            FileWriter.WriteToConsole("BUILDING ARRAY");
            List<SceneDefinition> filteredScenes = await BuildSceneArray();
            FileWriter.WriteToConsole("ARRAY BUILT PROCESSING " + filteredScenes.Count);
            SaveSceneFilteredToFile(filteredScenes, Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, "non-empty-scenes.txt"));*/
        }
        
        
        public async Task<List<SceneDefinition>> BuildSceneArray()
        {
            WebRequestsHandler webRequestsHandler = new WebRequestsHandler();

            List<string> allCoords = new List<string>();
            //allCoords.Add($"0,0");
            //allCoords.Add($"-9,-9");
            //allCoords.Add($"100,100");
            //allCoords.Add($"-147,100");

            for (int i = -150; i <= 150; i++)
                for (int j = -150; j <= 150; j++)
                    allCoords.Add($"{i},{j}");

            List<SceneDefinition> filteredScenes = new List<SceneDefinition>();
            List<string> ignoreScene = new List<string>();

            
            foreach (var coord in allCoords)
            {
                if (ignoreScene.Contains(coord))
                    continue;
                string rawSceneDefinition = 
                    await webRequestsHandler.PostRequest(activeEntitiesURL, "{\"pointers\":[\"" + coord + "\"]}");
                FileWriter.WriteToConsole("FINISHED PROCESSING COORD " + coord);

                List<SceneDefinition> sceneDefinitions
                    = JsonConvert.DeserializeObject<List<SceneDefinition>>(rawSceneDefinition);

                if (sceneDefinitions.Count >= 1)
                {
                    filteredScenes.Add(sceneDefinitions[0]);
                    foreach (var sceneDefinitionPointer in sceneDefinitions[0].pointers)
                        ignoreScene.Add(sceneDefinitionPointer);
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
        
        private void SaveSceneFilteredToFile(List<SceneDefinition> results, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var result in results)
                    writer.WriteLine(result.pointers[0]);
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