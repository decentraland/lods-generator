using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZDeleteAllAnimations : IPXZModifier
    {
        public void ApplyModification(PiXYZAPI pxz)
        {
            AnimationList animationList = pxz.Scene.ListAnimations();
            foreach (var animation in animationList.list)
            {
                pxz.Scene.DeleteAnimation(animation);
            }
        }
    }
}