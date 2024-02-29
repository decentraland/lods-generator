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
        private static List<string> ignorePointers;

        public static async Task GenerateWorldROADJSON()
        {
            pathHandler = new SceneConversionPathHandler(true, Path.Combine(Directory.GetCurrentDirectory(), "built-lods"),
                Path.Combine(Directory.GetCurrentDirectory(), "scene-lod-entities-manifest-builder/"),
                "SuccessScenes.txt", "FailScenes.txt", "PolygonCount.txt" , "FailedGLBImport.txt" , "");


            ignorePointers = new List<string>();
            NPMUtils.DoNPMInstall(pathHandler.ManifestProjectDirectory);

            webRequestsHandler = new WebRequestsHandler();
            string allParcels = await webRequestsHandler.GetRequest("https://api.decentraland.org/v2/tiles?x1=-5&y1=-150&x2=150&y2=150&include=type");
            var response = JsonConvert.DeserializeObject<AtlasJSONResponse>(allParcels);

            var singleParcelRoadsDictionary = new Dictionary<string, RoadInfoJSON>();

            int addedValues = 0;
            foreach (var keyValuePair in response.Data)
            {
                string parcelPointer = keyValuePair.Key;
                if (keyValuePair.Value.Type.Equals("road") && !ignorePointers.Contains(parcelPointer))
                {
                    Console.WriteLine($"Analyzing {parcelPointer}");
                    var isSingleParcelRoad = await IsSingleParcelRoad(parcelPointer);
                    if (isSingleParcelRoad.Item1)
                    {
                        bool added = singleParcelRoadsDictionary.TryAdd(parcelPointer, isSingleParcelRoad.Item2);
                        if (added)
                        {
                            addedValues++;
                            if (addedValues > 100)
                            {
                                FileWriter.WriteToFile(JsonConvert.SerializeObject(singleParcelRoadsDictionary), Path.Combine(Directory.GetCurrentDirectory(), "SingleParcelRoadInfo.json"), append: false);
                                addedValues = 0;
                            }
                        }
                    }
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
            {
                foreach (string pointer in sceneImporter.GetPointers())
                {
                    ignorePointers.Add(pointer);
                }
                return (false, roadInfoJson);
            }

            pathHandler.SetOutputPath(sceneImporter);

            await PXZEntryPoint.ManifestGeneratedSuccesfully("coords", pathHandler, parcelPointer);

            if (!File.Exists($"{pathHandler.ManifestOutputJsonDirectory}\\{sceneImporter.GetSceneHash()}-lod-manifest.json"))
            {
                Console.WriteLine("MANIFEST NOT GENERATED");
                FileWriter.WriteToFile(sceneImporter.GetSceneHash(), Path.Combine(Directory.GetCurrentDirectory(), "RoadsManifestFailInfo.txt"), append: false);
                return (false, roadInfoJson);
            }
            
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