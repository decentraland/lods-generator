using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using DCL_PiXYZ.Utils;
using UnityEngine.Pixyz.Algo;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZDecimator : IPXZModifier
    {
        private DecimateOptionsSelector decimate;
        private string scenePointer;
        private ulong originalPolygonCount;
        private readonly SceneConversionPathHandler _pathHandler;

        public PXZDecimator(string scenePointer, string decimationType, double decimationParam, int parcelAmount, SceneConversionPathHandler pathHandler)
        {
            decimate = new DecimateOptionsSelector();
            if (decimationType.Equals("triangle"))
            {
                decimate._type = DecimateOptionsSelector.Type.TRIANGLECOUNT;
                decimate.triangleCount = (ulong)(decimationParam * parcelAmount);
            }
            else
            {
                decimate._type = DecimateOptionsSelector.Type.RATIO;
                decimate.ratio = decimationParam;
            }

            _pathHandler = pathHandler;
            this.scenePointer = scenePointer;
        } 
        
        public async Task ApplyModification(PiXYZAPI pxz)
        {
            Console.WriteLine("BEGIN PXZ MODIFIER DECIMATOR");
            originalPolygonCount =
                pxz.Scene.GetPolygonCount(new OccurrenceList(new uint[] { pxz.Scene.GetRoot() }), true);
            pxz.Algo.DecimateTarget(new OccurrenceList(new uint[]{pxz.Scene.GetRoot()}), decimate);
            WriteFinalVertexAmount(pxz.Scene.GetPolygonCount(new OccurrenceList(new uint[] { pxz.Scene.GetRoot() }),true));
            Console.WriteLine("END PXZ MODIFIER DECIMATOR");
        }
        
        private void WriteFinalVertexAmount(ulong polygonCount)
        {
            if (decimate._type == DecimateOptionsSelector.Type.TRIANGLECOUNT)
                FileWriter.WriteToFile($"{scenePointer}\t{decimate._type}\t{decimate.triangleCount}\t{originalPolygonCount}\t{polygonCount}", _pathHandler.PolygonCountFile);
            else
                FileWriter.WriteToFile($"{scenePointer}\t{decimate._type}\t{decimate.ratio}\t{originalPolygonCount}\t{polygonCount}", _pathHandler.PolygonCountFile);
        }
    }
}