using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using DCL_PiXYZ.Utils;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZExporter : IPXZModifier
    {
        private List<string> extensions;
        private string path;
        private string filename;
        private readonly int lodLevel;
        private readonly SceneConversionDebugInfo debugInfo;

        public PXZExporter(PXZParams pxzParams, SceneConversionDebugInfo debugInfo)
        {
            this.debugInfo = debugInfo;
            extensions = new List<string>() { ".fbx", ".glb" };
            path = pxzParams.OutputDirectory;
            filename = $"{pxzParams.SceneHash}_{pxzParams.LodLevel}";
            lodLevel = pxzParams.LodLevel;
        }
        
        public async Task ApplyModification(PiXYZAPI pxz)
        {
            int currentExtensionTried = 0;
            bool exportSucceeded = false;

            while (currentExtensionTried < extensions.Count)
            {
                var exportTask = Task.Run(() => DoExportWithExtension(pxz, extensions[currentExtensionTried]));
                var completedTask = await Task.WhenAny(exportTask, Task.Delay(TimeSpan.FromMinutes(5)));

                if (completedTask == exportTask)
                {
                    // Export completed before the timeout
                    exportSucceeded = true;
                    break; // Break out of the loop if export succeeds
                }
                else
                {
                    // Handle the timeout case here
                    FileWriter.WriteToFile($"Export for file {filename} timed out for extension {extensions[currentExtensionTried]}", debugInfo.FailFile);
                    currentExtensionTried++; // Move on to the next extension
                }
            }

            if (!exportSucceeded)
                FileWriter.WriteToFile($"All extensions failed for {filename}", debugInfo.FailFile);
        }


        private void DoExportWithExtension(PiXYZAPI pxz, string extension)
        {
            Console.WriteLine($"BEGIN PXZ EXPORT {filename}{extension}");
            Directory.CreateDirectory(path);
            //Use it to flatten the hierarchy
            //TODO: This will break all possible skinning. But do we care about it?
            if (lodLevel != 0)
            {
                pxz.Scene.MergeOccurrencesByTreeLevel(new OccurrenceList(new[]
                {
                    pxz.Scene.GetRoot()
                }), 1);
            }
            pxz.IO.ExportScene(Path.Combine(path, $"{filename}{extension}"), pxz.Scene.GetRoot());
            Console.WriteLine("END PXZ EXPORT ");
       }
    }
}