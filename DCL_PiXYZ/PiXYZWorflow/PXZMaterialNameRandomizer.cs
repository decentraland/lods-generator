using System;
using System.Threading.Tasks;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Material;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZMaterialNameRandomizer : IPXZModifier
    {
        public async Task ApplyModification(PiXYZAPI pxz)
        {
            pxz.Scene.MergeImages();
            PackedTree packedTree = pxz.Scene.GetSubTree(pxz.Scene.GetRoot());
            for (var i = 0; i < packedTree.occurrences.list.Length; i++)
            {
                if (pxz.Scene.HasComponent(packedTree.occurrences[i], ComponentType.Part) && !packedTree.names[i].Contains("collider"))
                {
                    MaterialList material = pxz.Scene.GetMaterialsFromSubtree(packedTree.occurrences[i]);
                    for (var j = 0; j < material.list.Length; j++)
                    {
                        Random random = new Random();
                        string nameToSet = random.Next(0, 1000).ToString();
                        string currentName = pxz.Core.GetProperty(material.list[j], "Name");
                        if (currentName.Contains(PXZConstants.FORCED_TRANSPARENT_MATERIAL))
                            nameToSet += PXZConstants.FORCED_TRANSPARENT_MATERIAL;
                        pxz.Core.SetProperty(material.list[j], "Name", nameToSet);
                    }
                }
            }
        }
    }
}