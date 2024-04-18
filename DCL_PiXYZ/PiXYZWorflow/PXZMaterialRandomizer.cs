using System;
using System.Threading.Tasks;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Material;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZMaterialNameRandomizer : IPXZModifier
    {
        public void ApplyModification(PiXYZAPI pxz)
        {
            //There is a bug in the PXZ FBX exporter. If two materials have the same name, they are incorrectly assigned
            //at export time
            int materialName = 0;
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
                        string nameToSet = materialName.ToString();
                        string currentName = pxz.Core.GetProperty(material.list[j], "Name");
                        if (currentName.Contains(PXZConstants.FORCED_TRANSPARENT_MATERIAL))
                            nameToSet += PXZConstants.FORCED_TRANSPARENT_MATERIAL;
                        pxz.Core.SetProperty(material.list[j], "Name", nameToSet);
                        materialName++;
                    }
                }
            }
        }
    }
}