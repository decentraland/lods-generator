﻿using System;
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
using UnityEngine.Pixyz.Core;

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
            bool installNPM = true;
            bool runAssetBundleConversion = false;

            if (args.Length > 0)
            {
                defaultScene = args[1];
                defaultOutputPath = args[2];
                defaultSceneLodManifestDirectory = args[3];
                bool.TryParse(args[4], out isDebug);
                bool.TryParse(args[5], out installNPM);
                bool.TryParse(args[6], out runAssetBundleConversion);
            }
            
            

            //Conversion type can be single or bulk
            //If its single, we pass as many scenes as we want to parse separated by ;
            //If its bulk, a single number will represent a square to parse, going from -value to value.
            //Comment (Juani): Im living bulk implementation for reference, but currently Im invoking the creation of all the scenes through an external program
            //PiXYZ was crashing and exiting the application if it was called form the same program

            //Scenes param is single coordinates or bulk value. Single scenes are separated by 
            var sceneConversionInfo = new SceneConversionInfo("7000;3000;1000;500", "triangle", "coords", "single", defaultScene);
            var pathHandler = new SceneConversionPathHandler(isDebug, defaultOutputPath, defaultSceneLodManifestDirectory, "SuccessScenes.txt", "FailScenes.txt", "PolygonCount.txt" , "FailedGLBImport.txt" , defaultScene);

            List<string> roadCoordinates = LoadRoads();
            var convertedScenes = LoadConvertedScenes();
            CreateDirectories(sceneConversionInfo);
            FrameworkInitialization(pathHandler.ManifestProjectDirectory, installNPM);

            foreach (string currentScene in sceneConversionInfo.ScenesToAnalyze)
            {
                if (IsRoad(roadCoordinates, currentScene)) continue;

                if (HasSceneBeenAnalyzed(convertedScenes, currentScene)) continue;

                sceneConversionInfo.SceneImporter = new SceneImporter(sceneConversionInfo.ConversionType, currentScene, sceneConversionInfo.WebRequestsHandler);
                if (!await SceneDefinitionDownloadSuccesfully(sceneConversionInfo, currentScene, pathHandler)) continue;

                if (HasSceneBeenConverted(convertedScenes, sceneConversionInfo.SceneImporter.GetSceneBasePointer())) continue;

                if (CheckEmptyScene(sceneConversionInfo.SceneImporter.GetCurrentScenePointersList(), currentScene)) continue;

                pathHandler.SetOutputPath(sceneConversionInfo.SceneImporter);

                //Add it to the analyzed scenes array
                foreach (string pointer in sceneConversionInfo.SceneImporter.GetCurrentScenePointersList())
                    convertedScenes.Add(pointer);

                Console.WriteLine("BEGIN SCENE CONVERSION FOR " + sceneConversionInfo.SceneImporter.GetSceneBasePointer());
                if (!await ManifestGeneratedSuccesfully(sceneConversionInfo, pathHandler, sceneConversionInfo.SceneImporter.GetSceneBasePointer())) continue;
                if (!await sceneConversionInfo.SceneImporter.DownloadAllContent(pathHandler)) continue;
                var pxzParams = new PXZParams
                {
                    DecimationType = sceneConversionInfo.DecimationType, ParcelAmount = sceneConversionInfo.SceneImporter.GetCurrentScenePointersList().Length, SceneContent = sceneConversionInfo.SceneImporter.sceneContent
                };
                foreach (var decimationValue in sceneConversionInfo.DecimationToAnalyze)
                {
                    pxz.Core.ResetSession();
                    pxzParams.DecimationValue = decimationValue;
                    await DoConversion(pxzParams, sceneConversionInfo, pathHandler);
                    pxzParams.LodLevel += 1;
                }

                if (runAssetBundleConversion)
                {
                    var assetBundleBuilder = new AssetBundleBuilder(Environment.GetEnvironmentVariable("ASSETBUNDLECONVERTERUNITYPATH"), Environment.GetEnvironmentVariable("ASSETBUNDLECONVERTERPATH"));
                    assetBundleBuilder.RunAssetBundleConversion(runAssetBundleConversion, pxzParams.LodLevel, pathHandler, sceneConversionInfo.SceneImporter.GetSceneHash());
                }

                UpdateConvertedScenesFile(isDebug, convertedScenes);
                
                GC.Collect();
                Console.WriteLine("END SCENE CONVERSION FOR " + sceneConversionInfo.SceneImporter.GetSceneBasePointer());
                AssetBundleUtils.RunAssetBundleConversion(runAssetBundleConversion, 4, pathHandler, "bafkreictrpcnce5eoink3tdtrm74vgbniho7afl6xoi46lk3iag2u7aju4");
                UpdateConvertedScenesFile(isDebug, convertedScenes);
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

        private static async Task DoConversion(PXZParams pxzParams, SceneConversionInfo sceneConversionInfo, SceneConversionPathHandler pathHandler)
        {
            var stopwatch = new Stopwatch();

            try
            {
                //Check if they were converted
                stopwatch.Restart();
                Console.WriteLine($"BEGIN CONVERTING {sceneConversionInfo.SceneImporter.GetSceneBasePointer()} WITH {pxzParams.DecimationValue}");
                await ConvertScene(pxzParams, pathHandler, sceneConversionInfo);
                stopwatch.Stop();

                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}",
                    stopwatch.Elapsed.Hours, stopwatch.Elapsed.Minutes, stopwatch.Elapsed.Seconds);

                FileWriter.WriteToFile($"{sceneConversionInfo.SceneImporter.GetSceneBasePointer()}\t{pxzParams.DecimationValue}\t{elapsedTime}" , pathHandler.SuccessFile);
            }
            catch (Exception e)
            {
                FileWriter.WriteToFile($"{sceneConversionInfo.SceneImporter.GetSceneBasePointer()}\t{pxzParams.DecimationValue}\tCONVERSION ERROR: {e.Message}", pathHandler.FailFile);
            }
        }

        private static bool HasSceneBeenConverted(List<string> convertedScenes, string scene)
        {
            if (convertedScenes.Contains(scene))
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

        private static List<string> LoadConvertedScenes()
        {
            string convertedScenePathFile = Path.Combine(Directory.GetCurrentDirectory(), "ConvertedScenes.json");
            if (File.Exists(convertedScenePathFile))
                return JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(convertedScenePathFile));

            return new List<string>();
        }

        private static void UpdateConvertedScenesFile(bool isDebug, List<string> convertedScenes)
        {
            if (!isDebug)
                return;
            
            string convertedScenePathFile = Path.Combine(Directory.GetCurrentDirectory(), "ConvertedScenes.json");
            File.WriteAllText(convertedScenePathFile, JsonConvert.SerializeObject(convertedScenes));
        }

        private static void FrameworkInitialization(string sceneManifestDirectory, bool installAndBuildNPM)
        {
            if (installAndBuildNPM)
            {
                Console.WriteLine("INSTALLING AND BUILDING NPM");
                NPMUtils.DoNPMInstall(sceneManifestDirectory);
            }
            Console.WriteLine("INITIALIZING PIXYZ");
            InitializePiXYZ();
        }

        private static void InitializePiXYZ()
        {
            pxz = PiXYZAPI.Initialize(Environment.GetEnvironmentVariable("PIXYZPRODUCTNAME"), Environment.GetEnvironmentVariable("PIXYZTOKEN")); 

            foreach (string s in pxz.Core.ListTokens().list)
                pxz.Core.AddWantedToken(s);
            
            // if no license is found, try to configure a license server
            if (!pxz.Core.CheckLicense())
                pxz.Core.ConfigureLicenseServer("18.204.36.86", 27000);
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
