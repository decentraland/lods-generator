using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using SceneImporter;
using UnityEngine.Pixyz.Algo;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Geom;
using UnityEngine.Pixyz.Scene;
using Vector3 = System.Numerics.Vector3;

namespace DCL_PiXYZ
{
    class PixyzEntryPoint
    {

        private static PiXYZAPI pxz;
        private static uint pxzRootOcurrence;
        
        static async Task Main(string[] args)
        {
            InitializePiXYZ();
            CreateResourcesDirectory();
            WebRequestsHandler webRequestsHandler = new WebRequestsHandler();
            
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN IMPORT");
            Importer importer = new Importer("bafkreieifr7pyaofncd6o7vdptvqgreqxxtcn3goycmiz4cnwz7yewjldq",
                "https://peer.decentraland.org/content/contents/",
                webRequestsHandler);
            await importer.GenerateSceneContent();
            Dictionary<string,string> sceneContent = await importer.DownloadAllContent();
            Console.WriteLine("END IMPORT");
/*
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN REPOSITIONING");
            SceneRepositioner.SceneRepositioner sceneRepositioner =
                new SceneRepositioner.SceneRepositioner(webRequestsHandler,
                    "C:/Users/juanm/Documents/Decentraland/PiXYZ/DCL_PiXYZ/SceneRepositioner/Resources/",
                    "rendereable-entities-manifest.json", sceneContent, pxz);
            await sceneRepositioner.SetupSceneInPiXYZ();
            Console.WriteLine("END REPOSITIONING");


            Console.WriteLine("-------------------------");


            OccurrenceList occurenceToDelete = pxz.Scene.FindOccurrencesByProperty("Name", ".*collider.*");
            pxz.Scene.DeleteOccurrences(occurenceToDelete);

            Console.WriteLine("BEGIN PXZ EXPORT " + pxz.Core.GetVersion());
            pxz.IO.ExportScene(Path.Combine("C:/Users/juanm/Documents/Decentraland/asset-bundle-converter/asset-bundle-converter/Assets/Resources", $"0_Combined_Meshes_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.fbx"), pxz.Scene.GetRoot());
            Console.WriteLine("END PXZ EXPORT");
            */

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //Trading Center
            uint tradingCenter = pxz.IO.ImportScene(sceneContent["models/trading_center.glb"]);
            pxz.Scene.SetParent(tradingCenter, pxz.Scene.GetRoot());
            
            //The Shell
            uint theShell = pxz.IO.ImportScene(sceneContent["models/shell.glb"]);
            pxz.Scene.SetParent(theShell, pxz.Scene.GetRoot());
            
            OccurrenceList occurenceToDelete = pxz.Scene.FindOccurrencesByProperty("Name", ".*collider.*");
            pxz.Scene.DeleteOccurrences(occurenceToDelete);
            
            pxz.Core.SetModuleProperty("Algo", "UseGPUBaking", "False");

            BakeOption bakeOption = new BakeOption();
            BakeMaps bakeMaps = new BakeMaps();
            bakeMaps.diffuse = true;
            bakeOption.bakingMethod = BakingMethod.RayOnly;
            bakeOption.resolution = 1024;
            bakeOption.padding = 1;
            bakeOption.textures = bakeMaps;
            uint combinedMesh = pxz.Algo.CombineMeshes(new OccurrenceList(new uint[] {tradingCenter, theShell}), bakeOption);
            

            
            pxz.IO.ExportScene(Path.Combine("C:/Users/juanm/Documents/Decentraland/asset-bundle-converter/asset-bundle-converter/Assets/Resources", $"0_Combined_Meshes_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.fbx"), combinedMesh);
            
            
            //DecimateOptionsSelector decimate = new DecimateOptionsSelector();
            //decimate.ratio = 100f;
            //decimate._type = DecimateOptionsSelector.Type.RATIO;
            //pxz.Algo.DecimateTarget(new OccurrenceList(new uint[] {combinedMesh}), decimate);
            stopwatch.Stop();
            TimeSpan timeTaken = stopwatch.Elapsed;
            // For a formatted string:
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                timeTaken.Hours, timeTaken.Minutes, timeTaken.Seconds,
                timeTaken.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
            
        }

        private static void CreateResourcesDirectory()
        {
            Directory.CreateDirectory(PiXYZConstants.RESOURCES_DIRECTORY);
        }


        private static void InitializePiXYZ()
        {
            pxz =
                PiXYZAPI.Initialize("PixyzSDKCSharp",
                    "204dda67aa3ea8bcb22a76bff9aa1224823b253144396405300e235e434c4711591892c19069c7");
            // if no license is found, try to configure a license server
            if (!pxz.Core.CheckLicense())
                pxz.Core.InstallLicense("C:/Users/juanm/Documents/Decentraland/PiXYZ/pixyzsdk-29022024.lic");
            
            pxzRootOcurrence = pxz.Scene.GetRoot();
        }
    }
}