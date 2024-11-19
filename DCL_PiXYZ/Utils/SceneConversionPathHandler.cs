using System.IO;

namespace DCL_PiXYZ.Utils
{
public struct SceneConversionPathHandler
    {
        public string SuccessFile;
        public string FailFile;
        public string PolygonCountFile;
        public string FailGLBImporterFile;
        public string OutputPath;
        public string DownloadPath;
        public string ManifestOutputJsonFile;
        public string ManifestOutputJsonDirectory;
        public string ManifestProjectDirectory;
        public string sceneBasePointer;

        private readonly string DefaultOutputPath;

        private bool isDebug;

        public SceneConversionPathHandler(bool isDebug, string defaultOutputPath, string manifestProjectDirectory)
        {
            DefaultOutputPath = defaultOutputPath;
            ManifestProjectDirectory = manifestProjectDirectory;
            DownloadPath = PXZConstants.RESOURCES_DIRECTORY;
            this.isDebug = isDebug;

            OutputPath = "";
            ManifestOutputJsonFile = "";
            ManifestOutputJsonDirectory = "";
            
            SuccessFile = "";
            FailFile = "";
            PolygonCountFile = "";
            FailGLBImporterFile = "";
            sceneBasePointer = "";
        }

        public void SetOutputPath(SceneImporter sceneSceneImporter)
        {
            sceneBasePointer = sceneSceneImporter.GetSceneBasePointer();
            DownloadPath = Path.Combine(PXZConstants.RESOURCES_DIRECTORY, sceneBasePointer);
            OutputPath = Path.Combine(DefaultOutputPath, sceneBasePointer);
            ManifestOutputJsonDirectory = Path.Combine(ManifestProjectDirectory, "output-manifests");
            ManifestOutputJsonFile = Path.Combine(ManifestOutputJsonDirectory, sceneSceneImporter.GetSceneHash() + "-lod-manifest.json");

            if (isDebug)
            {
                Directory.CreateDirectory(DefaultOutputPath);
                SuccessFile = Path.Combine(DefaultOutputPath, "SuccessScenes.txt");
                FailFile = Path.Combine(DefaultOutputPath, "FailScenes.txt");
                PolygonCountFile =  Path.Combine(DefaultOutputPath, "PolygonCount.txt");
                FailGLBImporterFile = Path.Combine(DefaultOutputPath, "FailedGLBImport.txt");
            }
            else
            {
                //TODO: Clean this directory issue
                Directory.CreateDirectory(Path.Combine(DefaultOutputPath, sceneBasePointer));
                string pathWithBasePointer = $"{sceneBasePointer}/output.txt";
                SuccessFile = Path.Combine(DefaultOutputPath, pathWithBasePointer);
                FailFile = Path.Combine(DefaultOutputPath, pathWithBasePointer);
                PolygonCountFile =  Path.Combine(DefaultOutputPath, pathWithBasePointer);
                FailGLBImporterFile =  Path.Combine(DefaultOutputPath, pathWithBasePointer);
            }

            Directory.CreateDirectory(DownloadPath);
            Directory.CreateDirectory(DefaultOutputPath);
            Directory.CreateDirectory(OutputPath);
        }
        
    }
}