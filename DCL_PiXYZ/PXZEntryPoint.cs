using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using SceneImporter;
using UnityEngine.Pixyz.Algo;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Geom;
using UnityEngine.Pixyz.Scene;
using Vector3 = System.Numerics.Vector3;

namespace DCL_PiXYZ
{
    class PXZEntryPoint
    {
        private static PiXYZAPI pxz;
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                args = new string[]
                {
                    "coords",
                    "-129,-77",
                    "100",
                    "../../../../scene-lod-entities-manifest-builder/",
                    "C:/Users/juanm/Documents/Decentraland/asset-bundle-converter/asset-bundle-converter/Assets/Resources/",
                    //Geneisis Plaza
                    //"bafkreieifr7pyaofncd6o7vdptvqgreqxxtcn3goycmiz4cnwz7yewjldq",
                    //"0,0",
                    //MonsterScene
                    // "bafkreifaupi2ycrpneu7danakhxvhyjewv4ixcnryu5w25oqpvcnwtjohq",
                    //"-129,-77",
                };
            }

            var paramType = args[0];
            var sceneParam = args[1];
            var decimationRatio = args[2];
            var sceneManifestDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), args[3]));
            var scenePositionJsonDirectory = Path.Combine(sceneManifestDirectory, "output-manifests/");
            var outputDirectory = args[4];
            
            GenerateSceneManifest(sceneManifestDirectory, sceneParam);
            InitializePiXYZ();
            CreateResourcesDirectory();
            WebRequestsHandler webRequestsHandler = new WebRequestsHandler();
            
            Importer importer = new Importer(paramType,sceneParam,webRequestsHandler);
            await importer.GenerateSceneContent();
            Dictionary<string,string> sceneContent = await importer.DownloadAllContent();

            SceneRepositioner.SceneRepositioner sceneRepositioner = 
                 new SceneRepositioner.SceneRepositioner(webRequestsHandler,
                     scenePositionJsonDirectory,
                     $"{importer.GetSceneHash()}-lod-manifest.json", sceneContent, pxz);
            List<PXZModel> models = await sceneRepositioner.SetupSceneInPiXYZ();

            double ratio = double.Parse(decimationRatio);
            //string finalOutputDirectory = Path.Combine(outputDirectory, $"{importer.GetScenePointer()}/{ratio.ToString()}");
            string finalOutputDirectory = outputDirectory;
            Directory.CreateDirectory(finalOutputDirectory);
            List<IPXZModifier> modifiers = new List<IPXZModifier>();
            modifiers.Add(new PXZDeleteByName(".*collider.*"));
            modifiers.Add(new PXZRepairMesh(models));
            modifiers.Add(new PXZDecimator(DecimateOptionsSelector.Type.RATIO, ratio));
            modifiers.Add(new PXZMergeMeshes());
            //modifiers.Add(new PXZDecimateAndBake());
            modifiers.Add(new PXZExporter(finalOutputDirectory, $"0_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}", ".fbx"));

            PXZStopwatch stopwatch = new PXZStopwatch();
            
            foreach (var pxzModifier in modifiers)
            {
                stopwatch.Start();
                pxzModifier.ApplyModification(pxz);
                stopwatch.StopAndPrint(pxzModifier.GetType().Name);
            }
   
        }

        private static void GenerateSceneManifest(string sceneManifestProjectDirectory, string coords)
        {
            Console.WriteLine("Scene manifest directory " + sceneManifestProjectDirectory);
            DoNPMInstall(sceneManifestProjectDirectory);
            Console.WriteLine("Finish installation " + sceneManifestProjectDirectory);
            RunNPMTool(sceneManifestProjectDirectory, coords);
            Console.WriteLine("Finish building manifest " + sceneManifestProjectDirectory);
        }

        private static void RunNPMTool(string sceneManifestProjectDirectory, string coords)
        {
            // Set up the process start information
            ProcessStartInfo install = new ProcessStartInfo
            {
                FileName = "powershell", // or the full path to npm if not in PATH
                Arguments = "npm run start --coords=" + coords, // replace with your npm command
                WorkingDirectory = sceneManifestProjectDirectory,
                RedirectStandardOutput = true, // if you want to read output
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Start the process
            using (Process process = Process.Start(install))
            {
                // Read the output (if needed)
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine(result);
                }

                process.WaitForExit(); // Wait for the process to complete
            }
        }

        private static void DoNPMInstall(string sceneManifestProjectDirectory)
        {
            // Set up the process start information
            ProcessStartInfo install = new ProcessStartInfo
            {
                FileName = "powershell", // or the full path to npm if not in PATH
                Arguments = "npm i", // replace with your npm command
                WorkingDirectory = sceneManifestProjectDirectory,
                RedirectStandardOutput = true, // if you want to read output
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            // Start the process
            using (Process process = Process.Start(install))
            {
                // Read the output (if needed)
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine(result);
                }

                process.WaitForExit(); // Wait for the process to complete
            }
            
            ProcessStartInfo build = new ProcessStartInfo
            {
                FileName = "powershell", // or the full path to npm if not in PATH
                Arguments = "npm run build", // replace with your npm command
                WorkingDirectory = sceneManifestProjectDirectory,
                RedirectStandardOutput = true, // if you want to read output
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Start the process
            using (Process process = Process.Start(build))
            {
                // Read the output (if needed)
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine(result);
                }

                process.WaitForExit(); // Wait for the process to complete
            }
        }

        private static void CreateResourcesDirectory() =>
            Directory.CreateDirectory(PXYZConstants.RESOURCES_DIRECTORY);


        private static void InitializePiXYZ()
        {
            pxz =
                PiXYZAPI.Initialize("PixyzSDKCSharp",
                    "204dda67aa3ea8bcb22a76bff9aa1224823b253144396405300e235e434c4711591892c19069c7");
            // if no license is found, try to configure a license server
            if (!pxz.Core.CheckLicense())
                pxz.Core.InstallLicense("pixyzsdk-29022024.lic");
        }
    }
}