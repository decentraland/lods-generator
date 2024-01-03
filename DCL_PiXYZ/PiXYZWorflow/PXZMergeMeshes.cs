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
        public void ApplyModification(PiXYZAPI pxz)
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
            pxz.Algo.CombineMeshes(new OccurrenceList(new uint[]{pxz.Scene.GetRoot()}), bakeOption);
            Console.WriteLine("END PXZ MERGE MESHES FOR SETTINGS " + pxz.Core.GetVersion());
        }
    }
}