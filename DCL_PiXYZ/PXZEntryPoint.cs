using System;
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
            FileWriter.PrintDriveSize("ON START PROCESS");
            await pxzClient.RunLODBuilder(obj);
            FileWriter.PrintDriveSize("ON END PROCESS");
        }
        
        public static void CloseApplication(string errorMessage)
        {
            Console.Error.WriteLine(errorMessage);
            Environment.Exit(1);
        }
        

        
    }
}
