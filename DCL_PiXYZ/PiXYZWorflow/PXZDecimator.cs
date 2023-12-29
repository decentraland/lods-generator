using System;
using UnityEngine.Pixyz.Algo;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZDecimator : IPXZModifier
    {
        public OccurrenceList ApplyModification(PiXYZAPI pxz, OccurrenceList occurrenceList)
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN PXZ MODIFIER DECIMATOR");
            DecimateOptionsSelector decimate = new DecimateOptionsSelector();
            decimate.ratio = 100f;
            decimate._type = DecimateOptionsSelector.Type.RATIO;
            pxz.Algo.DecimateTarget(occurrenceList, decimate);
            Console.WriteLine("END PXZ MODIFIER DECIMATOR");
            return occurrenceList;
        }
    }
}