using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using DCL_PiXYZ.Utils;
using SceneImporter;
using UnityEngine.Pixyz.Algo;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Geom;
using Vector2 = System.Numerics.Vector2;

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
            if (args.Length == 0)
            {
                args = new string[]
                {
                    //Are we geting scenes by hash or coord?
                    "coords",
                    //Are we doing single or bulk scenes?
                    //If its single, we pass as many scenes as we want to parse separated by ;
                    //If its bulk, a single number will represent a square to parse, going from -value to value
                    "single",
                    //Third param is single coordinates or bulk value. Single scenes are separated by ;
                    "0,0",
                    //Fourth param is decimation type (ratio or triangle)
                    "triangle",
                    //Fifth param is decimation value, separated by ;
                    "7000"
                };
            }
            
            var sceneType = args[0];
            var conversionType = args[1];
            var scenes = args[2];
            var decimationType = args[3];
            var decimationValues = args[4];
            var sceneManifestDirectory = Path.Combine(Directory.GetCurrentDirectory(), "scene-lod-entities-manifest-builder"); 
            var scenePositionJsonDirectory = Path.Combine(sceneManifestDirectory, "output-manifests/");
            var outputDirectory =  Path.Combine(Directory.GetCurrentDirectory(), "built-lods"); 
            var scenesToAnalyze = GetScenesToAnalyzeList(conversionType, scenes);
            List<double> decimationToAnalyze = GetDecimationValues(decimationType, decimationValues);
            List<string> analyzedScenes = new List<string>();

            FrameworkInitialization(sceneManifestDirectory);
            CreateResourcesDirectory();
            
            WebRequestsHandler webRequestsHandler = new WebRequestsHandler();
            
            int failedConversions = 0;
            int successConversions = 0;
            int totalRetriesForManifestFile = 20;
            string successFile = "SuccessScenes.txt";
            string failFile = "FailScenes.txt";

            Console.WriteLine($"About to convert {scenesToAnalyze.Count} scenes");
            foreach (var scene in scenesToAnalyze)
            {
                //Check if the scene has already been analyzed (for bulk conversion)
                if (analyzedScenes.Contains(scene))
                {
                    Console.WriteLine($"Scene {scene} has already been analyzed");
                    continue;
                }
                
                //Try to import scene and generate scene content
                Importer importer = new Importer(conversionType,scene,webRequestsHandler);
                try
                {
                    await importer.GenerateSceneContent();
                }
                catch (Exception e)
                {
                    CloseApplication($"Error: Unable to generate scene content due to {e.Message}");
                }
                string[] currentPointersList = importer.GetCurrentScenePointersList();
                
                //Check empty scenes
                if (currentPointersList.Length == 0)
                {
                    Console.WriteLine($"Scene {scene} is empty. Ignoring");
                    WriteToFile($"{scene}", "EmptyScenes.txt");
                    continue;
                }
                
                //Check if they were converted
                if (Directory.Exists(Path.Combine(outputDirectory, currentPointersList[0])))
                {
                    Console.WriteLine($"Skipping scene {scene} since its already converted");
                    continue;
                }
                
                //Add it to the analyzed scenes array
                foreach (var pointer in currentPointersList)
                    analyzedScenes.Add(pointer);

                if (currentPointersList[0].Equals("-27,-17") || currentPointersList[0].Equals("-75,-9") || currentPointersList[0].Equals("-5,36") || currentPointersList[0].Equals("16,34"))
                {
                    Console.WriteLine($"Skipping scene {scene} because it was causing an exit without exception");
                    continue;
                }
                
                Console.WriteLine("BEGIN MANIFEST GENERATION FOR SCENE " + scene);
                bool manifestGenerated =  GenerateManifest(sceneType, scene, sceneManifestDirectory, 
                    new List<string>(){"manifest file already exists", "Failed to load script"}, failFile);
                
                if (!manifestGenerated)
                {
                    failedConversions += decimationValues.Length;
                    continue;
                }
                Console.WriteLine("END MANIFEST GENERATION FOR SCENE " + scene);

                return;
                Dictionary<string, string> sceneContent = new Dictionary<string, string>();

                try
                {
                    sceneContent  = await importer.DownloadAllContent();
                }
                catch (Exception e)
                {
                    failedConversions += decimationValues.Length;
                    foreach (var conversionValue in decimationValues)
                        WriteToFile($"{scene}\t{conversionValue}\tDOWNLOAD ERROR: {e.Message}", failFile);
                    continue;
                }

                Console.WriteLine("BEGIN SCENE CONVERSION FOR SCENE " + scene);
                int currentLODLevel = 0;
                foreach (var decimationValue in decimationToAnalyze)
                {
                    pxz.Core.ResetSession();
                    try
                    {
                        Console.WriteLine($"Converting {scene} with {decimationValue}");
                        ConversionParams conversionParams = new ConversionParams()
                        {
                            DecimationType = decimationType,
                            DecimationValue = decimationValue,
                            LodLevel = currentLODLevel,
                            ManifestDirectory = sceneManifestDirectory,
                            OutputDirectory = outputDirectory,
                            ParcelAmount = currentPointersList.Length,
                            SceneContent = sceneContent,
                            SceneHash = importer.GetSceneHash(),
                            ScenePointer = importer.GetScenePointer()
                        };
                        await ConvertScene(webRequestsHandler, conversionParams);
                        Console.WriteLine($"Finished Converting {scene} with {decimationValue}");
                        successConversions++;
                        WriteToFile($"{scene}\t{decimationValue}" , successFile);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("CONVERSION ERROR: " + e.Message);
                        failedConversions++;
                        WriteToFile($"{scene}\t{decimationValue}\tCONVERSION ERROR: {e.Message}", failFile);
                    }
                    currentLODLevel++;
                }
                GC.Collect();
                Console.WriteLine("END SCENE CONVERSION FOR SCENE " + scene);
            }

        }
        private static bool GenerateManifest(string sceneType, string sceneValue,string sceneManifestDirectory, 
            List<string> errorsToIgnore, string failFile)
        {
            string possibleError = NPMUtils.RunNPMTool(sceneManifestDirectory, sceneType, sceneValue);
    
            if (!string.IsNullOrEmpty(possibleError))
            {
                // Check if the error can be ignored based on the provided error list.
                foreach (var errorToIgnore in errorsToIgnore)
                {
                    if (possibleError.Contains(errorToIgnore))
                    {
                        // Return true if error is ignorable.
                        return true;
                    }
                }
                Console.WriteLine($"MANIFEST ERROR: {possibleError}");
                WriteToFile($"{sceneValue}\tMANIFEST ERROR: {possibleError}", failFile);
                // Return false as the error is not ignorable.
                return false;
            }
            // Return true if there's no error in the NPM tool output.
            return true;
        }
        
        private static async Task ConvertScene(WebRequestsHandler webRequestsHandler, ConversionParams conversionParams)
        {
            SceneRepositioner.SceneRepositioner sceneRepositioner = 
                new SceneRepositioner.SceneRepositioner(webRequestsHandler,
                    conversionParams.ManifestDirectory,
                    $"{conversionParams.SceneHash}-lod-manifest.json", conversionParams.SceneContent, pxz);
            List<PXZModel> models = await sceneRepositioner.SetupSceneInPiXYZ();

            List<IPXZModifier> modifiers = new List<IPXZModifier>();
            modifiers.Add(new PXZDeleteByName(".*collider.*"));
            modifiers.Add(new PXZRepairMesh(models));
            modifiers.Add(new PXZDecimator(conversionParams.ScenePointer, conversionParams.DecimationType,
                conversionParams.DecimationValue, conversionParams.ParcelAmount));
            modifiers.Add(new PXZMergeMeshes());
            string filename = $"{conversionParams.SceneHash}_{conversionParams.LodLevel}";
            modifiers.Add(new PXZExporter(Path.Combine(conversionParams.OutputDirectory, $"{conversionParams.ScenePointer}/{conversionParams.DecimationValue}"), filename, ".fbx"));

            PXZStopwatch stopwatch = new PXZStopwatch();
            
            foreach (var pxzModifier in modifiers)
            {
                stopwatch.Start();
                pxzModifier.ApplyModification(pxz);
                stopwatch.StopAndPrint(pxzModifier.GetType().Name);
            }
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
        
        private static void CreateResourcesDirectory() =>
            Directory.CreateDirectory(PXYZConstants.RESOURCES_DIRECTORY);
        
        private static void WriteToFile(string message, string fileName)
        {
            using (StreamWriter file = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(),fileName), true))
                file.WriteLine(message);
        }
        
        private static List<double> GetDecimationValues(string decimationType, string decimationValues)
        {
            List<double> decimationValuesToReturn = new List<double>();

            if (decimationType.Equals("triangle") || decimationType.Equals("ratio"))
                try
                {
                    decimationValuesToReturn = decimationValues.Split(';').Select(double.Parse).ToList();
                }catch(Exception e)
                {
                    CloseApplication($"Error: Wrong decimation value param {decimationValues}");
                }
            else 
                CloseApplication($"Error: Wrong decimation type param {decimationType}");
            return decimationValuesToReturn;
        }
        
        private static List<string> GetScenesToAnalyzeList(string conversionType, string sceneParam)
        {
            List<string> scenes = new List<string>();

            if (conversionType.Equals("single"))
                scenes = sceneParam.Split(';').ToList();
            else if(conversionType.Equals("bulk"))
            {
                if (int.TryParse(sceneParam, out int limitInt))
                {
                    for (int i = -limitInt; i <= limitInt; i++)
                    {
                        for(int j = -limitInt; j <= limitInt; j++)
                        {
                            scenes.Add($"{i},{j}");
                        }
                    }
                }
                else
                    CloseApplication($"Error: Wrong scene type param {sceneParam}");
            }
            else
                CloseApplication($"Error: Wrong conversion type param {conversionType}");
            return scenes;
        }

        private static void CloseApplication(string errorMessage)
        {
            Console.Error.WriteLine(errorMessage);
            Environment.Exit(1);
        }
    }
}