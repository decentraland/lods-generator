using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommandLine;
using DCL_PiXYZ.Utils;

namespace DCL_PiXYZ
{
    class PXZEntryPoint
    {
        static async Task Main(string[] args)
        {
            ParserResult<PXZEntryArgs> entryArgs = Parser.Default.ParseArguments<PXZEntryArgs>(args);
            await RunLODGeneration(entryArgs.Value);
        }

        private static async Task RunLODGeneration(PXZEntryArgs obj)
        {
            PXZClient pxzClient = new PXZClient();
            FileWriter.currentScene = obj.SceneToConvert;
            FileWriter.WriteToConsole("PROCESS START");
            FileWriter.PrintDriveSize();
            RunTopCPUProcessesScript();
            await pxzClient.RunLODBuilder(obj);
            FileWriter.WriteToConsole("PROCESS END");
            FileWriter.PrintDriveSize();
            RunTopCPUProcessesScript();
        }
        
        private static void RunTopCPUProcessesScript()
        {
            // Define the PowerShell script to run
            string script = @"
            $topCount = 10;
            Get-Process | Sort-Object CPU -Descending | Select-Object -First $topCount -Property Id, ProcessName, CPU, StartTime | Format-Table -AutoSize
        ";

            // Start the PowerShell process
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-Command \"{script}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                // Read and display the output
                using (System.IO.StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    FileWriter.WriteToConsole("Top 10 CPU-consuming processes:");
                    FileWriter.WriteToConsole(result);
                }
            }
        }
        
        public static void CloseApplication(string errorMessage)
        {
            Console.Error.WriteLine(errorMessage);
            Environment.Exit(1);
        }
        

        
    }
}
