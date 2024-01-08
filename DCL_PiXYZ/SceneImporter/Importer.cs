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
            Console.WriteLine("BEGIN IMPORT");
            Console.WriteLine("Getting Scene Definition");
            try
            {
                if (paramByHash)
                {
                    sceneHash = sceneParam;
                    string rawSceneDefinition = await webRequestsHandler.GetRequest($"{contentsURL}{sceneHash}");
                    sceneDefinition = JsonConvert.DeserializeObject<SceneDefinition>(rawSceneDefinition);
                    scenePointer = sceneDefinition.pointers[0];
                }
                else
                {
                    scenePointer = sceneParam;
                    string rawSceneDefinition = await webRequestsHandler.PostRequest(activeEntitiesURL, "{\"pointers\":[\"" + sceneParam + "\"]}");
                    sceneDefinition = JsonConvert.DeserializeObject<List<SceneDefinition>>(rawSceneDefinition)[0];
                    sceneHash = sceneDefinition.id;
                    scenePointer = sceneDefinition.pointers[0];
                }
            }
            catch (HttpRequestException e)
            {
                throw new Exception($"Scene fetch failed: {e}");
            }
            Console.WriteLine("Scene Definition Success!");
        }

        public async Task<Dictionary<string,string>> DownloadAllContent()
        {
            Console.WriteLine("Getting File Content");
            Dictionary<string, string> contentDictionary = new Dictionary<string, string>();
            foreach (var content in sceneDefinition.content)
            {
                try
                {
                    if (ignoreExtensions.Contains(Path.GetExtension(content.file)))
                    {
                        Console.WriteLine($"File {content.file} ignored");
                        continue;
                    }
                    Console.WriteLine($"Getting File {content.file}");
                    string filePath = Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, content.file);
                    await webRequestsHandler.DownloadFileAsync($"{contentsURL}{content.hash}", filePath);
                    contentDictionary.Add(content.file, filePath);
                    Console.WriteLine($"File {content.file} Success!");
                }
                catch (HttpRequestException e)
                {
                    throw new Exception($"URL failed: {contentsURL}{sceneParam} with error {e}");
                }
            }
            Console.WriteLine("File Content Success!");
            Console.WriteLine("END IMPORT");
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