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
            InitializePiXYZ();
            CreateResourcesDirectory();
            WebRequestsHandler webRequestsHandler = new WebRequestsHandler();

            if (args.Length == 0)
            {
                args = new string[]
                {
                    //MonsterScene
                    // "bafkreifaupi2ycrpneu7danakhxvhyjewv4ixcnryu5w25oqpvcnwtjohq",
                    //Geneisis Plaza
                    //"bafkreieifr7pyaofncd6o7vdptvqgreqxxtcn3goycmiz4cnwz7yewjldq",
                    "coords",
                    "0,0",
                    "C:/Users/juanm/Documents/Decentraland/PiXYZ/DCL_PiXYZ/SceneRepositioner/Resources/",
                };
            }

            var paramType = args[0];
            var sceneId = args[1];
            var scenePositionJsonDirectory = args[2];

            
            Importer importer = new Importer(paramType,sceneId,webRequestsHandler);
            await importer.GenerateSceneContent();
            Dictionary<string,string> sceneContent = await importer.DownloadAllContent();

            SceneRepositioner.SceneRepositioner sceneRepositioner = 
                 new SceneRepositioner.SceneRepositioner(webRequestsHandler,
                     scenePositionJsonDirectory,
                     $"{importer.GetSceneHash()}-lod-manifest.json", sceneContent, pxz);
            List<PXZModel> models = await sceneRepositioner.SetupSceneInPiXYZ();

            double ratio = 50;
            string outputDirectory =
                $"C:/Users/juanm/Documents/Decentraland/asset-bundle-converter/asset-bundle-converter/Assets/Resources/{importer.GetScenePointer()}/{ratio.ToString()}";
            Directory.CreateDirectory(outputDirectory);
            List<IPXZModifier> modifiers = new List<IPXZModifier>();
            modifiers.Add(new PXZDeleteByName(".*collider.*"));
            modifiers.Add(new PXZRepairMesh(models));
            modifiers.Add(new PXZDecimator(DecimateOptionsSelector.Type.RATIO, ratio));
            modifiers.Add(new PXZMergeMeshes());
            //modifiers.Add(new PXZDecimateAndBake());
            modifiers.Add(new PXZExporter(outputDirectory, $"0_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}", ".fbx"));

            PXZStopwatch stopwatch = new PXZStopwatch();
            
            
            foreach (var pxzModifier in modifiers)
            {
                stopwatch.Start();
                pxzModifier.ApplyModification(pxz);
                stopwatch.StopAndPrint(pxzModifier.GetType().Name);
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
                pxz.Core.InstallLicense("C:/Users/juanm/Documents/Decentraland/PiXYZ/pixyzsdk-29022024.lic");
        }
    }
}