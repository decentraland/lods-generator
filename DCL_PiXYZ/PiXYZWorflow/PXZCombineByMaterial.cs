using System;
using System.Threading.Tasks;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZCombineByMaterial : IPXZModifier
    {
        public async Task ApplyModification(PiXYZAPI pxz)
        {
            //pxz.Algo.CombineMeshesByMaterials(new OccurrenceList(new[] { pxz.Scene.GetRoot() }));
        }
    }
}