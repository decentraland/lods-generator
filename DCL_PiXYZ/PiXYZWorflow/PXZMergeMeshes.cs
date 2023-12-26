using UnityEngine.Pixyz.Algo;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZMergeMeshes : IPXZModifier
    {
        public OccurrenceList ApplyModification(PiXYZAPI pxz, OccurrenceList occurrenceList)
        {
            BakeOption bakeOption = new BakeOption();
            BakeMaps bakeMaps = new BakeMaps();
            bakeMaps.diffuse = true;
            bakeOption.bakingMethod = BakingMethod.ProjOnly;
            bakeOption.padding = 0;
            bakeOption.resolution = 256;
            bakeOption.textures = bakeMaps;
            uint combinedMesh = pxz.Algo.CombineMeshes(occurrenceList, bakeOption);
            return new OccurrenceList(new uint[] { combinedMesh });
        }
    }
}