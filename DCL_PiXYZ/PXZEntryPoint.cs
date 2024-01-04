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
                    "bafkreifaupi2ycrpneu7danakhxvhyjewv4ixcnryu5w25oqpvcnwtjohq",
                    //Geneisis Plaza
                    //"bafkreieifr7pyaofncd6o7vdptvqgreqxxtcn3goycmiz4cnwz7yewjldq",
                    "C:/Users/juanm/Documents/Decentraland/PiXYZ/DCL_PiXYZ/SceneRepositioner/Resources/",
                };
            }

            Importer importer = new Importer(args[0],
                "https://peer.decentraland.org/content/contents/",
                webRequestsHandler);
            await importer.GenerateSceneContent();
            Dictionary<string,string> sceneContent = await importer.DownloadAllContent();
            
            SceneRepositioner.SceneRepositioner sceneRepositioner = 
                 new SceneRepositioner.SceneRepositioner(webRequestsHandler,
                     args[1],
                     $"{args[0]}-lod-manifest.json", sceneContent, pxz);
            List<PXZModel> models = await sceneRepositioner.SetupSceneInPiXYZ();

            
            List<IPXZModifier> modifiers = new List<IPXZModifier>();
            modifiers.Add(new PXZDeleteByName(".*collider.*"));
            modifiers.Add(new PXZRepairMesh(models));
            modifiers.Add(new PXZDecimator(DecimateOptionsSelector.Type.RATIO, 100));
            modifiers.Add(new PXZMergeMeshes());
            //modifiers.Add(new PXZDecimateAndBake());
            modifiers.Add(new PXZExporter("C:/Users/juanm/Documents/Decentraland/asset-bundle-converter/asset-bundle-converter/Assets/Resources",
                $"0_Combined_Meshes_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}", ".fbx"));

            OccurrenceList result = new OccurrenceList(new[] { pxz.Scene.GetRoot() });
          
            foreach (var pxzModifier in modifiers)
                pxzModifier.ApplyModification(pxz);
   
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