using System;
using System.Collections.Generic;
using System.IO;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
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

        public PXZDecimator(string scenePointer, string decimationType, double decimationParam, int parcelAmount)
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
            this.scenePointer = scenePointer;
        } 
        
        public void ApplyModification(PiXYZAPI pxz)
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN PXZ MODIFIER DECIMATOR");
            originalPolygonCount =
                pxz.Scene.GetPolygonCount(new OccurrenceList(new uint[] { pxz.Scene.GetRoot() }), true);
            pxz.Algo.DecimateTarget(new OccurrenceList(new uint[]{pxz.Scene.GetRoot()}), decimate);
            WriteFinalVertexAmount(pxz.Scene.GetPolygonCount(new OccurrenceList(new uint[] { pxz.Scene.GetRoot() }),true));
            Console.WriteLine("END PXZ MODIFIER DECIMATOR");
            Console.WriteLine("-------------------------");
        }
        
        private void WriteFinalVertexAmount(ulong polygonCount)
        {
            using (StreamWriter file = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(),"FinalCountVertex.txt"), true))
                if (decimate._type == DecimateOptionsSelector.Type.TRIANGLECOUNT)
                    file.WriteLine($"{scenePointer}\t{decimate._type}\t{decimate.triangleCount}\t{originalPolygonCount}\t{polygonCount}");
                else
                    file.WriteLine($"{scenePointer}\t{decimate._type}\t{decimate.ratio}\t{originalPolygonCount}\t{polygonCount}");
                
        }
    }
}