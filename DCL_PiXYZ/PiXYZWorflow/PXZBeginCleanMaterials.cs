using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Material;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZBeginCleanMaterials : IPXZModifier
    {

        private Dictionary<string, uint> materialDictionary;

        public PXZBeginCleanMaterials()
        {
            materialDictionary = new Dictionary<string, uint>();
        }
        
        public async Task ApplyModification(PiXYZAPI pxz)
        {
            Console.WriteLine("BEGIN PXZ CLEAN MATERIALS");
            pxz.Scene.MergeImages();
            /*
             Merge images seems to solve the issue. Leaving the code here until MergeImages() its 100% proven and fail proof
             PackedTree packedTree = pxz.Scene.GetSubTree(pxz.Scene.GetRoot());
            for (var i = 0; i < packedTree.occurrences.list.Length; i++)
            {
                if (pxz.Scene.HasComponent(packedTree.occurrences[i], ComponentType.Part) && !packedTree.names[i].Contains("collider"))
                {
                    MaterialList material = pxz.Scene.GetMaterialsFromSubtree(packedTree.occurrences[i]);
                    for (var j = 0; j < material.list.Length; j++)
                    {
                        PBRMaterialInfos materialInfos = pxz.Material.GetPBRMaterialInfos(material.list[j]);
                        string materialName = materialInfos.name;
                        if (materialName.Contains(PXYZConstants.CUSTOM_MATERIAL_CONVERTED))
                            continue;
                        if (!materialDictionary.ContainsKey(materialName))
                        {
                            uint copyMaterial 
                                = pxz.Material.CopyMaterial(material.list[j], true);
                            pxz.Core.SetProperty(copyMaterial, "Name", $"{PXYZConstants.CUSTOM_MATERIAL_CONVERTED}_{materialName}");
                            materialDictionary.Add(materialName, copyMaterial);
                        }
                        //TODO: When I modify it, does it change for all references with the same name?
                        pxz.Scene.ReplaceMaterial(material[j], materialDictionary[materialName],
                            new OccurrenceList(new uint[]
                            {
                                packedTree.occurrences[i]
                            }));
                    }
                    pxz.Scene.CleanUnusedImages();
                    pxz.Scene.CleanUnusedMaterials(true);
                }
            }*/

            Console.WriteLine("END PXZ CLEAN MATERIALS");
        }
    }
}