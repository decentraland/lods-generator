using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Material;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZCleanRepeatedTextures : IPXZModifier
    {


        private Dictionary<string, uint> materialDictionary;

        public PXZCleanRepeatedTextures()
        {
            materialDictionary = new Dictionary<string, uint>();
        }
        
        public async Task ApplyModification(PiXYZAPI pxz)
        {
            PackedTree packedTree = pxz.Scene.GetSubTree(pxz.Scene.GetRoot());
            for (var i = 0; i < packedTree.occurrences.list.Length; i++)
            {
                //Means it has a mesh component
                if (pxz.Scene.HasComponent(packedTree.occurrences[i], ComponentType.Part))
                {
                    MaterialList material = pxz.Scene.GetMaterialsFromSubtree(packedTree.occurrences[i]);
                    for (var j = 0; j < material.list.Length; j++)
                    {
                        PBRMaterialInfos materialInfos = pxz.Material.GetPBRMaterialInfos(material.list[j]);
                        string materialName = materialInfos.name;
                        if(materialDictionary.ContainsKey(materialInfos.name))
                            pxz.Scene.SetOccurrenceMaterial(packedTree.occurrences[i], materialDictionary[materialName]);
                        else
                            materialDictionary.Add(materialName, material.list[j]);
                    }                    
                }
            }
            pxz.Scene.CleanUnusedMaterials(true);
        }
    }
}