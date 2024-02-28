using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using Newtonsoft.Json;

namespace DCL_PiXYZ.Utils
{
    public class RoadJSONGenerator
    {
        private static WebRequestsHandler webRequestsHandler;
        private static SceneConversionPathHandler pathHandler;

        public static async Task GenerateWorldROADJSON()
        {
            pathHandler = new SceneConversionPathHandler(true, Path.Combine(Directory.GetCurrentDirectory(), "built-lods"),
                Path.Combine(Directory.GetCurrentDirectory(), "scene-lod-entities-manifest-builder/"),
                "SuccessScenes.txt", "FailScenes.txt", "PolygonCount.txt" , "FailedGLBImport.txt" , "");

            NPMUtils.DoNPMInstall(pathHandler.ManifestProjectDirectory);

            webRequestsHandler = new WebRequestsHandler();
            string allParcels = await webRequestsHandler.GetRequest("https://api.decentraland.org/v2/tiles?x1=-150&y1=-150&x2=150&y2=150&include=type");
            var response = JsonConvert.DeserializeObject<AtlasJSONResponse>(allParcels);

            var singleParcelRoadsDictionary = new Dictionary<string, RoadInfoJSON>();

            foreach (var keyValuePair in response.Data)
            {
                if (keyValuePair.Value.Type.Equals("road"))
                {
                    string parcelPointer = keyValuePair.Key;
                    Console.WriteLine($"Analyzing {parcelPointer}");
                    var isSingleParcelRoad = await IsSingleParcelRoad(parcelPointer);
                    if (isSingleParcelRoad.Item1)
                        singleParcelRoadsDictionary.TryAdd(parcelPointer, isSingleParcelRoad.Item2);
                    Console.WriteLine($"Finished analyzing {parcelPointer}");
                }
            }

            FileWriter.WriteToFile(JsonConvert.SerializeObject(singleParcelRoadsDictionary), Path.Combine(Directory.GetCurrentDirectory(), "SingleParcelRoadInfo.json"));
        }

        private static async Task<(bool, RoadInfoJSON)> IsSingleParcelRoad(string parcelPointer)
        {
            var roadInfoJson = default(RoadInfoJSON);
            var sceneImporter = new SceneImporter("coords", parcelPointer, webRequestsHandler);
            await sceneImporter.DownloadSceneDefinition();

            if (sceneImporter.GetPointers().Length != 1)
                return (false, roadInfoJson);

            pathHandler.SetOutputPath(sceneImporter);

            await PXZEntryPoint.ManifestGeneratedSuccesfully("coords", pathHandler, parcelPointer);

            var renderableEntities
                = JsonConvert.DeserializeObject<List<RenderableEntity>>(
                    File.ReadAllText($"{pathHandler.ManifestOutputJsonDirectory}\\{sceneImporter.GetSceneHash()}-lod-manifest.json"));

            if (renderableEntities.Count == 2)
            {
                foreach (var renderableEntity in renderableEntities)
                {
                    if (renderableEntity.componentName.Equals("core::GltfContainer"))
                    {
                        roadInfoJson.model = ((GLTFContainerData)renderableEntity.data).mesh.src;
                    }
                    else if (renderableEntity.componentName.Equals("core::Transform"))
                    {
                        var position = ((TransformData)renderableEntity.data).position;
                        roadInfoJson.position = position.ToString();
                        var rotation = ((TransformData)renderableEntity.data).rotation;
                        roadInfoJson.rotation = rotation.ToString();
                        var scale = ((TransformData)renderableEntity.data).scale;
                        roadInfoJson.scale = scale.ToString();

                        if (position.x != 8 && position.y != 0 && position.z != 8)
                        {
                            FileWriter.WriteToFile(sceneImporter.GetSceneHash(), Path.Combine(Directory.GetCurrentDirectory(), "NonDefaultPosition.txt"), append: true);
                        }

                        if (scale.x != 1 && scale.y != 1 && scale.z != 1)
                        {
                            FileWriter.WriteToFile(sceneImporter.GetSceneHash(), Path.Combine(Directory.GetCurrentDirectory(), "NonDefaultScale.txt"), append: true);
                        }
                    }
                }
            }
            else
            {
                FileWriter.WriteToFile(sceneImporter.GetSceneHash(), Path.Combine(Directory.GetCurrentDirectory(), "RoadsPositionFailInfo.txt"), append: false);
            }

            return (true, roadInfoJson);
        }
    }
}