using System.IO;
using CommandLine;

namespace DCL_PiXYZ.Utils
{
    public class PXZEntryArgs
    {
        public PXZEntryArgs()
        {
            DefaultOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "built-lods");
            DefaultSceneLodManifestDirectory = Path.Combine(Directory.GetCurrentDirectory(), "scene-lod-entities-manifest-builder/");
        }
        
        [Option("sceneToConvert", Required = false, Default = "0,0", HelpText = "The scene coordinate to convert")]
        public string SceneToConvert { get; set; }
        
        [Option("defaultOutputPath", Required = false, HelpText = "Output path for all files (LODs and Downloads)")]
        public string DefaultOutputPath { get; set; }
        
        [Option("defaultSceneLodManifestDirectory", Required = false, HelpText = "Path to the manifest project")]
        public string DefaultSceneLodManifestDirectory { get; set; }
        
        [Option("decimationValues", Required = false, Default = "7000;500", HelpText = "Triangle max count per lod level. Separate each leavel by a ;") ]
        public string DecimationValues { get; set; }
        
        [Option("startingLODLevel", Required = false, Default = 0, HelpText = "Starting LOD level to generate. Modifiers depend on this value") ]
        public int StartingLODLevel { get; set; }
        
        [Option("loadConvertedScenesFile", Required = false, Default = false, HelpText = "Load converted scenes file. Allows filtering of previous converted scenes")]
        public bool LoadConvertedScenesFile { get; set; }
        
        [Option("debugMode", Required = false, Default = false, HelpText = "If true, all debug info will go to a single file in root level and generated manifest wont be deleted")]
        public bool DebugMode { get; set; }
        
        [Option("installNPM", Required = false, Default = false, HelpText = "Install npm and build the manifest project.")]
        public bool InstallNPM { get; set; }

    }
}