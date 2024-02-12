﻿using System;
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
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            string firstErrorLine = "";
            using (process)
            {
                process.OutputDataReceived += (sender, args) => { outputBuilder.AppendLine(args.Data); };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (string.IsNullOrEmpty(firstErrorLine))
                        firstErrorLine = args.Data;
                    errorBuilder.AppendLine(args.Data);
                };

                process.Start();
            
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => 
                {               
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