using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZFlattenHierarchy : IPXZModifier
    {
        public void ApplyModification(PiXYZAPI pxz)
        {
            pxz.Scene.MergeOccurrencesByTreeLevel(new OccurrenceList(new[]
            {
                pxz.Scene.GetRoot()
            }), 1, MergeHiddenPartsMode.Destroy);
        }
    }
}