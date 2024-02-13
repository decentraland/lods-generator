using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DCL_PiXYZ.Utils
{
    public class RoadWorldBuilder
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string url = "https://peer.decentraland.org/content/entities/active/";
        private static readonly string singleRoad = Path.Combine(Directory.GetCurrentDirectory(), "SingleRoadScenes.txt");
        private static readonly string multipleRoad = Path.Combine(Directory.GetCurrentDirectory(), "MultipleRoadScenes.txt");

        private static readonly List<string> writtenRoads = new List<string>();

        public static async Task BuildRoadsFile()
        {
            var pointers = GeneratePointers(-150, 150);
            for (int i = 0; i < pointers.Count; i += 30)
            {
                var batch = pointers.GetRange(i, Math.Min(40, pointers.Count - i));
                string response = await PostRequestWithRetry(batch, 3);
                if (response != null)
                    WriteRoadTitlesToFile(response);
            }
        }

        private static List<string> GeneratePointers(int start, int end)
        {
            var pointers = new List<string>();
            for (int x = start; x <= end; x++)
            {
                for (int y = start; y <= end; y++)
                {
                    pointers.Add($"{x},{y}");
                }
            }

            return pointers;
        }

        private static async Task<string> PostRequestWithRetry(List<string> pointers, int retries)
        {
            for (int attempt = 0; attempt < retries; attempt++)
            {
                try
                {
                    string requestBody = JsonConvert.SerializeObject(new
                    {
                        pointers
                    });
                    var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        return responseBody;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");
                }
            }

            return null;
        }

        private static void WriteRoadTitlesToFile(string responseBody)
        {
            // Parse the response body to a JObject
            var response = JArray.Parse(responseBody);

            // Open the file once and use StreamWriter to append lines
            using (StreamWriter singleFile = new StreamWriter(singleRoad, true), multipleFile = new StreamWriter(multipleRoad, true))
            {
                foreach (var entity in response)
                {
                    // Use the JObject accessor and Value<string> method to get the title
                    string title = entity["metadata"]["display"]["title"].Value<string>();
                    string road = entity["metadata"]["scene"]["base"].Value<string>();
                    int parcelAmount = JArray.Parse(entity["metadata"]["scene"]["parcels"].ToString()).Count;
                    if (title.Contains("Road") && !writtenRoads.Contains(road))
                    {
                        if (parcelAmount > 1)
                        {
                            Console.WriteLine("MORE THAN 1 PARCEL " );
                            multipleFile.WriteLine(road);
                        }
                        else
                            singleFile.WriteLine(road);

                        writtenRoads.Add(road);
                    }
                }
            }
        }
    }
}