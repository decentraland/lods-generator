using System;
using System.IO;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZExporter : IPXZModifier
    {
        public OccurrenceList ApplyModification(PiXYZAPI pxz, OccurrenceList occurrenceList)
        {
            //pxz.Core.Save(Path.Combine("C:/Users/juanm/Documents/Decentraland/asset-bundle-converter/asset-bundle-converter/Assets/Resources", $"Combined_Meshes_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}"));
            
            pxz.IO.ExportScene(Path.Combine("C:/Users/juanm/Documents/Decentraland/asset-bundle-converter/asset-bundle-converter/Assets/Resources",
                $"Combined_Meshes_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.glb"), occurrenceList[0]);
            return new OccurrenceList();
        }
    }
}