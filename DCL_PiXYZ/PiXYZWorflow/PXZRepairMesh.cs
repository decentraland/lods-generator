using System;
using System.Collections.Generic;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZRepairMesh : IPXZModifier
    {
        
        private List<PXZModel> PXZModels;
        public PXZRepairMesh(List<PXZModel> pxzModels)
        {
            this.PXZModels = pxzModels;
        }
        
        public OccurrenceList ApplyModification(PiXYZAPI pxz, OccurrenceList origin)
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine($"BEGIN PXZ MESH REPAIR");
            foreach (var pxzModel in PXZModels)
            {
                if (pxzModel.needsRepair)
                    pxz.Algo.RepairMesh(new OccurrenceList(new uint[]{pxzModel.modelOcurrence}), 0.1, true, false);
            }
            Console.WriteLine($"END PXZ MESH REPAIR");
            return origin;
        }
    }
}