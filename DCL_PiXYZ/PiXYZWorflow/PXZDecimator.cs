using System;
using System.Collections.Generic;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using UnityEngine.Pixyz.Algo;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZDecimator : IPXZModifier
    {
        private DecimateOptionsSelector decimate;

        public PXZDecimator(DecimateOptionsSelector.Type type, double ratio)
        {
            decimate = new DecimateOptionsSelector();
            decimate.ratio = ratio;
            decimate._type = type;
        } 
        
        public void ApplyModification(PiXYZAPI pxz)
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN PXZ MODIFIER DECIMATOR");
            pxz.Algo.DecimateTarget(new OccurrenceList(new uint[]{pxz.Scene.GetRoot()}), decimate);
            Console.WriteLine("END PXZ MODIFIER DECIMATOR");
        }
    }
}