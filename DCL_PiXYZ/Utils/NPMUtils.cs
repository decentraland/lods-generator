using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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
                    FileWriter.WriteToConsole(result);
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
                    FileWriter.WriteToConsole(result);
                }

                process.WaitForExit(); // Wait for the process to complete
            }
        }
        
        public static async Task<string> RunNPMTool(string sceneManifestProjectDirectory, string sceneType, string sceneValue)
        {
            //TODO: I need standard output for it to work. Why?
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell", // or the full path to npm if not in PATH
                    Arguments = $"npm run start --{sceneType}={sceneValue}", // replace with your npm command
                    WorkingDirectory = sceneManifestProjectDirectory,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true, // if you want to read output
                    UseShellExecute = false,
                    CreateNoWindow = true
                }, 
                EnableRaisingEvents = true
            };
            
            string firstErrorLine = "";
            using (process)
            {
                process.OutputDataReceived += (sender, args) => { };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (string.IsNullOrEmpty(firstErrorLine))
                        firstErrorLine = args.Data;
                };

                process.Start();
            
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => 
                {     
                //Timeout is based on the current frame rate (90 frames * 33ms)
                //No scene should take longer than 2970ms to finish running frame,
                //but all the setup may take time. So we are leaving a big and safe margin
                if (!process.WaitForExit(20_000))
                    process.Kill();
                });
                
                if (!string.IsNullOrEmpty(firstErrorLine))
                    return firstErrorLine;
            }

            return "";
        }
    }
}