using UnityEngine.Pixyz.API;

namespace DCL_PiXYZ
{
    public class PXZDeleteEmptyOccurrences : IPXZModifier
    {
        public void ApplyModification(PiXYZAPI pxz)
        {
            pxz.Scene.DeleteEmptyOccurrences(pxz.Scene.GetRoot());
        }
    }
}