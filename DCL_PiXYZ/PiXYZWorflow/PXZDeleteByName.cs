using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZDeleteByName : IPXZModifier
    {
        public OccurrenceList ApplyModification(PiXYZAPI pxz, OccurrenceList occurrenceList)
        {
            OccurrenceList occurenceToDelete = pxz.Scene.FindOccurrencesByProperty("Name", ".*_collider");
            pxz.Scene.DeleteOccurrences(occurenceToDelete);
            return occurrenceList;
        }
    }
}