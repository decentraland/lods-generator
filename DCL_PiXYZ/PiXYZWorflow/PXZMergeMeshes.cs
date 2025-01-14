using System;
using DCL_PiXYZ.Utils;
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
            maxVertexCountPerMerge = 200_000;
            
            bakeOption = new BakeOption();
            BakeMaps bakeMaps = new BakeMaps();
            bakeMaps.diffuse = true;
            bakeOption.resolution = 1024;
            this.lodLevel = lodLevel;
            bakeOption.padding = 1;
            bakeOption.textures = bakeMaps;
        }
        
        
        public void ApplyModification(PiXYZAPI pxz)
        {
            this.pxz = pxz;
            try
            {
                AddOcurrences();
                MergeSubMeshes(opaquesToMerge, true);
                MergeSubMeshes(transparentsToMerge, false);
            }
            catch(Exception e)
            {
                FileWriter.WriteToConsole(e.Message);
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

            FileWriter.WriteToConsole($"Merging meshes {(isOpaque ? "OPAQUE" : "TRANSPARENT")} {toMerge.list.Length} vertex count {currentVertexCount}");

            uint combineMeshes = pxz.Scene.MergePartOccurrences(toMerge)[0];
            pxz.Core.SetProperty(combineMeshes, "Name", $"MERGED MESH {index} {(isOpaque ? "OPAQUE" : PXZConstants.FORCED_TRANSPARENT_MATERIAL)}");
            pxz.Algo.CombineMaterials(toMerge, bakeOption);

            FileWriter.WriteToConsole("Copying Material");
            //Apply a copy of the material not to lose the reference
            MaterialList material = pxz.Scene.GetMaterialsFromSubtree(combineMeshes);

            if (material.list?.Length > 0)
            {
                uint copyMaterial = pxz.Material.CopyMaterial(material.list[0], false);
                pxz.Core.SetProperty(copyMaterial, "Name", $"MERGE MATERIAL {index} {(isOpaque ? "OPAQUE" : PXZConstants.FORCED_TRANSPARENT_MATERIAL)}");
                pxz.Scene.SetOccurrenceMaterial(combineMeshes,copyMaterial);
                FileWriter.WriteToConsole("Setting Material");
            }
        }


        private void AddOcurrences()
        {
            PackedTree packedTree = pxz.Scene.GetSubTree(pxz.Scene.GetRoot());
            OccurrenceList occurrenceListToDelete = new OccurrenceList();
            if (packedTree.occurrences.list != null)
            {
                for (var i = 0; i < packedTree.occurrences.list.Length; i++)
                {
                    uint packedTreeOccurrence = packedTree.occurrences[i];
                    //Means it has a mesh component
                    if (pxz.Scene.HasComponent(packedTreeOccurrence, ComponentType.Part) 
                         && !IsAnimated(packedTreeOccurrence)
                         && !packedTree.names[i].Contains("collider"))
                    {
                        MaterialList material = pxz.Scene.GetMaterialsFromSubtree(packedTreeOccurrence);
                        //A material will be consider transparent only if it has a single material and its name contains "FORCED_TRANSPARENT" added during the material curation
                        bool isTransparent = material.list.Length == 1 && pxz.Core.GetProperty(material.list[0], "Name").Contains(PXZConstants.FORCED_TRANSPARENT_MATERIAL);
                        if (isTransparent)
                            transparentsToMerge.AddOccurrence(packedTreeOccurrence);
                        else
                            opaquesToMerge.AddOccurrence(packedTreeOccurrence);
                    }
                    
                    if (pxz.Scene.HasComponent(packedTreeOccurrence, ComponentType.Part)
                        && IsAnimated(packedTreeOccurrence))
                    {
                        occurrenceListToDelete.AddOccurrence(packedTreeOccurrence);
                    }
                }
            }
            pxz.Scene.DeleteOccurrences(occurrenceListToDelete);
        }

        private bool IsAnimated(uint parentOccurent)
        {
            PackedTree packedTree = pxz.Scene.GetSubTree(parentOccurent);
            for (var i = 0; i < packedTree.occurrences.list.Length; i++)
            {
                uint packedTreeOccurrence = packedTree.occurrences[i];
                if (pxz.Scene.HasComponent(packedTreeOccurrence, ComponentType.Animation))
                    return true;
            }
            return false;
        }
    }
}