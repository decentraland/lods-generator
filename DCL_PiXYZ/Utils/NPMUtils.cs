using System;
using System.Diagnostics;
using System.IO;

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
        
        public static string RunNPMToolAndReturnExceptionIfPresent(string sceneManifestProjectDirectory, string coords)
        {
            // Set up the process start information
            ProcessStartInfo install = new ProcessStartInfo
            {
                FileName = "powershell", // or the full path to npm if not in PATH
                Arguments = "npm run start --coords=" + coords, // replace with your npm command
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
                
                //TODO: Not sure I have to wait after printing output for the process to end. Thats why it has this weird place
                process.WaitForExit(); 

                // Read the output (if needed)
                using (StreamReader reader = process.StandardError)
                    return reader.ReadLine();
                

            }
            return "";
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