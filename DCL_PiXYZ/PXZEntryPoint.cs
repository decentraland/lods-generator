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
            bool installNPM = false;
            bool loadConvertedScenesFile = false;
            string decimationValues = "7000;500";
            int startingLODLevel = 0;


            if (args.Length > 0)
            {
                defaultScene = args[1];
                defaultOutputPath = args[2];
                defaultSceneLodManifestDirectory = args[3];
                bool.TryParse(args[4], out isDebug);
                bool.TryParse(args[5], out installNPM);
                decimationValues = args[6];
                int.TryParse(args[7], out startingLODLevel);
            }


            //Conversion type can be single or bulk
            //If its single, we pass as many scenes as we want to parse separated by ;
            //If its bulk, a single number will represent a square to parse, going from -value to value.
            //Comment (Juani): Im living bulk implementation for reference, but currently Im invoking the creation of all the scenes through an external program
            //PiXYZ was crashing and exiting the application if it was called form the same program

            //Scenes param is single coordinates or bulk value. Single scenes are separated by 
            var sceneConversionInfo = new SceneConversionInfo(decimationValues, "triangle", "coords", "single", defaultScene);
            var pathHandler = new SceneConversionPathHandler(isDebug, defaultOutputPath, defaultSceneLodManifestDirectory, "SuccessScenes.txt", "FailScenes.txt", "PolygonCount.txt" , "FailedGLBImport.txt" , defaultScene);

            List<string> roadCoordinates = LoadRoads();
            var convertedScenes = LoadConvertedScenes(loadConvertedScenesFile);
            CreateDirectories(sceneConversionInfo);
            FrameworkInitialization(pathHandler.ManifestProjectDirectory, installNPM);

            foreach (string currentScene in sceneConversionInfo.ScenesToAnalyze)
            {
                if (IsRoad(roadCoordinates, currentScene)) continue;

                if (HasSceneBeenConverted(convertedScenes, currentScene)) continue;

                sceneConversionInfo.SceneImporter = new SceneImporter(sceneConversionInfo.ConversionType, currentScene, sceneConversionInfo.WebRequestsHandler, pathHandler);
                if (!await SceneDefinitionDownloadSuccesfully(sceneConversionInfo, currentScene, pathHandler)) continue;

                if (HasSceneBeenConverted(convertedScenes, sceneConversionInfo.SceneImporter.GetSceneBasePointer())) continue;

                if (CheckEmptyScene(sceneConversionInfo.SceneImporter.GetCurrentScenePointersList(), currentScene)) continue;

                pathHandler.SetOutputPath(sceneConversionInfo.SceneImporter);

                //Add it to the analyzed scenes array
                foreach (string pointer in sceneConversionInfo.SceneImporter.GetCurrentScenePointersList())
                    convertedScenes.Add(pointer);

                FileWriter.WriteToConsole("BEGIN SCENE CONVERSION FOR " + currentScene);
                if (!await ManifestGeneratedSuccesfully(sceneConversionInfo, pathHandler)) continue;
         
                if (!await sceneConversionInfo.SceneImporter.DownloadAllContent(pathHandler)) continue;
                var pxzParams = new PXZParams
                {
                    DecimationType = sceneConversionInfo.DecimationType, 
                    ParcelAmount = sceneConversionInfo.SceneImporter.GetCurrentScenePointersList().Length, 
                    SceneContent = sceneConversionInfo.SceneImporter.sceneContent,
                    LodLevel = startingLODLevel
                };
                foreach (var decimationValue in sceneConversionInfo.DecimationToAnalyze)
                {
                    //TODO (Juani) : PIXYZ very weird bug. IF this await is not here, two run can output different results. Visible in -48,33
                    await Task.Delay(1000);
                    pxz.Core.ResetSession();
                    pxzParams.DecimationValue = decimationValue;
                    await DoConversion(pxzParams, sceneConversionInfo, currentScene, pathHandler);
                    pxzParams.LodLevel += 1;
                }
                GC.Collect();
                FileWriter.WriteToConsole("END SCENE CONVERSION FOR " + currentScene);
                UpdateConvertedScenesFile(convertedScenes);
            }
            //TODO (Juani): Clear  resources folder
            DoManifestCleanup(isDebug, pathHandler);
            pxz.Core.ResetSession();
        }

        private static void DoManifestCleanup(bool isDebug, SceneConversionPathHandler pathHandler)
        {
            if (!isDebug)
                return;
            
            if (string.IsNullOrEmpty(pathHandler.ManifestOutputJsonDirectory) 
                || Directory.Exists(pathHandler.ManifestOutputJsonDirectory)) return;
            
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
                FileWriter.WriteToConsole($"BEGIN CONVERTING {scene} WITH {pxzParams.DecimationValue}");
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

        private static bool HasSceneBeenConverted(List<string> convertedScenes, string scene)
        {
            if (convertedScenes.Contains(scene))
            {
                FileWriter.WriteToConsole($"Skipping scene {scene} since its already converted");
                return true;
            }
            return false;
        }

        private static async Task<bool> ManifestGeneratedSuccesfully(SceneConversionInfo sceneConversionInfo, SceneConversionPathHandler pathHandler)
        {
            if (File.Exists(pathHandler.ManifestOutputJsonFile))
                return true;

            return await GenerateManifest(pathHandler, sceneConversionInfo,
                new List<string>
                {
                    "manifest file already exists", "Failed to load script"
                });
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
                FileWriter.WriteToConsole($"Scene {scene} is empty. Ignoring");
                return true;
            }

            return false;
        }

        private static async Task<bool> GenerateManifest(SceneConversionPathHandler pathHandler, SceneConversionInfo sceneConversionInfo, List<string> errorsToIgnore)
        {
            FileWriter.WriteToConsole($"BEGIN MANIFEST GENERATION FOR SCENE {sceneConversionInfo.SceneImporter.GetSceneBasePointer()}");
            string possibleError = await NPMUtils.RunNPMTool(pathHandler.ManifestProjectDirectory, sceneConversionInfo.SceneType, sceneConversionInfo.SceneImporter.GetSceneBasePointer());

            //TODO: Im adding because there were issues were the file was not fully written after  
            //the NPM tools closes
            await Task.Delay(2000);
            if (File.Exists(pathHandler.ManifestOutputJsonFile))
            {
                bool isIgnorableError = errorsToIgnore.Any(errorToIgnore => !string.IsNullOrEmpty(possibleError) && possibleError.Contains(errorToIgnore));
                // If the error is not ignorable, log it and return false.
                if (!isIgnorableError)
                {
                    FileWriter.WriteToConsole($"MANIFEST EXISTS, BUT HAS ERROR: {possibleError}");
                    FileWriter.WriteToFile($"{sceneConversionInfo.SceneImporter.GetSceneBasePointer()}\tMANIFEST EXISTS, BUT HAS ERROR: {possibleError}", pathHandler.SuccessFile);
                }
                return true;
            }
            
            FileWriter.WriteToConsole($"MANIFEST DOES NOT EXIST: {possibleError}");
            FileWriter.WriteToFile($"{sceneConversionInfo.SceneImporter.GetSceneBasePointer()}\tMANIFEST ERROR: {possibleError}", pathHandler.FailFile);
            return false; 
        }

        private static async Task ConvertScene(PXZParams pxzParams, SceneConversionPathHandler pathHandler, SceneConversionInfo sceneConversionInfo)
        {
            SceneRepositioner.SceneRepositioner sceneRepositioner =
                new SceneRepositioner.SceneRepositioner(pxzParams.SceneContent, pxz, pathHandler, pxzParams.LodLevel);
            List<PXZModel> models = await sceneRepositioner.SetupSceneInPiXYZ();
            
            List<IPXZModifier> modifiers = new List<IPXZModifier>();
            modifiers.Add(new PXZBeginCleanMaterials());
            modifiers.Add(new PXZRepairMesh(models));
            modifiers.Add(new PXZMaterialNameRandomizer());
            
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
                FileWriter.WriteToConsole($"BEGIN {pxzModifier.GetType().Name}");
                pxzModifier.ApplyModification(pxz);
                FileWriter.WriteToConsole($"FINISHED {pxzModifier.GetType().Name}");
                stopwatch.StopAndPrint(pxzModifier.GetType().Name);
            }
        }
        
        private static bool IsRoad(List<string> roadCoordinates, string currentScene)
        {
            if (roadCoordinates.Contains(currentScene))
            {
                FileWriter.WriteToConsole($"Skipping scene {currentScene} since its a road");
                return true;
            }

            return false;
        }

        private static List<string> LoadRoads()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "RoadCoordinates.json");
            return JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(filePath));
        }

        private static List<string> LoadConvertedScenes(bool loadConvertedScenes)
        {
            if (!loadConvertedScenes)
                return new List<string>();
            
            string convertedScenePathFile = Path.Combine(Directory.GetCurrentDirectory(), "ConvertedScenes.json");
            if (File.Exists(convertedScenePathFile))
                return JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(convertedScenePathFile));
            return new List<string>();
        }

        private static void UpdateConvertedScenesFile(List<string> convertedScenes)
        {
            string convertedScenePathFile = Path.Combine(Directory.GetCurrentDirectory(), "ConvertedScenes.json");
            File.WriteAllText(convertedScenePathFile, JsonConvert.SerializeObject(convertedScenes));
        }

        private static void FrameworkInitialization(string sceneManifestDirectory, bool installAndBuildNPM)
        {
            if (installAndBuildNPM)
            {
                FileWriter.WriteToConsole("INSTALLING AND BUILDING NPM");
                NPMUtils.DoNPMInstall(sceneManifestDirectory);
            }
            FileWriter.WriteToConsole("INITIALIZING PIXYZ");
            InitializePiXYZ();
        }

        private static void InitializePiXYZ()
        {
            pxz = PiXYZAPI.Initialize(Environment.GetEnvironmentVariable("PIXYZPRODUCTNAME"), Environment.GetEnvironmentVariable("PIXYZTOKEN")); 

            if (!pxz.Core.CheckLicense())
                pxz.Core.InstallLicense("pixyz_license_decentraland.bin");
        }

        private static void CreateDirectories(SceneConversionInfo sceneConversionInfo)
        {
            Directory.CreateDirectory(PXZConstants.RESOURCES_DIRECTORY);
        }


        public static void CloseApplication(string errorMessage)
        {
            Console.Error.WriteLine(errorMessage);
            Environment.Exit(1);
        }
    }
}
