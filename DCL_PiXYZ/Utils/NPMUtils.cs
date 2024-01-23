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
        
        public static string RunNPMTool(string sceneManifestProjectDirectory, string sceneType, string sceneValue)
        {
            // Set up the process start information
            ProcessStartInfo install = new ProcessStartInfo
            {
                FileName = "powershell", // or the full path to npm if not in PATH
                Arguments = $"npm run start --{sceneType}={sceneValue}", // replace with your npm command
                WorkingDirectory = sceneManifestProjectDirectory,
                RedirectStandardError = true,
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
                }
                
                using (StreamReader reader = process.StandardError)
                {
                    string errorLine = reader.ReadLine();
                    if (!string.IsNullOrEmpty(errorLine))
                        return errorLine;
                }

                process.WaitForExit(); // Wait for the process to complete
            }

            return "";
        }
    }
}