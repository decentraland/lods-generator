using System;
using System.Collections.Generic;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using UnityEngine.Pixyz.Algo;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Material;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZMergeMeshes : IPXZModifier
    {
        public void ApplyModification(PiXYZAPI pxz)
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN PXZ MERGE MESHES");
            BakeOption bakeOption = new BakeOption();
            BakeMaps bakeMaps = new BakeMaps();
            bakeMaps.diffuse = true;
            bakeOption.bakingMethod = BakingMethod.RayOnly;
            bakeOption.resolution = 1024;
            bakeOption.padding = 1;
            bakeOption.textures = bakeMaps;
            OccurrenceList children = pxz.Scene.GetChildren(pxz.Scene.GetRoot());

            /*int amountToMergeCandidate = 40;
            int amountToMergeTogether = children.list.Length < amountToMergeCandidate ? children.list.Length : amountToMergeCandidate;
            for (int i = 0; i < children.list.Length; i += amountToMergeTogether)
            {
                OccurrenceList childrenToMerge = new OccurrenceList();
                for (int j = i; j < i + amountToMergeTogether && j < children.list.Length; j++)
                {
                    childrenToMerge.AddOccurrence(children.list[j]); 
                }
                Console.WriteLine("Merging meshes " + childrenToMerge.list.Length + " iteration " + i);
                uint combineMeshes = pxz.Algo.CombineMeshes(childrenToMerge, bakeOption);
                Console.WriteLine("End merging meshes ");

                Console.WriteLine("Copying Material");
                //Apply a copy of the material not to lose the reference
                MaterialList material = pxz.Scene.GetMaterialsFromSubtree(combineMeshes);
                uint copyMaterial = pxz.Material.CopyMaterial(material.list[0], false);
                pxz.Core.SetProperty(copyMaterial, "Name", "MERGE MATERIAL ITERATION " + i);
                pxz.Scene.SetOccurrenceMaterial(combineMeshes,copyMaterial);
                Console.WriteLine("Setting Material");
            }*/

            try
            {
                ulong maxVertexCountPerMerge = 200000;
                ulong currentVertexCount = 0; 
                for (int i = 0; i < children.list.Length;)
                {
                    OccurrenceList childrenToMerge = new OccurrenceList();
                    for (int j = i; j < children.list.Length;)
                    {
                        ulong vertexCount = pxz.Scene.GetVertexCount(new OccurrenceList(new uint[] { children.list[j] }));
                        if(currentVertexCount + vertexCount > maxVertexCountPerMerge)
                            break;
                    
                        childrenToMerge.AddOccurrence(children.list[j]);
                        currentVertexCount += vertexCount;
                        i++;
                        j = i;
                    }
                
                    Console.WriteLine("Merging meshes " + childrenToMerge.list.Length + " vertex count " + currentVertexCount);
                    uint combineMeshes = pxz.Algo.CombineMeshes(childrenToMerge, bakeOption);
                    pxz.Core.SetProperty(combineMeshes, "Name", "MERGED MESH " + i);
                
                    Console.WriteLine("End merging meshes ");
                    currentVertexCount = 0;
                
                    Console.WriteLine("Copying Material");
                    //Apply a copy of the material not to lose the reference
                    MaterialList material = pxz.Scene.GetMaterialsFromSubtree(combineMeshes);
                    uint copyMaterial = pxz.Material.CopyMaterial(material.list[0], false);
                    pxz.Core.SetProperty(copyMaterial, "Name", "MERGE MATERIAL " + i);
                    pxz.Scene.SetOccurrenceMaterial(combineMeshes,copyMaterial);
                    Console.WriteLine("Setting Material");
                }           
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
                 
            
            //pxz.Algo.CombineMeshes(new OccurrenceList(new uint[]{}), bakeOption);
            Console.WriteLine("END PXZ MERGE MESHES");
        }
    }
}