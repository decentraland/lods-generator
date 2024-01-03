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
        public OccurrenceList ApplyModification(PiXYZAPI pxz, OccurrenceList origin)
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN PXZ MODIFIER DECIMATOR");
            DecimateOptionsSelector decimate = new DecimateOptionsSelector();
            decimate.ratio = 100f;
            decimate._type = DecimateOptionsSelector.Type.RATIO;
            pxz.Algo.DecimateTarget(origin, decimate);
            Console.WriteLine("END PXZ MODIFIER DECIMATOR");
            return origin;
        }
    }
}