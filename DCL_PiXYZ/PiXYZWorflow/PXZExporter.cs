using System;
using System.IO;
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

        public OccurrenceList ApplyModification(PiXYZAPI pxz, OccurrenceList occurrenceList)
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine($"BEGIN PXZ EXPORT {filename}{extension}");
            pxz.IO.ExportScene(Path.Combine(path, $"{filename}{extension}"), occurrenceList[0]);
            Console.WriteLine("END PXZ EXPORT ");
            return new OccurrenceList();
        }
    }
}