using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DCL_PiXYZ.Utils
{
    public class NPMUtils
    {
        public static void DoNPMInstall(string sceneManifestProjectDirectory)
        {
            // Set up the process start information
            ProcessStartInfo install = new ProcessStartInfo
            {
                FileName = "powershell", // or the full path to npm if not in PATH
                Arguments = "npm i", // replace with your npm command
                WorkingDirectory = sceneManifestProjectDirectory,
                RedirectStandardOutput = true, // if you want to read output
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            // Start the process
            using (Process process = Process.Start(install))
            {
                // Read the output (if needed)
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine(result);
                }

                process.WaitForExit(); // Wait for the process to complete
            }
            
            ProcessStartInfo build = new ProcessStartInfo
            {
                FileName = "powershell", // or the full path to npm if not in PATH
                Arguments = "npm run build", // replace with your npm command
                WorkingDirectory = sceneManifestProjectDirectory,
                RedirectStandardOutput = true, // if you want to read output
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Start the process
            using (Process process = Process.Start(build))
            {
                // Read the output (if needed)
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine(result);
                }

                process.WaitForExit(); // Wait for the process to complete
            }
        }
        
        public static async Task<(string, bool)> RunNPMToolAndReturnExceptionIfPresent(string sceneManifestProjectDirectory, string coords, int timeoutInMilliseconds)
        {
            bool isSDK7 = false;
            bool readTimeout = false;
            string exception = "";
            // Set up the process start information
            ProcessStartInfo install = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "npm run start --coords=" + coords, 
                WorkingDirectory = sceneManifestProjectDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true, 
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            // Start the process
            using (Process process = Process.Start(install))
            {
                //TODO: Not sure I have to wait after printing output for the process to end. Thats why it has this weird place
                if (process.WaitForExit(timeoutInMilliseconds))
                {
                    //using (StreamReader reader = process.StandardOutput)
                    //{
                        //string result = reader.ReadToEnd();
                        //Console.WriteLine(result);
                        //isSDK7 = !result.Contains("sdk7? false");
                    //}
                    
                    // Read the output (if needed)
                    using (StreamReader reader = process.StandardError)
                    {
                        Task<string> readTask = reader.ReadLineAsync();
                        if (await Task.WhenAny(readTask, Task.Delay(500)) == readTask)
                            exception = readTask.Result;
                        else
                            readTimeout = true;
                    }
                    
                    return (exception, readTimeout);
                }

                process.Kill();

                using (StreamReader reader = process.StandardError)
                {
                    Task<string> readTask = reader.ReadLineAsync();
                    if (await Task.WhenAny(readTask, Task.Delay(2000)) == readTask)
                        exception = readTask.Result;
                    else
                        readTimeout = true;
                }
                    
                return (exception, readTimeout);
            }
        }

        public static void RunNPMTool(string sceneManifestProjectDirectory, string coords)
        {
            // Set up the process start information
            ProcessStartInfo install = new ProcessStartInfo
            {
                FileName = "powershell", // or the full path to npm if not in PATH
                Arguments = "npm run start --coords=" + coords, // replace with your npm command
                WorkingDirectory = sceneManifestProjectDirectory,
                RedirectStandardOutput = true, // if you want to read output
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Start the process
            using (Process process = Process.Start(install))
            {
                // Read the output (if needed)
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine(result);
                }

                process.WaitForExit(); // Wait for the process to complete
            }
        }
    }
}