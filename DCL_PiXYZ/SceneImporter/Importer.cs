using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DCL_PiXYZ;
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
        public Importer(string paramType, string sceneParam, WebRequestsHandler webRequestsHandler)
        {
            this.sceneParam = sceneParam;
            this.webRequestsHandler = webRequestsHandler;

            paramByHash = paramType.Equals(PXYZConstants.HASH_PARAM);

            ignoreExtensions = new []{".mp3", ".js", ".lib", ".json", ".md", ".wav", ".bin"};
            contentsURL = "https://peer.decentraland.org/content/contents/";
            activeEntitiesURL = "https://peer.decentraland.org/content/entities/active";
        }

        public async Task GenerateSceneContent()
        {
            Console.WriteLine("-------------------------");
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
                Console.WriteLine("-------------------------");
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
            scenePointer = sceneDefinition.pointers[0];
            currentPointersList = sceneDefinition.pointers;
        }

        public string[] GetCurrentScenePointersList()
        {
            return currentPointersList;
        }

        public async Task<Dictionary<string,string>> DownloadAllContent()
        {
            Console.WriteLine("BEGIN FILE CONTENT DOWNLOAD");
            Dictionary<string, string> contentDictionary = new Dictionary<string, string>();
            foreach (var content in sceneDefinition.content)
            {
                try
                {
                    if (ignoreExtensions.Contains(Path.GetExtension(content.file)))
                    {
                        //Console.WriteLine($"File {content.file} ignored");
                        continue;
                    }
                    //Console.WriteLine($"Getting File {content.file}");
                    string filePath = Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, content.file);
                    await webRequestsHandler.DownloadFileAsync($"{contentsURL}{content.hash}", filePath);
                    contentDictionary.Add(content.file, filePath);
                    //Console.WriteLine($"File {content.file} Success!");
                }
                catch (Exception e)
                {
                    throw new Exception($"URL failed: {contentsURL}{sceneParam} with error {e}");
                }
            }
            Console.WriteLine("END FILE CONTENT DOWNLOAD");
            return contentDictionary;
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