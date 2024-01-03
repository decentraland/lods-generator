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
        public void ApplyModification(PiXYZAPI pxz)
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN PXZ MODIFIER DECIMATOR");
            DecimateOptionsSelector decimate = new DecimateOptionsSelector();
            decimate.ratio = 100f;
            decimate._type = DecimateOptionsSelector.Type.RATIO;
            pxz.Algo.DecimateTarget(new OccurrenceList(new uint[]{pxz.Scene.GetRoot()}), decimate);
            Console.WriteLine("END PXZ MODIFIER DECIMATOR");
        }
    }
}