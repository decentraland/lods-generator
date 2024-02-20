using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using DCL_PiXYZ.Utils;
using Newtonsoft.Json;
using SceneImporter;
using UnityEngine.Pixyz.API;

namespace DCL_PiXYZ
{
    class PXZEntryPoint
    {
        private static PiXYZAPI pxz;
        static async Task Main(string[] args)
        {
            await RunLODBuilder(args);
        }

        private static async Task RunLODBuilder(string[] args)
        {
            string defaultScene = "0,10";
            string defaultOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "built-lods") ;
            string defaultSceneLodManifestDirectory = Path.Combine(Directory.GetCurrentDirectory(), "scene-lod-entities-manifest-builder/");

            bool isDebug = true;

            if (args.Length > 0)
            {
                defaultScene = args[1];
                defaultOutputPath = args[2];
                defaultSceneLodManifestDirectory = args[3];
                isDebug = false;
            }

            //Conversion type can be single or bulk
            //If its single, we pass as many scenes as we want to parse separated by ;
            //If its bulk, a single number will represent a square to parse, going from -value to value

            //Scenes param is single coordinates or bulk value. Single scenes are separated by 
            var sceneConversionInfo = new SceneConversionInfo("7000;3000;1000", "triangle", "coords", "single", defaultScene, defaultOutputPath, defaultSceneLodManifestDirectory);
            var debugInfo = new SceneConversionDebugInfo(defaultOutputPath, "SuccessScenes.txt", "FailScenes.txt", "PolygonCount.txt" , "FailedGLBImport.txt" , defaultScene, isDebug);

            List<string> roadCoordinates = LoadRoads();
            CreateDirectories(sceneConversionInfo);
            FrameworkInitialization(sceneConversionInfo.SceneManifestDirectory);

            foreach (string currentScene in sceneConversionInfo.ScenesToAnalyze)
            {
                if (IsRoad(roadCoordinates, currentScene)) continue;
                
                if (HasSceneBeenAnalyzed(sceneConversionInfo.AnalyzedScenes, currentScene)) continue;

                sceneConversionInfo.SceneImporter = new Importer(sceneConversionInfo.ConversionType, currentScene, sceneConversionInfo.WebRequestsHandler);
                if (!await SceneDefinitionDownloadSuccesfully(sceneConversionInfo, currentScene, debugInfo)) continue;

                if (CheckEmptyScene(sceneConversionInfo.SceneImporter.GetCurrentScenePointersList(), currentScene, debugInfo)) continue;

                //Add it to the analyzed scenes array
                foreach (string pointer in sceneConversionInfo.SceneImporter.GetCurrentScenePointersList())
                    sceneConversionInfo.AnalyzedScenes.Add(pointer);

                if (CheckFaillingDebugScenes(sceneConversionInfo.SceneImporter.GetCurrentScenePointersList(), currentScene)) continue;

              if (!await ManifestGeneratedSuccesfully(sceneConversionInfo, debugInfo, currentScene)) continue;

                if (!await sceneConversionInfo.SceneImporter.DownloadAllContent(debugInfo)) continue;

                Console.WriteLine("BEGIN SCENE CONVERSION FOR SCENE " + currentScene);
                var pxzParams = new PXZParams
                {
                    DecimationType = sceneConversionInfo.DecimationType, ManifestOutputJSONDirectory = sceneConversionInfo.ManifestOutputJsonDirectory, OutputDirectory = Path.Combine(sceneConversionInfo.OutputDirectory, sceneConversionInfo.SceneImporter.GetScenePointer()), ParcelAmount = sceneConversionInfo.SceneImporter.GetCurrentScenePointersList().Length,
                    SceneContent = sceneConversionInfo.SceneImporter.sceneContent, SceneHash = sceneConversionInfo.SceneImporter.GetSceneHash(), ScenePointer = sceneConversionInfo.SceneImporter.GetScenePointer()
                };
                foreach (var decimationValue in sceneConversionInfo.DecimationToAnalyze)
                {
                    pxz.Core.ResetSession();
                    if (isDebug && SceneHasBeenConverted(sceneConversionInfo, decimationValue, currentScene))
                    {
                        pxzParams.LodLevel += 1;
                        continue;
                    }

                    pxzParams.DecimationValue = decimationValue;
                    await DoConversion(pxzParams, sceneConversionInfo, currentScene, debugInfo);
                    pxzParams.LodLevel += 1;
                }
                GC.Collect();
                Console.WriteLine("END SCENE CONVERSION FOR SCENE " + currentScene);
            }
        }

