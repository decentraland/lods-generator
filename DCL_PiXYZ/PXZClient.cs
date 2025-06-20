﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using DCL_PiXYZ.Utils;
using Newtonsoft.Json;
using UnityEngine.Pixyz.API;

namespace DCL_PiXYZ
{
    
    public class PXZClient
    {
        private PiXYZAPI pxz;

        public async Task RunLODBuilder(PXZEntryArgs args)
        {
            //Conversion type can be single or bulk
            //If its single, we pass as many scenes as we want to parse separated by ;
            //If its bulk, a single number will represent a square to parse, going from -value to value.
            //Comment (Juani): Im living bulk implementation for reference, but currently Im invoking the creation of all the scenes through an external program
            //PiXYZ was crashing and exiting the application if it was called form the same program

            //Scenes param is single coordinates or bulk value. Single scenes are separated by 
            var sceneConversionInfo = new SceneConversionInfo(args.DecimationValues, "triangle", "coords", "single", args.SceneToConvert);
            var pathHandler = new SceneConversionPathHandler(args.DebugMode, args.DefaultOutputPath, args.DefaultSceneLodManifestDirectory);

            List<string> roadCoordinates = LoadRoads();
            var convertedScenes = LoadConvertedScenes(args.LoadConvertedScenesFile);
            CreateDirectories(sceneConversionInfo);
            FrameworkInitialization(pathHandler.ManifestProjectDirectory, args.InstallNPM);

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
                var pxzParams = new PXZConversionParams
                {
                    DecimationType = sceneConversionInfo.DecimationType, 
                    ParcelAmount = sceneConversionInfo.SceneImporter.GetCurrentScenePointersList().Length, 
                    SceneContent = sceneConversionInfo.SceneImporter.sceneContent,
                    LodLevel = args.StartingLODLevel
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
            DoManifestCleanup(args.DebugMode, pathHandler);
            ClearResourcesFolder(args.DebugMode, pathHandler);
            pxz.Core.ResetSession();
        }

        private void ClearResourcesFolder(bool isDebug, SceneConversionPathHandler pathHandler)
        {
            if (isDebug)
                return;
            
            if (Directory.Exists(PXZConstants.RESOURCES_DIRECTORY))
                Directory.Delete(PXZConstants.RESOURCES_DIRECTORY, true);
            
        }

        private void DoManifestCleanup(bool isDebug, SceneConversionPathHandler pathHandler)
        {
            if (isDebug)
                return;
          
            if (Directory.Exists(pathHandler.ManifestOutputJsonDirectory))
                Directory.Delete(pathHandler.ManifestOutputJsonDirectory, true);
        }

        private async Task DoConversion(PXZConversionParams pxzParams, SceneConversionInfo sceneConversionInfo, string scene, SceneConversionPathHandler pathHandler)
        {
            var stopwatch = new Stopwatch();

            try
            {
                //Check if they were converted
                stopwatch.Restart();
                FileWriter.WriteToConsole($"BEGIN CONVERTING {scene} WITH {pxzParams.DecimationValue}");
                ConvertScene(pxzParams, pathHandler, sceneConversionInfo);
                stopwatch.Stop();

                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}",
                    stopwatch.Elapsed.Hours, stopwatch.Elapsed.Minutes, stopwatch.Elapsed.Seconds);

                FileWriter.WriteToFile($"{scene}\t{pxzParams.DecimationValue}\t{elapsedTime}" , pathHandler.SuccessFile);
            }
            catch (Exception e)
            {
                FileWriter.WriteToFile($"{scene}\tCONVERSION ERROR {pxzParams.DecimationValue}: {e.Message}", pathHandler.FailFile);
            }
        }

        private bool HasSceneBeenConverted(List<string> convertedScenes, string scene)
        {
            if (convertedScenes.Contains(scene))
            {
                FileWriter.WriteToConsole($"Skipping scene {scene} since its already converted");
                return true;
            }
            return false;
        }

        private async Task<bool> ManifestGeneratedSuccesfully(SceneConversionInfo sceneConversionInfo, SceneConversionPathHandler pathHandler)
        {
            if (File.Exists(pathHandler.ManifestOutputJsonFile))
                return true;

            return await GenerateManifest(pathHandler, sceneConversionInfo,
                new List<string>
                {
                    "manifest file already exists", "Failed to load script"
                });
        }

        private async Task<bool> SceneDefinitionDownloadSuccesfully(SceneConversionInfo sceneConversionInfo, string scene, SceneConversionPathHandler pathHandler)
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

        private bool CheckEmptyScene(string[] currentPointersList, string scene)
        {
            //Check empty scenes
            if (currentPointersList.Length == 0)
            {
                FileWriter.WriteToConsole($"Scene {scene} is empty. Ignoring");
                return true;
            }

            return false;
        }

