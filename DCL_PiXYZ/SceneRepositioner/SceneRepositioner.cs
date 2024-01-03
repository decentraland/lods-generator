using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using DCL_PiXYZ.SceneRepositioner.SceneBuilder.Entities;
using Newtonsoft.Json;
using UnityEngine.Pixyz.API;

namespace DCL_PiXYZ.SceneRepositioner
{
    public class SceneRepositioner {

        private WebRequestsHandler webRequestsHandler;
        private string baseURL;
        private string sceneID;
        private Dictionary<string, string> sceneContent;
        private PiXYZAPI pxz;

        public SceneRepositioner(WebRequestsHandler webRequestHandler, string baseUrl, string sceneID,
            Dictionary<string, string> sceneContent, PiXYZAPI pxz)
        {
            this.webRequestsHandler = webRequestHandler;
            this.baseURL = baseUrl;
            this.sceneID = sceneID;
            this.sceneContent = sceneContent;
            this.pxz = pxz;
        }
    
        public async Task<List<PXZModel>> SetupSceneInPiXYZ()
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN REPOSITIONING");
            List<PXZModel> models = new List<PXZModel>();
            //string rawSceneDefinition = await webRequestsHandler.FetchStringAsync($"{baseURL}{sceneID}");
            SceneDescriptorData sceneDescriptor = JsonConvert.DeserializeObject<SceneDescriptorData>(File.ReadAllText($"{baseURL}{sceneID}"));

            Dictionary<int, DCLRendereableEntity> renderableEntitiesDictionary = new Dictionary<int, DCLRendereableEntity>();
            foreach (var sceneDescriptorRenderableEntity in sceneDescriptor.RenderableEntities)
            {
                if(sceneDescriptorRenderableEntity.entityId != 512)
                    continue;
                
                if (!renderableEntitiesDictionary.TryGetValue(sceneDescriptorRenderableEntity.entityId, out var dclEntity))
                {
                    dclEntity = new DCLRendereableEntity();
                    renderableEntitiesDictionary.Add(sceneDescriptorRenderableEntity.entityId, dclEntity);
                }
                dclEntity.SetComponentData(sceneDescriptorRenderableEntity);
            }

            foreach (var dclRendereableEntity in renderableEntitiesDictionary)
                dclRendereableEntity.Value.InitEntity(pxz, pxz.Scene.GetRoot());

            foreach (KeyValuePair<int, DCLRendereableEntity> dclRendereableEntity in renderableEntitiesDictionary)
                models.Add(dclRendereableEntity.Value.PositionAndInstantiteMesh(sceneContent, renderableEntitiesDictionary));

            Console.WriteLine("END REPOSITIONING");

            return models;
        }
    }
}