        private static async Task DoConversion(PXZParams pxzParams, SceneConversionInfo sceneConversionInfo, string scene, SceneConversionDebugInfo debugInfo)
        {
            var stopwatch = new Stopwatch();

            try
            {
                //Check if they were converted

                stopwatch.Restart();
                Console.WriteLine($"BEGIN CONVERTING {scene} WITH {pxzParams.DecimationValue}");
                await ConvertScene(pxzParams, debugInfo);
                Console.WriteLine($"END CONVERTING {scene} WITH {pxzParams.DecimationValue}");
                stopwatch.Stop();

                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}",
                    stopwatch.Elapsed.Hours, stopwatch.Elapsed.Minutes, stopwatch.Elapsed.Seconds);

                FileWriter.WriteToFile($"{scene}\t{pxzParams.DecimationValue}\t{elapsedTime}" , debugInfo.SuccessFile);
            }
            catch (Exception e)
            {
                FileWriter.WriteToFile($"{scene}\t{pxzParams.DecimationValue}\tCONVERSION ERROR: {e.Message}", debugInfo.FailFile);
            }
        }

        private static bool SceneHasBeenConverted(SceneConversionInfo sceneConversionInfo, double currentDecimationValue, string currentScene)
        {
            if (Directory.Exists(Path.Combine(sceneConversionInfo.OutputDirectory, Path.Combine(sceneConversionInfo.SceneImporter.GetCurrentScenePointersList()[0], currentDecimationValue.ToString()))))
            {
                Console.WriteLine($"Skipping scene {currentScene} since its already converted");
                return true;
            }

            return false;
        }

        private static async Task<bool> ManifestGeneratedSuccesfully(SceneConversionInfo sceneConversionInfo, SceneConversionDebugInfo debugInfo, string scene)
        {
            return await GenerateManifest(sceneConversionInfo.SceneType, scene, sceneConversionInfo.SceneManifestDirectory,
                new List<string>
                {
                    "manifest file already exists", "Failed to load script"
                }, debugInfo.FailFile);
        }

        private static async Task<bool> SceneDefinitionDownloadSuccesfully(SceneConversionInfo sceneConversionInfo, string scene, SceneConversionDebugInfo debugInfo)
        {
            try
            {
                await sceneConversionInfo.SceneImporter.DownloadSceneDefinition();
            }
            catch (Exception e)
            {
                FileWriter.WriteToFile($"{scene}\tSCENE DEFINITION DOWNLOAD ERROR: {e.Message}", debugInfo.FailFile);
                return false;
            }

            return true;
        }

        private static bool CheckFaillingDebugScenes(string[] currentPointersList, string scene)
        {
            if (currentPointersList[0].Equals("-27,-17") || currentPointersList[0].Equals("-75,-9") || currentPointersList[0].Equals("-5,36") || currentPointersList[0].Equals("16,34"))
            {
                Console.WriteLine($"Skipping scene {scene} because it was causing an exit without exception");
                return true;
            }

            return false;
        }

        private static bool CheckEmptyScene(string[] currentPointersList, string scene, SceneConversionDebugInfo debugInfo)
        {
            //Check empty scenes
            if (currentPointersList.Length == 0)
            {
                Console.WriteLine($"Scene {scene} is empty. Ignoring");
                return true;
            }

            return false;
        }

        private static bool HasSceneBeenAnalyzed(List<string> analyzedScenes, string scene)
        {
            //Check if the scene has already been analyzed (for bulk conversion)
            if (analyzedScenes.Contains(scene))
            {
                Console.WriteLine($"SCENE {scene} HAS ALREADY BEEN ANALYZED");
                return true;
            }

            return false;
        }


        private static async Task<bool> GenerateManifest(string sceneType, string sceneValue, string sceneManifestDirectory, List<string> errorsToIgnore, string failFile)
        {
            Console.WriteLine($"BEGIN MANIFEST GENERATION FOR SCENE {sceneValue}");
            string possibleError = await NPMUtils.RunNPMTool(sceneManifestDirectory, sceneType, sceneValue);

            if (!string.IsNullOrEmpty(possibleError))
            {
                bool isIgnorableError = errorsToIgnore.Any(errorToIgnore => possibleError.Contains(errorToIgnore));
                // If the error is not ignorable, log it and return false.
                if (!isIgnorableError)
                {
                    Console.WriteLine($"MANIFEST ERROR: {possibleError}");
                    FileWriter.WriteToFile($"{sceneValue}\tMANIFEST ERROR: {possibleError}", failFile);
                    return false; // Early exit if the error cannot be ignored.
                }
            }

            Console.WriteLine($"END MANIFEST GENERATION FOR SCENE {sceneValue}");
            return true; // Return true as default, indicating success if no unignorable error was found.
        }

        private static async Task ConvertScene(PXZParams pxzParams, SceneConversionDebugInfo debugInfo)
        {
            SceneRepositioner.SceneRepositioner sceneRepositioner = 
                new SceneRepositioner.SceneRepositioner(
                    pxzParams.ManifestOutputJSONDirectory,
                    $"{pxzParams.SceneHash}-lod-manifest.json", pxzParams.SceneContent, pxz, debugInfo);
            List<PXZModel> models = await sceneRepositioner.SetupSceneInPiXYZ();

            
            List<IPXZModifier> modifiers = new List<IPXZModifier>();
            modifiers.Add(new PXZBeginCleanMaterials());
            modifiers.Add(new PXZRepairMesh(models));

            
            if (pxzParams.LodLevel != 0)
            {
                modifiers.Add(new PXZDeleteByName(".*collider.*"));
                modifiers.Add(new PXZDecimator(pxzParams.ScenePointer, pxzParams.DecimationType,
                    pxzParams.DecimationValue, pxzParams.ParcelAmount, debugInfo));
                modifiers.Add(new PXZMergeMeshes(pxzParams.LodLevel));
            }

            modifiers.Add(new PXZExporter(pxzParams, debugInfo));

            PXZStopwatch stopwatch = new PXZStopwatch();
            foreach (var pxzModifier in modifiers)
            {
                stopwatch.Start();
                await pxzModifier.ApplyModification(pxz);
                stopwatch.StopAndPrint(pxzModifier.GetType().Name);
            }
        }
        
        private static bool IsRoad(List<string> roadCoordinates, string currentScene)
        {
            if (roadCoordinates.Contains(currentScene))
            {
                Console.WriteLine($"Skipping scene {currentScene} since its a road");
                return true;
            }

            return false;
        }

        private static List<string> LoadRoads()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "RoadCoordinates.json");
            return JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(filePath));
        }
        
        private static void FrameworkInitialization(string sceneManifestDirectory)
        {
            Console.WriteLine("INSTALLING AND BUILDING NPM");
            NPMUtils.DoNPMInstall(sceneManifestDirectory);
            Console.WriteLine("END INSTALLING AND BUILDING NPM");
            Console.WriteLine("INITIALIZING PIXYZ");
            InitializePiXYZ();
            Console.WriteLine("END INITIALIZING PIXYZ");
        }

        private static void InitializePiXYZ()
        {
            pxz =
                PiXYZAPI.Initialize("PixyzSDKCSharp",
                    "204dda67aa3ea8bcb22a76bff9aa1224823b253144396405300e235e434c4711591892c19069c7");
            // if no license is found, try to configure a license server
            if (!pxz.Core.CheckLicense())
                pxz.Core.InstallLicense("pixyzsdk-29022024.lic");
        }

        private static void CreateDirectories(SceneConversionInfo sceneConversionInfo)
        {
            Directory.CreateDirectory(PXYZConstants.RESOURCES_DIRECTORY);
            Directory.CreateDirectory(sceneConversionInfo.OutputDirectory);
            Directory.CreateDirectory(Path.Combine(sceneConversionInfo.OutputDirectory, sceneConversionInfo.Scene));
        }

        public static void CloseApplication(string errorMessage)
        {
            Console.Error.WriteLine(errorMessage);
            Environment.Exit(1);
        }
    }
}
