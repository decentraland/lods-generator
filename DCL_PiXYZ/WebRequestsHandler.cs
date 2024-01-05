using System.IO;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace DCL_PiXYZ
{
    public class WebRequestsHandler
    {
        public async Task<string> GetRequest(string uri)
        {
            HttpClient client = new HttpClient();
            using (HttpResponseMessage response = await client.GetAsync(uri))
            {
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Error: {response.StatusCode}");
            
                return await response.Content.ReadAsStringAsync();
            }
        }
        
        public async Task<string> PostRequest(string uri, string jsonData)
        {
            HttpClient client = new HttpClient();
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            using (HttpResponseMessage response = await client.PostAsync(uri, content))
            {
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Error: {response.StatusCode}");
            
                return await response.Content.ReadAsStringAsync();
            }
        }
        
        public async Task DownloadFileAsync(string uri, string destinationPath)
        {
            if (CheckFileExists(destinationPath))
                return;
            
            CheckDirectory(destinationPath);
            HttpClient client = new HttpClient();
            using (HttpResponseMessage response = await client.GetAsync(uri))
            {
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Error: {response.StatusCode}");
            
                using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync())
                {
                    using (Stream streamToWriteTo = File.Open(destinationPath, FileMode.Create))
                        await streamToReadFrom.CopyToAsync(streamToWriteTo);
                }
            }

        }

        private bool CheckFileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        private void CheckDirectory(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);

            // Check if the directory exists
            if (!Directory.Exists(directory))
            {
                // If not, create the directory
                Directory.CreateDirectory(directory);
            }
        }
    }
}