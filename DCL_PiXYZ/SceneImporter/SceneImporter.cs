using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DCL_PiXYZ.Utils;
using Newtonsoft.Json;
using SceneImporter;

namespace DCL_PiXYZ
{
    public class SceneImporter
    {
        private string contentsURL;
        private string activeEntitiesURL;
        private string sceneParam;
        private WebRequestsHandler webRequestsHandler;
        private SceneDefinition sceneDefinition;
        private string[] ignoreExtensions;

        private bool paramByHash;

        private string sceneHash;
        private string sceneBasePointer;

        public Dictionary<string, string> sceneContent;

        public SceneImporter(string paramType, string sceneParam, WebRequestsHandler webRequestsHandler)
        {
            this.sceneParam = sceneParam;
            this.webRequestsHandler = webRequestsHandler;

            paramByHash = paramType.Equals(PXYZConstants.HASH_PARAM);
            sceneDefinition = new SceneDefinition();

            ignoreExtensions = new []{".mp3", ".js", ".lib", ".json", ".md", ".wav"};
            contentsURL = "https://peer.decentraland.org/content/contents/";
            activeEntitiesURL = "https://peer.decentraland.org/content/entities/active";
        }

        public async Task DownloadSceneDefinition()
        {
            Console.WriteLine("BEGIN SCENE DEFINITION DOWNLOAD");
            try
            {
                if (paramByHash)
                {
                    string rawSceneDefinition = await webRequestsHandler.GetRequest($"{contentsURL}{sceneHash}");
                    sceneDefinition = JsonConvert.DeserializeObject<SceneDefinition>(rawSceneDefinition);
                }
                else
                {
                    string rawSceneDefinition = await webRequestsHandler.PostRequest(activeEntitiesURL, "{\"pointers\":[\"" + sceneParam + "\"]}");
                    List<SceneDefinition> sceneDefinitions =
                        JsonConvert.DeserializeObject<List<SceneDefinition>>(rawSceneDefinition);
                    if (sceneDefinitions.Count > 0)
                        sceneDefinition = sceneDefinitions[0];
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Scene fetch failed: {e}");
            }
        }

        public string[] GetPointers()
        {
            return sceneDefinition.pointers;
        }

        public async Task<bool> DownloadAllContent(SceneConversionPathHandler pathHandler)
        {
            Console.WriteLine("BEGIN FILE CONTENT DOWNLOAD");
            sceneContent = new Dictionary<string, string>();
            foreach (var content in sceneDefinition.content)
            {
                try
                {
                    if (ignoreExtensions.Contains(Path.GetExtension(content.file)))
                        continue;
                    string filePath = Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, content.file);
                    await webRequestsHandler.DownloadFileAsync($"{contentsURL}{content.hash}", filePath);
                    sceneContent.Add(content.file.ToLower(), filePath);
                }
                catch (Exception e)
                {
                    FileWriter.WriteToFile($"{sceneBasePointer}\tDOWNLOAD ERROR: {e.Message}", pathHandler.FailFile);
                    return false;
                }
            }
            return true;
        }


        public string GetSceneHash()
        {
            return sceneDefinition.id;
        }

        public string GetSceneBasePointer()
        {
            return sceneDefinition.metadata.scene.baseParcel;
        }

    }
}