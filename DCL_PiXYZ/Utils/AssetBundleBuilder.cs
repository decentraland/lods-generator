using System;
using System.Diagnostics;
using System.IO;

namespace DCL_PiXYZ.Utils
{
    public class AssetBundleBuilder
    {
        private readonly string unityPath;
        private readonly string assetBundleConverterPath;

        public AssetBundleBuilder(string unityPath, string assetBundleConverterPath)
        {
            this.unityPath = unityPath;
            this.assetBundleConverterPath = assetBundleConverterPath;
        }

        public void RunAssetBundleConversion(bool runAssetBundleConversion, int finalLodLevel, SceneConversionPathHandler pathHandler, string sceneHash)
        {
            if (!runAssetBundleConversion)
                return;

            string lodsPath = "";
            for (int i = 0; i < finalLodLevel; i++)
                lodsPath += "file://" + Path.Combine(pathHandler.OutputPathWithBasePointer, $"{sceneHash}_{i}.fbx") + ";";

            lodsPath = lodsPath.Remove(lodsPath.Length - 1);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell", Arguments = $"Start-Process -FilePath '{unityPath}'" +
                                                         $"-ArgumentList '-projectPath','asset-bundle-converter','-batchmode','-executeMethod','DCL.ABConverter.LODClient.ExportURLLODsToAssetBundles','-lods','{lodsPath}','-output','{pathHandler.OutputPathWithAssetBundle}','-logFile','./tmp/log.txt' -Wait", // replace with your npm command
                    WorkingDirectory = assetBundleConverterPath, RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            using (process)
            {
                process.Start();
                process.WaitForExit();
                Console.WriteLine("Standard Output:");
                Console.WriteLine(process.StandardOutput.ReadToEnd());
                Console.WriteLine("Standard Error:");
                Console.WriteLine(process.StandardError.ReadToEnd());
            }
        }
    }
}