using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DCL_PiXYZ;
using DCL_PiXYZ.Utils;
using Newtonsoft.Json;

namespace SceneImporter
{
    public class Importer
    {
        private string contentsURL;
        private string activeEntitiesURL;
        private string sceneParam;
        private WebRequestsHandler webRequestsHandler;
        private SceneDefinition sceneDefinition;
        private string[] ignoreExtensions;

        private bool paramByHash;

        private string sceneHash;
        private string scenePointer;

        private string[] currentPointersList;

        public Dictionary<string, string> sceneContent;

        public Importer(string paramType, string sceneParam, WebRequestsHandler webRequestsHandler)
        {
            this.sceneParam = sceneParam;
            this.webRequestsHandler = webRequestsHandler;

            paramByHash = paramType.Equals(PXYZConstants.HASH_PARAM);

            ignoreExtensions = new []{".mp3", ".js", ".lib", ".json", ".md", ".wav", ".bin"};
            contentsURL = "https://peer.decentraland.org/content/contents/";
            activeEntitiesURL = "https://peer.decentraland.org/content/entities/active";
        }

        public async Task DownloadSceneDefinition()
        {
            Console.WriteLine("BEGIN SCENE DEFINITION DOWNLOAD");
            currentPointersList = Array.Empty<string>();
            try
            {
                if (paramByHash)
                {
                    string rawSceneDefinition = await webRequestsHandler.GetRequest($"{contentsURL}{sceneHash}");
                    sceneDefinition = JsonConvert.DeserializeObject<SceneDefinition>(rawSceneDefinition);
                    SetResult(sceneHash);
                }
                else
                {
                    string rawSceneDefinition = await webRequestsHandler.PostRequest(activeEntitiesURL, "{\"pointers\":[\"" + sceneParam + "\"]}");
                    List<SceneDefinition> sceneDefinitions =
                        JsonConvert.DeserializeObject<List<SceneDefinition>>(rawSceneDefinition);
                    if (sceneDefinitions.Count > 0)
                    {
                        sceneDefinition = sceneDefinitions[0];
                        SetResult(sceneDefinitions[0].id);
                    }
                }
                Console.WriteLine("END SCENE DEFINITION DOWNLOAD");
            }
            catch (Exception e)
            {
                throw new Exception($"Scene fetch failed: {e}");
            }
        }

        private void SetResult(string setSceneHash)
        {
            this.sceneHash = setSceneHash;
            //TODO: Change to scene base
            scenePointer = sceneDefinition.metadata.scene.baseParcel;
            currentPointersList = sceneDefinition.pointers;
        }

        public string[] GetCurrentScenePointersList()
        {
            return currentPointersList;
        }

        public async Task<bool> DownloadAllContent(SceneConversionDebugInfo debugInfo)
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
                    FileWriter.WriteToFile($"{scenePointer}\tDOWNLOAD ERROR: {e.Message}", debugInfo.FailFile);
                    return false;
                }
            }
            Console.WriteLine("END FILE CONTENT DOWNLOAD");
            return true;
        }


        public string GetSceneHash()
        {
            return sceneHash;
        }

        public string GetScenePointer()
        {
            return scenePointer;
        }
    }
}