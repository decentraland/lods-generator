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
            string defaultScene = "5,19";
            string defaultOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "built-lods") ;
            string defaultSceneLodManifestDirectory = Path.Combine(Directory.GetCurrentDirectory(), "scene-lod-entities-manifest-builder/");

            bool isDebug = true;

            if (args.Length > 0)
            {
                defaultScene = args[1];
                defaultOutputPath = args[2];
                defaultSceneLodManifestDirectory = args[3];
                bool.TryParse(args[4], out isDebug);
            }

            //Conversion type can be single or bulk
            //If its single, we pass as many scenes as we want to parse separated by ;
            //If its bulk, a single number will represent a square to parse, going from -value to value

            //Scenes param is single coordinates or bulk value. Single scenes are separated by 
            var sceneConversionInfo = new SceneConversionInfo("7000;3000;1000", "triangle", "coords", "single", defaultScene);
            var pathHandler = new SceneConversionPathHandler(isDebug, defaultOutputPath, defaultSceneLodManifestDirectory, "SuccessScenes.txt", "FailScenes.txt", "PolygonCount.txt" , "FailedGLBImport.txt" , defaultScene);

            List<string> roadCoordinates = LoadRoads();
            CreateDirectories(sceneConversionInfo);
            FrameworkInitialization(pathHandler.ManifestProjectDirectory, isDebug);

            foreach (string currentScene in sceneConversionInfo.ScenesToAnalyze)
            {
                if (IsRoad(roadCoordinates, currentScene)) continue;
                
                if (HasSceneBeenAnalyzed(sceneConversionInfo.AnalyzedScenes, currentScene)) continue;

                sceneConversionInfo.SceneImporter = new SceneImporter(sceneConversionInfo.ConversionType, currentScene, sceneConversionInfo.WebRequestsHandler);
                if (!await SceneDefinitionDownloadSuccesfully(sceneConversionInfo, currentScene, pathHandler)) continue;

                pathHandler.SetOutputPath(sceneConversionInfo.SceneImporter);

                if (HasSceneBeenConverted(pathHandler, currentScene)) continue;

                if (CheckEmptyScene(sceneConversionInfo.SceneImporter.GetCurrentScenePointersList(), currentScene)) continue;

                //Add it to the analyzed scenes array
                foreach (string pointer in sceneConversionInfo.SceneImporter.GetCurrentScenePointersList())
                    sceneConversionInfo.AnalyzedScenes.Add(pointer);

                Console.WriteLine("BEGIN SCENE CONVERSION FOR " + currentScene);
                if (!await ManifestGeneratedSuccesfully(sceneConversionInfo, pathHandler, currentScene)) continue;
                if (!await sceneConversionInfo.SceneImporter.DownloadAllContent(pathHandler)) continue;
                var pxzParams = new PXZParams
                {
                    DecimationType = sceneConversionInfo.DecimationType, ParcelAmount = sceneConversionInfo.SceneImporter.GetCurrentScenePointersList().Length, SceneContent = sceneConversionInfo.SceneImporter.sceneContent
                };
                foreach (var decimationValue in sceneConversionInfo.DecimationToAnalyze)
                {
                    pxz.Core.ResetSession();
                    pxzParams.DecimationValue = decimationValue;
                    await DoConversion(pxzParams, sceneConversionInfo, currentScene, pathHandler);
                    pxzParams.LodLevel += 1;
                }
                GC.Collect();
                Console.WriteLine("END SCENE CONVERSION FOR " + currentScene);
            }

            DoManifestCleanup(isDebug, pathHandler);
        }

        private static void DoManifestCleanup(bool isDebug, SceneConversionPathHandler pathHandler)
        {
            if (isDebug)
                return;

            var dir = new DirectoryInfo(pathHandler.ManifestOutputJsonDirectory);

            foreach (var fi in dir.GetFiles())
                fi.Delete();
        }

        private static async Task DoConversion(PXZParams pxzParams, SceneConversionInfo sceneConversionInfo, string scene, SceneConversionPathHandler pathHandler)
        {
            var stopwatch = new Stopwatch();

            try
            {
                //Check if they were converted
                stopwatch.Restart();
                Console.WriteLine($"BEGIN CONVERTING {scene} WITH {pxzParams.DecimationValue}");
                await ConvertScene(pxzParams, pathHandler, sceneConversionInfo);
                stopwatch.Stop();

                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}",
                    stopwatch.Elapsed.Hours, stopwatch.Elapsed.Minutes, stopwatch.Elapsed.Seconds);

                FileWriter.WriteToFile($"{scene}\t{pxzParams.DecimationValue}\t{elapsedTime}" , pathHandler.SuccessFile);
            }
            catch (Exception e)
            {
                FileWriter.WriteToFile($"{scene}\t{pxzParams.DecimationValue}\tCONVERSION ERROR: {e.Message}", pathHandler.FailFile);
            }
        }

        private static bool HasSceneBeenConverted(SceneConversionPathHandler pathHandler, string scene)
        {
            var d =  new DirectoryInfo(pathHandler.OutputPath);
            if (d.Exists && d.GetFiles().Length > 0)
            {
                Console.WriteLine($"Skipping scene {scene} since its already converted");
                return true;
            }
            return false;
        }

        private static async Task<bool> ManifestGeneratedSuccesfully(SceneConversionInfo sceneConversionInfo, SceneConversionPathHandler pathHandler, string scene)
        {
            if (File.Exists(pathHandler.ManifestOutputJsonFile))
                return true;

            return await GenerateManifest(sceneConversionInfo.SceneType, scene, pathHandler.ManifestProjectDirectory,
                new List<string>
                {
                    "manifest file already exists", "Failed to load script"
                }, pathHandler.FailFile);
        }

        private static async Task<bool> SceneDefinitionDownloadSuccesfully(SceneConversionInfo sceneConversionInfo, string scene, SceneConversionPathHandler pathHandler)
        {
            try
            {
                await sceneConversionInfo.SceneImporter.DownloadSceneDefinition();
            }
            catch (Exception e)
            {
                FileWriter.WriteToFile($"{scene}\tSCENE DEFINITION DOWNLOAD ERROR: {e.Message}", pathHandler.FailFile);
                return false;
            }

            return true;
        }

        private static bool CheckEmptyScene(string[] currentPointersList, string scene)
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
                Console.WriteLine($"Scene {scene} has already been analyzed");
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

            return true; // Return true as default, indicating success if no unignorable error was found.
        }

        private static async Task ConvertScene(PXZParams pxzParams, SceneConversionPathHandler pathHandler, SceneConversionInfo sceneConversionInfo)
        {
            SceneRepositioner.SceneRepositioner sceneRepositioner =
                new SceneRepositioner.SceneRepositioner(pathHandler.ManifestOutputJsonFile, pxzParams.SceneContent, pxz, pathHandler, pxzParams.LodLevel);
            List<PXZModel> models = await sceneRepositioner.SetupSceneInPiXYZ();

            
            List<IPXZModifier> modifiers = new List<IPXZModifier>();
            modifiers.Add(new PXZBeginCleanMaterials());
            modifiers.Add(new PXZRepairMesh(models));

            
            if (pxzParams.LodLevel != 0)
            {
                modifiers.Add(new PXZDeleteByName(".*collider.*"));
                modifiers.Add(new PXZDecimator(sceneConversionInfo.SceneImporter.GetSceneBasePointer(), pxzParams.DecimationType,
                    pxzParams.DecimationValue, pxzParams.ParcelAmount, pathHandler));
                modifiers.Add(new PXZMergeMeshes(pxzParams.LodLevel));
            }

            modifiers.Add(new PXZExporter(pxzParams, pathHandler, sceneConversionInfo));

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

        private static void FrameworkInitialization(string sceneManifestDirectory, bool isDebug)
        {
            Console.WriteLine("INSTALLING AND BUILDING NPM");
            if (!isDebug)
                NPMUtils.DoNPMInstall(sceneManifestDirectory);
            Console.WriteLine("INITIALIZING PIXYZ");
            InitializePiXYZ();
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

        }

        public static void CloseApplication(string errorMessage)
        {
            Console.Error.WriteLine(errorMessage);
            Environment.Exit(1);
        }
    }
}
