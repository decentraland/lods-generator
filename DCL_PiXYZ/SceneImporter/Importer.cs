﻿using System;
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
        private string baseURL;
        private string sceneID;
        private WebRequestsHandler webRequestsHandler;
        private SceneDefinition sceneDefinition;
        private string[] ignoreExtensions;

        private bool debugLog;
        public Importer(string sceneID, string baseURL, WebRequestsHandler webRequestsHandler)
        {
            this.baseURL = baseURL;
            this.sceneID = sceneID;
            this.webRequestsHandler = webRequestsHandler;
            ignoreExtensions = new []{".mp3", ".js", ".lib", ".json", ".md", ".wav", ".bin"};
        }

        public async Task GenerateSceneContent()
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN IMPORT");
            Console.WriteLine("Getting Scene Definition");
            try
            {
                string rawSceneDefinition = await webRequestsHandler.FetchStringAsync($"{baseURL}{sceneID}");
                sceneDefinition = JsonConvert.DeserializeObject<SceneDefinition>(rawSceneDefinition);
            }
            catch (HttpRequestException e)
            {
                throw new Exception($"URL failed: {baseURL}{sceneID}");
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
                    await webRequestsHandler.DownloadFileAsync($"{baseURL}{content.hash}", filePath);
                    if (filePath.Contains("https"))
                    {
                        Console.WriteLine("ASdQQ");
                    }
                    contentDictionary.Add(content.file, filePath);
                    Console.WriteLine($"File {content.file} Success!");
                }
                catch (HttpRequestException e)
                {
                    throw new Exception($"URL failed: {baseURL}{sceneID}");
                }
            }
            Console.WriteLine("File Content Success!");
            Console.WriteLine("END IMPORT");
            return contentDictionary;
        }


    }
}