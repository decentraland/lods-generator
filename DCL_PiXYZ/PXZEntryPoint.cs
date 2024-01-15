using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using DCL_PiXYZ.Utils;
using SceneImporter;
using UnityEngine.Pixyz.Algo;
using UnityEngine.Pixyz.API;

namespace DCL_PiXYZ
{
    class PXZEntryPoint
    {
        private static PiXYZAPI pxz;
        static async Task Main(string[] args)
        {
            await RunManifestWorldBuilder();
            //await RunLODBuilder(args);
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

            //-9,-9;2,-10;1,-10;-3,-10;-2,-10;0,-10;-1,-10;-1,-11;4,-10;0,-11;3,-10;-5,-10;-4,-10;5,-10;-6,-10;-3,-11;-2,-11;-10,2;-10,-3;-10,1;-10,0;-10,-1;-10,-2;-10,4;-10,-5;-10,3;-10,-4;2,10;1,10;-3,10;-1,10;-2,10;0,10;0,11;1,11;-2,11;-1,11;0,12;
            //-2,13
            string scenesToConvert =
                "3,10;-4,10;-1,15;2,11;-3,11;4,10;3,11;-5,10;-4,11;-10,5;-1,16;-5,11;-6,10;4,15;5,10;4,11;-3,12;-4,12;-2,17;-10,6;-4,13;-3,14;-6,11;5,11;-4,14;-10,7;-3,15;-10,8;7,15;-6,12;-5,14;2,16;-6,13;-3,16;-10,9;-7,10;7,10;6,10;-6,14;-9,10;8,10;9,10;-8,10;-10,10;-3,17;-4,17;-5,17;-6,15;6,11;-7,11;7,11;-9,11;-3,19;-8,11;2,19;-6,16;8,11;-9,12;-10,11;9,11;-6,17;-3,22;";
            string[] decimationValues = new[] { "25", "5"};
            string[] scenes = scenesToConvert.Split(';');
            
            InitializePiXYZ();
            CreateResourcesDirectory();
            
            foreach (var scene in scenes)
            {
                if (string.IsNullOrEmpty(scene))
                    continue;
                foreach (var decimationValue in decimationValues)
                {
                    Console.WriteLine($"Converting {scene} with {decimationValue}");
                    await ConvertScene(sceneManifestDirectory, scene, paramType, scenePositionJsonDirectory, decimationValue, outputDirectory);
                    pxz.Core.ResetSession();
                }
            }
        }


        private static async Task ConvertScene(string sceneManifestDirectory, string sceneParam, string paramType,
            string scenePositionJsonDirectory, string decimationRatio, string outputDirectory)
        {
            GenerateSceneManifest(sceneManifestDirectory, sceneParam);

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
            string finalOutputDirectory = Path.Combine(outputDirectory, $"{importer.GetScenePointer()}/{ratio.ToString()}");
            Directory.CreateDirectory(finalOutputDirectory);
            List<IPXZModifier> modifiers = new List<IPXZModifier>();
            modifiers.Add(new PXZDeleteByName(".*collider.*"));
            modifiers.Add(new PXZRepairMesh(models));
            modifiers.Add(new PXZDecimator(DecimateOptionsSelector.Type.RATIO, ratio));
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
            if(decimationRatio <= 25)
                return "_lod2";
            if(decimationRatio <= 50)
                return "_lod1";
            return "_lod0";
        }

        private static void GenerateSceneManifest(string sceneManifestProjectDirectory, string coords)
        {
            Console.WriteLine("Scene manifest directory " + sceneManifestProjectDirectory);
            NPMUtils.DoNPMInstall(sceneManifestProjectDirectory);
            Console.WriteLine("Finish installation " + sceneManifestProjectDirectory);
            NPMUtils.RunNPMTool(sceneManifestProjectDirectory, coords);
            Console.WriteLine("Finish building manifest " + sceneManifestProjectDirectory);
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