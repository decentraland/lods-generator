using System;
using System.Collections.Generic;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using UnityEngine.Pixyz.Algo;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZMergeMeshes : IPXZModifier
    {
        public OccurrenceList ApplyModification(PiXYZAPI pxz, OccurrenceList origin)
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN PXZ MERGE MESHES FOR SETTINGS " + pxz.Core.GetVersion());
            BakeOption bakeOption = new BakeOption();
            BakeMaps bakeMaps = new BakeMaps();
            bakeMaps.diffuse = true;
            bakeOption.bakingMethod = BakingMethod.RayOnly;
            bakeOption.resolution = 1024;
            bakeOption.padding = 1;
            bakeOption.textures = bakeMaps;
            uint combinedMesh = pxz.Algo.CombineMeshes(origin, bakeOption);
            Console.WriteLine("END PXZ MERGE MESHES FOR SETTINGS " + pxz.Core.GetVersion());
            return new OccurrenceList(new[] { combinedMesh });
        }
    }
}