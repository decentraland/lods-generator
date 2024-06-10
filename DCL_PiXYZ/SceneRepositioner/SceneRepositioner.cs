using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using DCL_PiXYZ.SceneRepositioner.SceneBuilder.Entities;
using DCL_PiXYZ.Utils;
using Newtonsoft.Json;
using UnityEngine.Pixyz.API;

namespace DCL_PiXYZ.SceneRepositioner
{
    public class SceneRepositioner {
    
        private Dictionary<string, string> sceneContent;
        private PiXYZAPI pxz;
        private readonly SceneConversionPathHandler pathHandler;
        private int lodLevel;

        public SceneRepositioner(Dictionary<string, string> sceneContent, PiXYZAPI pxz, SceneConversionPathHandler pathHandler, int lodLevel)
        {
            this.sceneContent = sceneContent;
            this.pxz = pxz;
            this.pathHandler = pathHandler;
            this.lodLevel = lodLevel;
        }
    
        public List<PXZModel> SetupSceneInPiXYZ()
        {
            FileWriter.WriteToConsole("BEGIN REPOSITIONING");
            List<PXZModel> models = new List<PXZModel>();
            var renderableEntities = JsonConvert.DeserializeObject<List<RenderableEntity>>(File.ReadAllText(pathHandler.ManifestOutputJsonFile));
            Dictionary<int, DCLRendereableEntity> renderableEntitiesDictionary = new Dictionary<int, DCLRendereableEntity>();
           
            foreach (var sceneDescriptorRenderableEntity in renderableEntities)
            {
                if (!renderableEntitiesDictionary.TryGetValue(sceneDescriptorRenderableEntity.entityId, out var dclEntity))
                {
                    dclEntity = new DCLRendereableEntity();
                    renderableEntitiesDictionary.Add(sceneDescriptorRenderableEntity.entityId, dclEntity);
                }
                dclEntity.SetComponentData(sceneDescriptorRenderableEntity);
            }

            uint rootOccurrence = pxz.Scene.CreateOccurrence("DCL_SCENE", pxz.Scene.GetRoot());
            
            foreach (var dclRendereableEntity in renderableEntitiesDictionary)
                dclRendereableEntity.Value.InitEntity(pxz, rootOccurrence);

            foreach (KeyValuePair<int, DCLRendereableEntity> dclRendereableEntity in renderableEntitiesDictionary)
                models.Add(dclRendereableEntity.Value.PositionAndInstantiteMesh(sceneContent, renderableEntitiesDictionary, pathHandler, lodLevel));


            return models;
        }
    }
}


