using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZRemoveHiddenPartOccurrences : IPXZModifier
    {
        public void ApplyModification(PiXYZAPI pxz)
        {
            pxz.Core.SetModuleProperty("Algo", "DisableGPUAlgorithms", "False");
            //We use low quality settings, since this modifier is used only for far away lods.
            //For more info, check https://www.pixyz-software.com/documentations/html/2024.2/sdk/manual/functions/deleteoccluded.html?q=Occluded
            OccurrenceList hiddenOccurrences = pxz.Algo.FindOccludedPartOccurrences(new OccurrenceList(new uint[]
            {
                pxz.Scene.GetRoot()
            }), 512, 2, 30, true);
            pxz.Scene.DeleteOccurrences(hiddenOccurrences);
        }
    }
}
