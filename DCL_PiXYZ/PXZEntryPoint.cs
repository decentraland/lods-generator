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
            await pxzClient.RunLODBuilder(obj);
        }
        
        public static void CloseApplication(string errorMessage)
        {
            Console.Error.WriteLine(errorMessage);
            Environment.Exit(1);
        }

        
    }
}
