using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using DCL_PiXYZ.Utils;
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
            if (args.Length > 0)
            {
                defaultScene = args[1];
                defaultOutputPath = args[2];
            }

            //Conversion type can be single or bulk
            //If its single, we pass as many scenes as we want to parse separated by ;
            //If its bulk, a single number will represent a square to parse, going from -value to value

            //Scenes param is single coordinates or bulk value. Single scenes are separated by 
            var sceneConversionInfo = new SceneConversionInfo("7000;3000;1000", "triangle", "coords", "single", defaultScene, defaultOutputPath);
            var debugInfo = new SceneConversionDebugInfo(defaultOutputPath, "SuccessScenes.txt", "FailScenes.txt", "EmptyScenes.txt", "PolygonCount.txt" , false);

            CreateDirectories(sceneConversionInfo);

            FrameworkInitialization(sceneConversionInfo.SceneManifestDirectory);

            foreach (var scene in sceneConversionInfo.ScenesToAnalyze)
            {
                if (HasSceneBeenAnalyzed(sceneConversionInfo.AnalyzedScenes, scene)) continue;

                sceneConversionInfo.SceneImporter = new Importer(sceneConversionInfo.ConversionType, scene, sceneConversionInfo.WebRequestsHandler);
                if (!await SceneDefinitionDownloadSuccesfully(sceneConversionInfo, scene, debugInfo)) continue;

                if (CheckEmptyScene(sceneConversionInfo.SceneImporter.GetCurrentScenePointersList(), scene, debugInfo)) continue;

                //Add it to the analyzed scenes array
                foreach (string pointer in sceneConversionInfo.SceneImporter.GetCurrentScenePointersList())
                    sceneConversionInfo.AnalyzedScenes.Add(pointer);

                if (CheckFaillingDebugScenes(sceneConversionInfo.SceneImporter.GetCurrentScenePointersList(), scene)) continue;

                if (!await ManifestGeneratedSuccesfully(sceneConversionInfo, debugInfo, scene)) continue;

                if (!await sceneConversionInfo.SceneImporter.DownloadAllContent(debugInfo)) continue;

                Console.WriteLine("BEGIN SCENE CONVERSION FOR SCENE " + scene);
                var pxzParams = new PXZParams
                {
                    DecimationType = sceneConversionInfo.DecimationType, ManifestOutputJSONDirectory = sceneConversionInfo.ManifestOutputJsonDirectory, OutputDirectory = sceneConversionInfo.OutputDirectory, ParcelAmount = sceneConversionInfo.SceneImporter.GetCurrentScenePointersList().Length,
                    SceneContent = sceneConversionInfo.SceneImporter.sceneContent, SceneHash = sceneConversionInfo.SceneImporter.GetSceneHash(), ScenePointer = sceneConversionInfo.SceneImporter.GetScenePointer()
                };
                foreach (var decimationValue in sceneConversionInfo.DecimationToAnalyze)
                {
                    pxz.Core.ResetSession();
                    if (!SceneHasBeenConverted(sceneConversionInfo, decimationValue, scene))
                    {
                        pxzParams.DecimationValue = decimationValue;
                        await DoConversion(pxzParams, sceneConversionInfo, scene, debugInfo);
                    }

                    pxzParams.LodLevel += 1;
                }
                GC.Collect();
                Console.WriteLine("END SCENE CONVERSION FOR SCENE " + scene);
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
                await ConvertScene(sceneConversionInfo.WebRequestsHandler, pxzParams, debugInfo);
                Console.WriteLine($"END CONVERTING {scene} WITH {pxzParams.DecimationValue}");
                stopwatch.Stop();

                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}",
                    stopwatch.Elapsed.Hours, stopwatch.Elapsed.Minutes, stopwatch.Elapsed.Seconds);

                WriteToFile($"{scene}\t{pxzParams.DecimationValue}\t{elapsedTime}" , debugInfo.SuccessFile);
            }
            catch (Exception e)
            {
                WriteToFile($"{scene}\t{pxzParams.DecimationValue}\tCONVERSION ERROR: {e.Message}", debugInfo.FailFile);
            }
        }

        private static bool SceneHasBeenConverted(SceneConversionInfo sceneConversionInfo, double decimationValue, string scene)
        {
            if (Directory.Exists(Path.Combine(sceneConversionInfo.OutputDirectory, Path.Combine(sceneConversionInfo.SceneImporter.GetCurrentScenePointersList()[0], decimationValue.ToString()))))
            {
                Console.WriteLine($"Skipping scene {scene} since its already converted");
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
                WriteToFile($"{scene}\tSCENE DEFINITION DOWNLOAD ERROR: {e.Message}", debugInfo.FailFile);
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
                WriteToFile($"{scene}", debugInfo.EmptyScenesFile);
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
                    WriteToFile($"{sceneValue}\tMANIFEST ERROR: {possibleError}", failFile);
                    return false; // Early exit if the error cannot be ignored.
                }
            }

            Console.WriteLine($"END MANIFEST GENERATION FOR SCENE {sceneValue}");
            return true; // Return true as default, indicating success if no unignorable error was found.
        }

        private static async Task ConvertScene(WebRequestsHandler webRequestsHandler, PXZParams pxzParams, SceneConversionDebugInfo debugInfo)
        {
            SceneRepositioner.SceneRepositioner sceneRepositioner = 
                new SceneRepositioner.SceneRepositioner(webRequestsHandler,
                    pxzParams.ManifestOutputJSONDirectory,
                    $"{pxzParams.SceneHash}-lod-manifest.json", pxzParams.SceneContent, pxz);
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

            modifiers.Add(new PXZExporter(pxzParams, debugInfo.IsDebug));

            PXZStopwatch stopwatch = new PXZStopwatch();
            foreach (var pxzModifier in modifiers)
            {
                stopwatch.Start();
                await pxzModifier.ApplyModification(pxz);
                stopwatch.StopAndPrint(pxzModifier.GetType().Name);
            }
        }
        
        private static void FrameworkInitialization(string sceneManifestDirectory)
        {
            //TODO: Check if build path is correctly copying the scene lod manifest project
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
        }

        public static void WriteToFile(string message, string fileName)
        {
            using (StreamWriter file = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(),fileName), true))
                file.WriteLine(message);
        }
        
        public static void CloseApplication(string errorMessage)
        {
            Console.Error.WriteLine(errorMessage);
            Environment.Exit(1);
        }
    }
}