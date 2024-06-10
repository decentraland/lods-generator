using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using DCL_PiXYZ.Utils;
using Newtonsoft.Json;
using SceneImporter;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Core;

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
