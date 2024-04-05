using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using DCL_PiXYZ.Utils;
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
        
        public async Task ApplyModification(PiXYZAPI pxz)
        {
            foreach (var pxzModel in PXZModels)
            {
                if (pxzModel.needsRepair)
                    pxz.Algo.RepairMesh(new OccurrenceList(new uint[]{pxzModel.modelOcurrence}), 0.1, true, false);
            }
        }
    }
}