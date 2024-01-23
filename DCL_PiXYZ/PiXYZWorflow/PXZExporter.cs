using System;
using System.Collections.Generic;
using System.IO;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZExporter : IPXZModifier
    {
        private string extension;
        private string path;
        private string filename;

        public PXZExporter(string path, string filename, string extension)
        {
            this.extension = extension;
            this.path = path;
            this.filename = filename;
        }

        public void ApplyModification(PiXYZAPI pxz)
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine($"BEGIN PXZ EXPORT {filename}{extension}");
            Directory.CreateDirectory(path);
            //Use it to flatten the hierarchy
            //TODO: This will break all possible skinning. But do we care about it?
            pxz.Scene.MergeOccurrencesByTreeLevel(new OccurrenceList(new[]{pxz.Scene.GetRoot()}),1);
            pxz.IO.ExportScene(Path.Combine(path, $"{filename}{extension}"), pxz.Scene.GetRoot());
            Console.WriteLine("END PXZ EXPORT ");
        }
    }
}