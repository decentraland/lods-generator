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
            //await RunManifestWorldBuilder();
            await RunLODBuilder(args);
        }

        private static async Task RunManifestWorldBuilder()
        {
            ManifestWorldBuilder manifestWorldBuilder = new ManifestWorldBuilder();
            await manifestWorldBuilder.Run();
        }

        private static async Task RunLODBuilder(string[] args)
        {
            if (args.Length == 0)
            {
                args = new string[]
                {
                    "coords",
                    "20"
                    //"-129,-77",
                    //"100",
                    //"../../../../scene-lod-entities-manifest-builder/",
                    //"C:/Users/juanm/Documents/Decentraland/asset-bundle-converter/asset-bundle-converter/Assets/Resources/",
                    //Geneisis Plaza
                    //"bafkreieifr7pyaofncd6o7vdptvqgreqxxtcn3goycmiz4cnwz7yewjldq",
                    //"0,0",
                    //MonsterScene
                    // "bafkreifaupi2ycrpneu7danakhxvhyjewv4ixcnryu5w25oqpvcnwtjohq",
                    //"-129,-77",
                };
            }
            
            //var sceneParam = args[1];
            //var decimationRatio = args[2];
            
            /*string scenesToConvert =
                "-9,-10;-8,-10;-7,-10;-6,-10;-5,-10;-4,-10;-3,-10;-2,-10;-1,-10;0,-10;1,-10;2,-10;3,-10;4,-10;5,-10;6,-10;7,-10;8,-10;9,-10;-10,-11;-9,-11;-8,-11;-7,-11;-6,-11;-5,-11;-4,-11;-3,-11;-2,-11;-1,-11;0,-11;1,-11;2,-11;3,-11;4,-11;5,-11;6,-11;7,-11;8,-11;9,-11;10,-11;-10,-10;10,-10;-10,-9;-10,-8;-10,-7;-10,-6;-10,-5;-10,-4;-10,-3;-10,-2;-10,-1;-10,0;-10,1;-10,2;-10,3;-10,4;" +
                "-10,5;-10,6;-10,7;-10,8;-10,9;-11,-18;0,-69;4,-12;5,-12;6,-12;8,-12;10,-12;11,-12;-11,-11;11,-11;-11,-10;11,-10;-11,-9;11,-9;-11,-8;11,-8;-11,-7;11,-7;-11,-6;11,-6;-11,-5;11,-5;-11,-4;11,-4;-11,-3;11,-3;-11,-2;11,-2;-11,-1;11,-1;-11,0;11,0;-11,1;11,1;-11,2;11,2;-11,3;11,3;-11,4;11,4;-11,5;11,5;-11,6;11,6;-11,7;11,7;-11,8;11,8;-11,9;11,9;-11,10;-10,10;-9,10;-8,10;-7,10;-6,10;" +
                "-5,10;-4,10;-3,10;-2,10;-1,10;0,10;1,10;2,10;3,10;4,10;5,10;6,10;7,10;8,10;9,10;10,10;11,10;3,-13;4,-13;5,-13;7,-13;9,-13;11,-13;12,-13;12,-12;12,-11;12,-10;-18,-7;12,-9;12,-8;12,-7;12,-6;-12,-5;12,-5;12,-4;12,-3;12,-2;-70,-1;12,-1;12,0;12,1;12,2;-12,3;12,3;12,4;-12,5;12,5;-12,6;12,6;12,7;12,8;-12,9;12,9;-12,10;12,10;-12,11;-11,11;-10,11;-9,11;-8,11;-7,11;-6,11;-5,11;-4,11;-3,11;" +
                "-2,11;-1,11;0,11;1,11;2,11;3,11;4,11;5,11;6,11;7,11;8,11;9,11;10,11;11,11;12,11;2,-14;3,-14;4,-14;5,-14;6,-14;7,-14;8,-14;9,-14;10,-14;11,-14;12,-14;13,-14;13,-12;-13,-11;13,-11;14,-9;14,-8;13,-7;-13,-6;13,-4;-13,-5;13,-3;-13,-2;13,-2;13,-1;13,0;-13,1;13,2;13,4;-13,6;13,7;-13,9;-13,10;13,10;13,11;-13,12;-10,13;-9,12;-12,19;-6,12;-4,12;-3,12;0,12;4,15;7,15;12,12;-14,-15;3,-15;4,-16;" +
                "5,-15;6,-15;7,-15;12,-15;14,-14;-14,-13;-14,-12;14,-11;14,-7;-14,-6;-14,-5;14,-5;14,-4;14,-3;14,-2;14,-1;-14,1;-14,2;-14,3;-14,4;-14,5;-14,6;-14,9;-14,10;14,10;-14,11;-14,12;-14,13;-9,13;-6,13;-4,13;-2,13;10,13;14,13;-15,-16;-14,-16;6,-16;7,-16;11,-16;15,-16;15,-15;15,-14;15,-13;15,-12;15,-11;15,-10;15,-9;15,-8;15,-7;-15,-6;15,-6;15,-5;-18,-4;15,-4;15,-3;15,-2;15,-1;15,2;-15,3;15,3;" +
                "15,5;-15,6;-15,9;15,10;15,11;-15,12;-15,14;-14,14;-13,14;-12,14;-11,14;-10,14;-9,14;-6,14;-5,14;-4,14;-3,14;10,14;12,14;13,14;14,14;15,14;-16,-18;2,-17;3,-17;4,-17;5,-17;6,-17;7,-17;8,-17;9,-17;10,-17;11,-17;12,-17;13,-17;14,-17;15,-17;16,-17;16,-15;16,-14;16,-11;16,-5;16,-4;-16,3;16,5;-16,6;16,8;-16,9;16,9;16,10;-16,12;-16,14;-14,15;-12,15;-9,15;2,-18;2,16;18,15;8,19;8,18;4,19;5,-18;" +
                "19,-2;23,-20;20,10;2,-24;5,-21;18,-25;23,19;7,21;15,-18;-18,-23;22,-8;-19,-20;-7,21;22,-19;23,11;-27,19;21,19;5,19;3,-19;19,-11;21,5;-17,-21;7,16;19,21;-17,22;19,-18;-22,22;21,15;4,-22;18,9;5,22;-17,-23;23,20;-3,19;7,-21;22,-1;15,-21;-18,-17;22,4;20,-8;12,-18;4,-21;7,-19;14,-24;9,-18;10,-24;23,13;20,-2;8,17;-23,-10;-2,17;-18,4;19,-1;2,-25;13,15;19,-14;23,18;19,8;2,19;-19,22;21,9;22,-9;" +
                "-5,20;12,-23;20,-7;15,-23;-23,17;19,11;16,-20;4,-23;18,-13;16,17;21,-3;4,21;3,-25;-17,4;-8,20;-20,8;-19,-7;19,13;15,15;-17,-18;-6,19;11,-23;3,-23;11,-20;-17,3;11,18;-9,17;-21,22;21,-5;17,17;17,-20;18,-4;-17,5;-16,17;12,-20;-16,19;21,-1;-22,-19;19,-23;22,15;19,-6;-20,5;20,-24;-20,15;-3,20;-3,16;18,-7;-17,20;-21,6;18,-8;-7,22;-21,20;17,-24;-23,11;-14,20;21,-13;8,-23;22,-23;8,-20;-12,20;" +
                "-20,19;-19,-14;-19,16;14,16;-22,9;8,-19;21,-21;17,14;22,3;-16,-19;-20,22;16,16;-6,17;-17,-19;18,-16;-16,20;10,-22;-6,22;6,20;15,-20;-22,3;18,-18;-37,-18;19,-5;23,-21;-6,20;9,-24;21,-22;21,-2;4,-24;18,-1;-9,22;-12,17;-23,21;9,19;-6,15;-19,-18;-1,16;-6,16;23,22;4,-25;-9,21;4,-19;-23,13;-23,9;21,-19;-17,11;-17,15;23,-24;20,-20;20,-23;-17,-14;-10,17;-7,20;-4,17;14,15;-23,14;17,-5;-22,12;7,17;" +
                "-6,18;6,-24;4,-18;23,-16;19,-17;18,8;-23,-23;-19,2;23,-18;-23,4;21,-4;-3,21;18,-6;21,8;-20,-21;6,17;12,-24;5,21;-20,9;21,-15;21,-10;18,-15;-20,3;-20,4;-18,3;-14,16;9,-21;2,-20;18,-23;-14,17;21,-8;-5,17;-20,14;18,-3;21,-16;-23,22;10,-21;6,-22;-22,21;18,-9;-20,12;-20,21;15,-24;18,18;-20,20;-18,6;-15,17;23,-23;-12,22;14,-23;-11,17;6,18;-21,12;9,-23;-3,15;-19,20;-27,-17;-17,16;21,-9;18,-12;24,4;" +
                "9,-20;21,-12;-19,6;18,-2;18,-5;-12,16;18,-10;13,-20;22,14;9,22;-20,16;-3,17;-23,15;10,-23;-11,20;12,17;21,-11;-19,-3;-23,-2;17,16;21,-7;-23,-22;18,-20;22,-15;-23,5;5,-23;13,18;-18,-3;-12,21;18,-17;-16,22;-18,-20;19,-7;22,-20;18,-11;6,-20;-16,21;18,-19;-21,-28;11,-22;-20,6;17,2;21,-14;-20,10;-10,20;-21,9;-17,12;-17,6;5,-20;-12,-18;-17,-11;-23,10;-3,22;-20,17;-4,20;22,21;-19,-16;16,-23;-17,-22;" +
                "8,-18;-21,16;22,-24;14,-20;-20,11;21,22;7,-20;-18,16;-23,18;-20,18;13,-23;-6,21;18,-14;-22,6;19,-20;-17,14;-21,3;21,-6;9,-22;-22,16;5,-19;-1,15;22,-22;22,-21;-23,12;6,-23;17,-23;20,-17;3,-20;-18,20;-9,16;20,3;18,4;-17,9;-17,10;21,-20;-16,18;21,-18;-19,-21;-22,1;2,-23;7,-23;-20,7;-23,-20;2,-22;17,-17;23,-9;11,17;4,-20;19,4;-15,20;-23,16;21,-17;-9,20;19,18;-21,21;10,-20;-23,3;17,-14;-23,6;-9,-9";
           string[] decimationValues = new[] { "100", "25", "5"};
            string[] scenes = scenesToConvert.Split(';');*/
            
            var paramType = args[0];
            var limitParam = args[1];
            var sceneManifestDirectory = Path.Combine(Directory.GetCurrentDirectory(), "scene-lod-entities-manifest-builder"); //Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), args[3]));
            var scenePositionJsonDirectory = Path.Combine(sceneManifestDirectory, "output-manifests/");
            var outputDirectory =  Path.Combine(Directory.GetCurrentDirectory(), "built-lods"); 

            var scenesToAnalyze = GetScenesToAnalyzeList(limitParam);
            List<string> analyzedScenes = new List<string>();
            int[] triangleLimitPerParcel = new[] { 7_000, 3_000, 1_000};
            
            FrameworkInitialization(sceneManifestDirectory);
            
            WebRequestsHandler webRequestsHandler = new WebRequestsHandler();
            
            Stopwatch allProcessStopwatch = new Stopwatch(); 
            allProcessStopwatch.Start();
            int failedConversions = 0;
            int successConversions = 0;
            int totalRetriesForManifestFile = 20;

            string successFile = "SuccessScenes.txt";
            string failFile = "FailScenes.txt";

            
            Console.WriteLine($"About to convert {scenesToAnalyze.Count} scenes");
            foreach (var scene in scenesToAnalyze)
            {


                if (analyzedScenes.Contains(scene))
                {
                    Console.WriteLine($"Scene {scene} has already been analyzed");
                    continue;
                }
                
                Importer importer = new Importer(paramType,scene,webRequestsHandler);
                await importer.GenerateSceneContent();
                string[] currentPointersList = importer.GetCurrentScenePointersList();
                if (currentPointersList.Length == 0)
                {
                    Console.WriteLine($"Scene {scene} is empty. Ignoring");
                    WriteToFile($"{scene}", "EmptyScenes.txt");
                    continue;
                }
                if (Directory.Exists(Path.Combine(outputDirectory, currentPointersList[0])))
                {
                    Console.WriteLine($"Skipping scene {scene} since its already converted");
                    continue;
                }
                foreach (var pointer in currentPointersList)
                {
                    if(!analyzedScenes.Contains(pointer))
                        analyzedScenes.Add(pointer);
                }

                if (currentPointersList[0].Equals("-27,-17") || currentPointersList[0].Equals("-75,-9") || currentPointersList[0].Equals("-5,36") || currentPointersList[0].Equals("16,34"))
                {
                    Console.WriteLine($"Skipping scene {scene} because it was causing an exit without exception");
                    continue;
                }


                Console.WriteLine("BEGIN MANIFEST GENERATION FOR SCENE " + scene);
                bool manifestGenerated = await GenerateManifest(scene, sceneManifestDirectory, new List<string>(){"manifest file already exists", "Failed to load script"}, triangleLimitPerParcel, failFile);
                if (!manifestGenerated)
                {
                    failedConversions += triangleLimitPerParcel.Length;
                    continue;
                }
                Console.WriteLine("END MANIFEST GENERATION FOR SCENE " + scene);

                int currentRetries = 0;
                //We have to wait till the file is written
                while (!File.Exists(Path.Combine(scenePositionJsonDirectory,
                           $"{importer.GetSceneHash()}-lod-manifest.json")))
                {
                    await Task.Delay(500);
                    currentRetries++;
                    if (currentRetries == totalRetriesForManifestFile)
                        break;
                }
                
                if (currentRetries == totalRetriesForManifestFile)
                {
                    failedConversions += triangleLimitPerParcel.Length;
                    foreach (var conversionValue in triangleLimitPerParcel)
                        WriteToFile($"{scene}\t{conversionValue}\tMANIFEST ERROR: Exceed retries waiting for file", failFile);
                    continue;
                }
                
                Dictionary<string, string> sceneContent = new Dictionary<string, string>();

                try
                {
                    sceneContent  = await importer.DownloadAllContent();
                }
                catch (Exception e)
                {
                    failedConversions += triangleLimitPerParcel.Length;
                    foreach (var conversionValue in triangleLimitPerParcel)
                        WriteToFile($"{scene}\t{conversionValue}\tDOWNLOAD ERROR: {e.Message}", failFile);
                    continue;
                }

                Console.WriteLine("BEGIN SCENE CONVERSION FOR SCENE " + scene);
                int currentLODLevel = 0;
                foreach (var triangleValue in triangleLimitPerParcel)
                {
                    pxz.Core.ResetSession();
                    try
                    {
                        Console.WriteLine($"Converting {scene} with {triangleValue}");
                        await ConvertSceneWithTriangleLimitValue(webRequestsHandler,
                            importer.GetSceneHash(), importer.GetScenePointer(),sceneContent, currentPointersList.Length,
                            scenePositionJsonDirectory, triangleValue, currentLODLevel,
                            outputDirectory);
                        Console.WriteLine($"Finished Converting {scene} with {triangleValue}");
                        successConversions++;
                        WriteToFile($"{scene}\t{triangleValue}" , successFile);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("CONVERSION ERROR: " + e.Message);
                        failedConversions++;
                        WriteToFile($"{scene}\t{triangleLimitPerParcel}\tCONVERSION ERROR: {e.Message}", failFile);
                    }
                    currentLODLevel++;
                }
                GC.Collect();
                Console.WriteLine("END SCENE CONVERSION FOR SCENE " + scene);
            }

            allProcessStopwatch.Stop();
            TimeSpan ts = allProcessStopwatch.Elapsed;
            // Format and display the TimeSpan value
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}",
                ts.Hours, ts.Minutes, ts.Seconds);
            Console.WriteLine($"Final runtime is {elapsedTime} with {successConversions} success and {failedConversions} fails");
            
        }

        private static async Task<bool> GenerateManifest(string scene, string sceneManifestDirectory, List<string> errorsToIgnore, int[] conversionValues, string failFile)
        {
            var (npmOutput, readTimeout) = await NPMUtils.RunNPMToolAndReturnExceptionIfPresent(
                sceneManifestDirectory, scene, 4000);
    
            if (!string.IsNullOrEmpty(npmOutput))
            {
                // Check if the error can be ignored based on the provided error list.
                foreach (var errorToIgnore in errorsToIgnore)
                {
                    if (npmOutput.Contains(errorToIgnore))
                    {
                        // Return true if error is ignorable.
                        return true;
                    }
                }
                Console.WriteLine($"MANIFEST ERROR: {npmOutput}");
                foreach (var conversionValue in conversionValues)
                    WriteToFile($"{scene}\t{conversionValue}\tMANIFEST ERROR: {npmOutput}", failFile);
                // Return false as the error is not ignorable.
                return false;
            }
            // Return true if there's no error in the NPM tool output.
            return true;
        }
        
        private static async Task ConvertSceneWithTriangleLimitValue(WebRequestsHandler webRequestsHandler, string sceneHash, string scenePointer, Dictionary<string,string> sceneContent, int parcelAmount,
            string scenePositionJsonDirectory, double triangleLimit, int lodLevel, string outputDirectory)
        {
            SceneRepositioner.SceneRepositioner sceneRepositioner = 
                new SceneRepositioner.SceneRepositioner(webRequestsHandler,
                    scenePositionJsonDirectory,
                    $"{sceneHash}-lod-manifest.json", sceneContent, pxz);
            List<PXZModel> models = await sceneRepositioner.SetupSceneInPiXYZ();

            List<IPXZModifier> modifiers = new List<IPXZModifier>();
            modifiers.Add(new PXZDeleteByName(".*collider.*"));
            modifiers.Add(new PXZRepairMesh(models));
            modifiers.Add(new PXZDecimator(scenePointer, DecimateOptionsSelector.Type.TRIANGLECOUNT, triangleLimit * parcelAmount));
            modifiers.Add(new PXZMergeMeshes());
            string filename = $"{sceneHash}_{lodLevel}";
            modifiers.Add(new PXZExporter(Path.Combine(outputDirectory, $"{scenePointer}/{triangleLimit}"), filename, ".fbx"));

            PXZStopwatch stopwatch = new PXZStopwatch();
            
            foreach (var pxzModifier in modifiers)
            {
                stopwatch.Start();
                pxzModifier.ApplyModification(pxz);
                stopwatch.StopAndPrint(pxzModifier.GetType().Name);
            }
        }
        
        
        private static async Task ConvertSceneWithDecimationValue(string sceneParam, string paramType,
            string scenePositionJsonDirectory, string decimationRatio, string outputDirectory, WebRequestsHandler webRequestsHandler)
        {
            Importer importer = new Importer(paramType,sceneParam,webRequestsHandler);
            await importer.GenerateSceneContent();
            Dictionary<string,string> sceneContent = await importer.DownloadAllContent();

            SceneRepositioner.SceneRepositioner sceneRepositioner = 
                new SceneRepositioner.SceneRepositioner(webRequestsHandler,
                    scenePositionJsonDirectory,
                    $"{importer.GetSceneHash()}-lod-manifest.json", sceneContent, pxz);
            List<PXZModel> models = await sceneRepositioner.SetupSceneInPiXYZ();

            double ratio = double.Parse(decimationRatio);
            string finalOutputDirectory = Path.Combine(outputDirectory, $"{importer.GetScenePointer()}/{ratio.ToString()}");
            Directory.CreateDirectory(finalOutputDirectory);
            List<IPXZModifier> modifiers = new List<IPXZModifier>();
            modifiers.Add(new PXZDeleteByName(".*collider.*"));
            modifiers.Add(new PXZRepairMesh(models));
            modifiers.Add(new PXZDecimator("", DecimateOptionsSelector.Type.RATIO, ratio));
            modifiers.Add(new PXZMergeMeshes());
            //modifiers.Add(new PXZDecimateAndBake());
            string filename = importer.GetSceneHash() + GetLodLevel(ratio);
            modifiers.Add(new PXZExporter(finalOutputDirectory, filename, ".fbx"));

            PXZStopwatch stopwatch = new PXZStopwatch();
            
            foreach (var pxzModifier in modifiers)
            {
                stopwatch.Start();
                pxzModifier.ApplyModification(pxz);
                stopwatch.StopAndPrint(pxzModifier.GetType().Name);
            }
        }

        private static string GetLodLevel(double decimationRatio)
        {
            if (decimationRatio.Equals(5))
            {
                return "_lod3";
            }
            if (decimationRatio.Equals(25))
            {
                return "_lod2";
            }
            return "_lod0";
        }

        private static void FrameworkInitialization(string sceneManifestDirectory)
        {
            InitializePiXYZ();
            Console.WriteLine("Scene manifest directory " + sceneManifestDirectory);
            NPMUtils.DoNPMInstall(sceneManifestDirectory);
            Console.WriteLine("Finish installation " + sceneManifestDirectory);
            CreateResourcesDirectory();
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
        
        private static List<string> GetScenesToAnalyzeList(string limitParam)
        {
            List<string> scenes = new List<string>();
            int limitInt = int.Parse(limitParam);
            for (int i = -limitInt; i <= limitInt; i++)
            {
                for(int j = -limitInt; j <= limitInt; j++)
                {
                    scenes.Add($"{i},{j}");
                }
            }

            return scenes;
        }
    }
}