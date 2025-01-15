using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZDeleteInvalidTransforms : IPXZModifier
    {
        public void ApplyModification(PiXYZAPI pxz)
        {
            OccurrenceList occs = pxz.Scene.GetFilteredOccurrences("Property(\"Transform\").Matches(\".*nan.*\")") ;
            pxz.Scene.DeleteOccurrences(occs);
        }
    }
}