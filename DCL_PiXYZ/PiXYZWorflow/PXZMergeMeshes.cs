using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private readonly int lodLevel;

        public PXZMergeMeshes(int lodLevel)
        {
            opaquesToMerge = new OccurrenceList();
            opaquesToMerge.list = new uint[]{};
            transparentsToMerge = new OccurrenceList();
            transparentsToMerge.list = new uint[]{};
            maxVertexCountPerMerge = 200000;
            
            bakeOption = new BakeOption();
            BakeMaps bakeMaps = new BakeMaps();
            bakeMaps.diffuse = true;
            bakeOption.bakingMethod = BakingMethod.RayOnly;
            this.lodLevel = lodLevel;
            bakeOption.padding = 1;
            bakeOption.textures = bakeMaps;
        }
        
        
        public async Task ApplyModification(PiXYZAPI pxz)
        {
            this.pxz = pxz;
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
        }

        private void MergeSubMeshes(OccurrenceList listToMerge, bool isOpaque)
        {
            OccurrenceList toMerge = new OccurrenceList();
            toMerge.list = new uint[]{};
            ulong currentVertexCount = 0;

            int mergedMesh = 0;
            for (int i = 0; i < listToMerge.list.Length; i++)
            {
                ulong vertexCount = pxz.Scene.GetVertexCount(new OccurrenceList(new uint[] { listToMerge.list[i] }));
                ulong currentCandidate = currentVertexCount + vertexCount;

                if (currentCandidate > maxVertexCountPerMerge)
                {
                    DoMerge(currentVertexCount, toMerge, isOpaque, mergedMesh);
                    toMerge = new OccurrenceList();
                    currentVertexCount = 0;
                    mergedMesh++;
                }
                
                toMerge.AddOccurrence(listToMerge.list[i]);
                currentVertexCount += vertexCount;
            }

            mergedMesh++;
            // Merge any remaining submeshes if needed
            if (toMerge.list.Length > 0)
                DoMerge(currentVertexCount, toMerge, isOpaque, mergedMesh);
        }

        private void DoMerge(ulong currentVertexCount, OccurrenceList toMerge, bool isOpaque, int index)
        {
            if (toMerge.list.Length == 0)
                return;

            //TODO: What would be the best option here?
            bakeOption.resolution = 1024;
            if (lodLevel == 1 && currentVertexCount < 150000)
                bakeOption.resolution = 512;
            else if (lodLevel == 2 && currentVertexCount < 150000)
                bakeOption.resolution = 256;
            
            Console.WriteLine($"Merging meshes {(isOpaque ? "OPAQUE" : "TRANSPARENT")} {toMerge.list.Length} vertex count {currentVertexCount}");

            
            uint combineMeshes = pxz.Algo.CombineMeshes(toMerge, bakeOption);
            pxz.Core.SetProperty(combineMeshes, "Name", $"MERGED MESH {index} {(isOpaque ? "OPAQUE" : "FORCED_TRANSPARENT")}");

            Console.WriteLine("Copying Material");
            //Apply a copy of the material not to lose the reference
            MaterialList material = pxz.Scene.GetMaterialsFromSubtree(combineMeshes);

            if (material.list?.Length > 0)
            {
                uint copyMaterial = pxz.Material.CopyMaterial(material.list[0], false);
                pxz.Core.SetProperty(copyMaterial, "Name", $"MERGE MATERIAL {index} {(isOpaque ? "OPAQUE" : "FORCED_TRANSPARENT")}");
                pxz.Scene.SetOccurrenceMaterial(combineMeshes,copyMaterial);
                Console.WriteLine("Setting Material");
            }

        }


        private void AddOcurrences(PiXYZAPI pxz, PackedTree packedTree)
        {
            if (packedTree.occurrences.list != null)
            {
                for (var i = 0; i < packedTree.occurrences.list.Length; i++)
                {
                    //Means it has a mesh component
                    uint packedTreeOccurrence = packedTree.occurrences[i];
                    if (pxz.Scene.HasComponent(packedTreeOccurrence, ComponentType.Part) && !packedTree.names[i].Contains("collider"))
                    {
                        MaterialList material = pxz.Scene.GetMaterialsFromSubtree(packedTreeOccurrence);
                        //A material will be consider transparent only if it has a single material and its name contains "FORCED_TRANSPARENT" added during the material curation
                        bool isTransparent = material.list.Length == 1 && pxz.Core.GetProperty(material.list[0], "Name").Contains("FORCED_TRANSPARENT");
                        if (isTransparent)
                            transparentsToMerge.AddOccurrence(packedTreeOccurrence);
                        else
                            opaquesToMerge.AddOccurrence(packedTreeOccurrence);
                    }
                }
            }
            
        }
    }
}