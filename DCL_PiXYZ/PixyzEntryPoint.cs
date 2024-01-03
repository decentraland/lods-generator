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
    class PixyzEntryPoint
    {
        private static PiXYZAPI pxz;
        static async Task Main(string[] args)
        {
            InitializePiXYZ();
            CreateResourcesDirectory();
            WebRequestsHandler webRequestsHandler = new WebRequestsHandler();
            

            Importer importer = new Importer("bafkreifaupi2ycrpneu7danakhxvhyjewv4ixcnryu5w25oqpvcnwtjohq",
                "https://peer.decentraland.org/content/contents/",
            //Importer importer = new Importer("bafkreieifr7pyaofncd6o7vdptvqgreqxxtcn3goycmiz4cnwz7yewjldq",
            //    "https://peer.decentraland.org/content/contents/",
                webRequestsHandler);
            await importer.GenerateSceneContent();
            Dictionary<string,string> sceneContent = await importer.DownloadAllContent();


            SceneRepositioner.SceneRepositioner sceneRepositioner =
           //     new SceneRepositioner.SceneRepositioner(webRequestsHandler,
           //         "C:/Users/juanm/Documents/Decentraland/PiXYZ/DCL_PiXYZ/SceneRepositioner/Resources/",
           //         "rendereable-entities-manifest.json", sceneContent, pxz);
                 new SceneRepositioner.SceneRepositioner(webRequestsHandler,
                     "C:/Users/juanm/Documents/Decentraland/PiXYZ/DCL_PiXYZ/SceneRepositioner/Resources/",
                     "LOD-builder-test-scene-manifest_-129,-77.json", sceneContent, pxz);
            List<PXZModel> models = await sceneRepositioner.SetupSceneInPiXYZ();

            
            List<IPXZModifier> modifiers = new List<IPXZModifier>();
            modifiers.Add(new PXZDeleteByName(".*collider.*"));
            modifiers.Add(new PXZRepairMesh(models));
            //modifiers.Add(new PXZDecimator());
            modifiers.Add(new PXZMergeMeshes());
            modifiers.Add(new PXZExporter("C:/Users/juanm/Documents/Decentraland/asset-bundle-converter/asset-bundle-converter/Assets/Resources",
                $"0_Combined_Meshes_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}", ".fbx"));

            OccurrenceList result = new OccurrenceList(new[] { pxz.Scene.GetRoot() });
          
            foreach (var pxzModifier in modifiers)
                result = pxzModifier.ApplyModification(pxz, result);
   
        }

        private static void CreateResourcesDirectory() =>
            Directory.CreateDirectory(PiXYZConstants.RESOURCES_DIRECTORY);


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