        private async Task<bool> GenerateManifest(SceneConversionPathHandler pathHandler, SceneConversionInfo sceneConversionInfo, List<string> errorsToIgnore)
        {
            FileWriter.WriteToConsole($"BEGIN MANIFEST GENERATION FOR SCENE {sceneConversionInfo.SceneImporter.GetSceneBasePointer()}");
            string manifestGenerationError = await NPMUtils.RunNPMTool(pathHandler.ManifestProjectDirectory, sceneConversionInfo.SceneType, sceneConversionInfo.SceneImporter.GetSceneBasePointer());

            //TODO: Im adding because there were issues were the file was not fully written after  
            //the NPM tools closes
            await Task.Delay(2000);
            if (File.Exists(pathHandler.ManifestOutputJsonFile))
            {
                if (!string.IsNullOrEmpty(manifestGenerationError))
                {
                    if (!errorsToIgnore.Any(errorToIgnore => manifestGenerationError.Contains(errorToIgnore)))
                    {
                        FileWriter.WriteToConsole($"MANIFEST EXISTS, BUT HAS ERROR: {manifestGenerationError}");
                        FileWriter.WriteToFile($"{sceneConversionInfo.SceneImporter.GetSceneBasePointer()}\tMANIFEST EXISTS, BUT HAS ERROR: {manifestGenerationError}", pathHandler.SuccessFile);
                    }
                }
                return true;
            }
            
            FileWriter.WriteToConsole($"MANIFEST DOES NOT EXIST: {manifestGenerationError}");
            FileWriter.WriteToFile($"{sceneConversionInfo.SceneImporter.GetSceneBasePointer()}\tMANIFEST ERROR: {manifestGenerationError}", pathHandler.FailFile);
            return false; 
        }

        private void ConvertScene(PXZConversionParams pxzParams, SceneConversionPathHandler pathHandler, SceneConversionInfo sceneConversionInfo)
        {
            SceneRepositioner.SceneRepositioner sceneRepositioner =
                new SceneRepositioner.SceneRepositioner(pxzParams.SceneContent, pxz, pathHandler, pxzParams.LodLevel);
            List<PXZModel> models = sceneRepositioner.SetupSceneInPiXYZ();
            
            List<IPXZModifier> modifiers = new List<IPXZModifier>();
            modifiers.Add(new PXZBeginCleanMaterials());
            modifiers.Add(new PXZRepairMesh(models));
            modifiers.Add(new PXZMaterialNameRandomizer());
            modifiers.Add(new PXZDeleteEmptyOccurrences());
            modifiers.Add(new PXZDeleteAllAnimations());
            modifiers.Add(new PXZDeleteInvalidTransforms());

            if (pxzParams.LodLevel != 0)
            {
                modifiers.Add(new PXZDeleteByName(".*collider.*"));
                modifiers.Add(new PXZRemoveHiddenPartOccurrences());
                modifiers.Add(new PXZDecimator(sceneConversionInfo.SceneImporter.GetSceneBasePointer(), pxzParams.DecimationType,
                    pxzParams.DecimationValue, pxzParams.ParcelAmount, pathHandler));
                modifiers.Add(new PXZMergeMeshes(pxzParams.LodLevel));
                
                //Cleanup after merge
                modifiers.Add(new PXZDeleteEmptyOccurrences());
                
                modifiers.Add(new PXZFlattenHierarchy());
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
        
        private bool IsRoad(List<string> roadCoordinates, string currentScene)
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
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SingleParcelRoadCoordinates.json");
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

        private void FrameworkInitialization(string sceneManifestDirectory, bool installAndBuildNPM)
        {
            if (installAndBuildNPM)
            {
                FileWriter.WriteToConsole("INSTALLING AND BUILDING NPM");
                NPMUtils.DoNPMInstall(sceneManifestDirectory);
            }
            FileWriter.WriteToConsole("INITIALIZING PIXYZ");
            InitializePiXYZ();
        }

        private void InitializePiXYZ()
        {
            pxz = PiXYZAPI.Initialize(); 
            
            if (!pxz.Core.CheckLicense())
                pxz.Core.ConfigureLicenseServer(Environment.GetEnvironmentVariable("PIXYZ_IP"), (ushort)int.Parse(Environment.GetEnvironmentVariable("PIXYZ_PORT")));
            
            foreach (string tokenName in pxz.Core.ListTokens().list)
                pxz.Core.NeedToken(tokenName);
        }

        private void CreateDirectories(SceneConversionInfo sceneConversionInfo)
        {
            Directory.CreateDirectory(PXZConstants.RESOURCES_DIRECTORY);
        }

    }
}
