using UnityEngine.Pixyz.Algo;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZDecimator : IPXZModifier
    {
        public OccurrenceList ApplyModification(PiXYZAPI pxz, OccurrenceList occurrenceList)
        {
            DecimateOptionsSelector decimate = new DecimateOptionsSelector();
            decimate.ratio = 100f;
            decimate._type = DecimateOptionsSelector.Type.RATIO;
            pxz.Algo.DecimateTarget(occurrenceList, decimate);
            return occurrenceList;
        }
    }
}