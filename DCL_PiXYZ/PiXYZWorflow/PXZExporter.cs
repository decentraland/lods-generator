﻿using System;
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
        private string path;
        private string filename;
        private readonly int lodLevel;
        private readonly SceneConversionPathHandler pathHandler;

        public PXZExporter(PXZParams pxzParams, SceneConversionPathHandler pathHandler, SceneConversionInfo sceneConversionInfo)
        {
            this.pathHandler = pathHandler;
            path = pathHandler.OutputPath;
            filename = $"{sceneConversionInfo.SceneImporter.GetSceneHash()}_{pxzParams.LodLevel}";
            lodLevel = pxzParams.LodLevel;
        }
        
        public void ApplyModification(PiXYZAPI pxz)
        {
            FileWriter.WriteToConsole($"BEGIN PXZ EXPORT {Path.Combine(path, $"{filename}.fbx")}");
            //Use it to flatten the hierarchy
            if (lodLevel != 0)
            {
                pxz.Scene.MergeOccurrencesByTreeLevel(new OccurrenceList(new[]
                {
                    pxz.Scene.GetRoot()
                }), 1);
            }
            pxz.IO.ExportScene(Path.Combine(path, $"{filename}.fbx"), pxz.Scene.GetRoot());
        }


    }
}