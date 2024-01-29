using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using UnityEngine.Pixyz.Algo;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Material;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZMergeMeshes : IPXZModifier
    {
        
        private OccurrenceList opaquesToMerge; 
        private OccurrenceList transparentsToMerge;
        private PiXYZAPI pxz;
        private ulong maxVertexCountPerMerge;
        private BakeOption bakeOption;


        public PXZMergeMeshes()
        {
            opaquesToMerge = new OccurrenceList();
            transparentsToMerge = new OccurrenceList();
            maxVertexCountPerMerge = 200000;
            
            bakeOption = new BakeOption();
            BakeMaps bakeMaps = new BakeMaps();
            bakeMaps.diffuse = true;
            bakeMaps.opacity = true;
            bakeOption.bakingMethod = BakingMethod.RayOnly;
            bakeOption.resolution = 1024;
            bakeOption.padding = 1;
            bakeOption.textures = bakeMaps;
        }
        
        
        public async Task ApplyModification(PiXYZAPI pxz)
        {
            this.pxz = pxz;
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN PXZ MERGE MESHES");
            try
            {
                AddOcurrences(pxz, pxz.Scene.GetSubTree(pxz.Scene.GetRoot()));
                MergeSubMeshes(opaquesToMerge, true);
                MergeSubMeshes(transparentsToMerge, false);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            Console.WriteLine("END PXZ MERGE MESHES");
            Console.WriteLine("-------------------------");
        }

        private void MergeSubMeshes(OccurrenceList listToMerge, bool isOpaque)
        {
            OccurrenceList toMerge = new OccurrenceList();
            ulong currentVertexCount = 0;

            for (int i = 0; i < listToMerge.list.Length; i++)
            {
                ulong vertexCount = pxz.Scene.GetVertexCount(new OccurrenceList(new uint[] { listToMerge.list[i] }));
                ulong currentCandidate = currentVertexCount + vertexCount;

                if (currentCandidate > maxVertexCountPerMerge)
                {
                    DoMerge(currentVertexCount, toMerge, isOpaque);
                    toMerge = new OccurrenceList();
                    currentVertexCount = 0;
                }
                
                toMerge.AddOccurrence(listToMerge.list[i]);
                currentVertexCount += vertexCount;
            }

            // Merge any remaining submeshes if needed
            if (toMerge.list.Length > 0)
            {
                DoMerge(currentVertexCount, toMerge, isOpaque);
            }
        }

        private void DoMerge(ulong currentVertexCount, OccurrenceList toMerge, bool isOpaque)
        {
            Console.WriteLine("Merging meshes " + toMerge.list.Length + " vertex count " + currentVertexCount);
            uint combineMeshes = pxz.Algo.CombineMeshes(toMerge, bakeOption);
            pxz.Core.SetProperty(combineMeshes, "Name", $"MERGED MESH {(isOpaque ? "OPAQUE" : "TRANSPARENT")}");
            Console.WriteLine("End merging meshes ");

            Console.WriteLine("Copying Material");
            //Apply a copy of the material not to lose the reference
            MaterialList material = pxz.Scene.GetMaterialsFromSubtree(combineMeshes);
            uint copyMaterial = pxz.Material.CopyMaterial(material.list[0], false);
            pxz.Core.SetProperty(copyMaterial, "Name", $"MERGE MATERIAL {(isOpaque ? "OPAQUE" : "TRANSPARENT")}");
            pxz.Scene.SetOccurrenceMaterial(combineMeshes,copyMaterial);
            Console.WriteLine("Setting Material");
        }


        private void AddOcurrences(PiXYZAPI pxz, PackedTree packedTree)
        {
            if (packedTree.occurrences.list != null)
            {
                for (var i = 0; i < packedTree.occurrences.list.Length; i++)
                {
                    //Means it has a mesh component
                    if (pxz.Scene.HasComponent(packedTree.occurrences[i], ComponentType.Part))
                    {
                        MaterialList material = pxz.Scene.GetMaterialsFromSubtree(packedTree.occurrences[i]);
                        bool isOpaque = true;
                        for (var j = 0; j < material.list.Length; j++)
                            isOpaque = isOpaque && pxz.Material.IsOpaque(material.list[j]);
                        if (isOpaque)
                            opaquesToMerge.AddOccurrence(packedTree.occurrences[i]);
                        else
                            transparentsToMerge.AddOccurrence(packedTree.occurrences[i]);
                    }
                }
            }
            
        }
    }
}