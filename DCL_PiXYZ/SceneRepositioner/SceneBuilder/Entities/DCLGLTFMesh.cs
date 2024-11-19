using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using DCL_PiXYZ;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using DCL_PiXYZ.Utils;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using UnityEngine.Pixyz.API;
using AlphaMode = SharpGLTF.Schema2.AlphaMode;

namespace AssetBundleConverter.LODs
{
    public class DCLGLTFMesh : DCLMesh
    {
        private SceneConversionPathHandler pathHandler;

        private readonly string src;

        public DCLGLTFMesh(string src)
        {
            this.src = src;
        }
        
        public override PXZModel InstantiateMesh(PiXYZAPI pxz, string entityID, uint parent, uint material, Dictionary<string, string> contentTable, SceneConversionPathHandler pathHandler, int lodLevel)
        {
            this.pathHandler = pathHandler;
            
            
            if (!contentTable.TryGetValue(src.ToLower(), out string modelPath))
            {
                LogError($"ERROR: GLTF {src} file not found in sceneContent");
                return PXZConstants.EMPTY_MODEL;
            }

            bool modelRecreatedSuccessfully = true;
            
            try
            {
                //TODO (Juani) : This is confusing. If you modify the material and it doesnt fail; but it fails on export of modified model, you wont know it
                ModifyModelMaterials(modelPath);
                ExportModifiedModel(modelPath, lodLevel, pathHandler.sceneBasePointer.Contains("-66,-56"));
            }
            catch (Exception e)
            {
                LogError($"ERROR pre-processing GLTF with GLTFSharp for file {src}: {e}");
                modelRecreatedSuccessfully = false;
            }

            try
            {
                uint importedFileOccurrence = pxz.IO.ImportScene(modelRecreatedSuccessfully ? modelPath + "_EDITED.glb" : modelPath);
                pxz.Scene.SetParent(importedFileOccurrence, parent);
                return new PXZModel(true, importedFileOccurrence);
            }
            catch (Exception e)
            {
                FileWriter.WriteToConsole($"ERROR: Importing GLTF {src} failed with error {e}");
                return PXZConstants.EMPTY_MODEL;
            }
        }

        private void ModifyModelMaterials(string modelPath)
        {
            var model = ModelRoot.Load(modelPath, new ReadSettings(ValidationMode.Skip));
            foreach (var gltfMaterial in model.LogicalMaterials)
            {
                if (gltfMaterial.Alpha != AlphaMode.OPAQUE && !gltfMaterial.Name.Contains(PXZConstants.FORCED_TRANSPARENT_MATERIAL))
                    gltfMaterial.Name += PXZConstants.FORCED_TRANSPARENT_MATERIAL;
            }

            SaveModel(model, modelPath);
        }
        
        private void SaveModel(ModelRoot model, string modelPath)
        {
            if (Path.GetExtension(modelPath).Contains("glb", StringComparison.OrdinalIgnoreCase))
                model.SaveGLB(modelPath, new WriteSettings(ValidationMode.Skip));
            else
                model.SaveGLTF(modelPath, new WriteSettings(ValidationMode.Skip));
        }
        
        private void ExportModifiedModel(string modelPath, int lodLevel, bool forceIgnoreSMR = false)
        {
            var model = ModelRoot.Load(modelPath, new ReadSettings(ValidationMode.Skip));
            var modelRoot = ModelRoot.CreateModel();
            var sceneModel = modelRoot.UseScene("Default");
            foreach (var modelLogicalNode in model.LogicalNodes)
            {
                //TODO (JUANI): If we are in LOD_0, we want to keep the original file and structure to avoid possible transformation issues.
                //If we arent, we dont want to throw a SMR to the merge since PiXYZ offers inconsistent results
                if (modelLogicalNode.Skin != null)
                {
                    if (lodLevel == 0 && !forceIgnoreSMR)
                        throw new Exception("Model has a SMR");
                    else
                        continue;
                }
                
                if (modelLogicalNode.Mesh != null)
                {
                    foreach (var meshPrimitive in modelLogicalNode.Mesh.Primitives)
                    {
                        var meshToExport = modelRoot.CreateMesh(modelLogicalNode.Name);
                        BuildMesh(meshToExport.CreatePrimitive(), meshPrimitive.GetVertexAccessor("POSITION"),
                            meshPrimitive.GetVertexAccessor("NORMAL"),
                            meshPrimitive.GetVertexAccessor("TEXCOORD_0"),
                            (int[])(object)meshPrimitive.GetIndexAccessor().AsIndicesArray().ToArray(),
                            modelRoot.CreateMaterial(meshPrimitive.Material.ToMaterialBuilder()));
                        var node = sceneModel.CreateNode(modelLogicalNode.Name).WithMesh(meshToExport);
                        node.WorldMatrix = modelLogicalNode.WorldMatrix;
                    }
                }
            }

            modelRoot.SaveGLB(modelPath + "_EDITED.glb", new WriteSettings(ValidationMode.Skip));
        }
        
        private void BuildMesh(MeshPrimitive meshToExport, Accessor positions, Accessor normals, Accessor texcoord, int[] indices, Material material)
        {
            if (positions != null)
                meshToExport.WithVertexAccessor("POSITION", positions.AsVector3Array().ToArray());

            if (normals != null)
                meshToExport.WithVertexAccessor("NORMAL", normals.AsVector3Array().ToArray());

            if (texcoord != null)
                meshToExport.WithVertexAccessor("TEXCOORD_0", texcoord.AsVector2Array().ToArray());

            meshToExport.WithIndicesAccessor(PrimitiveType.TRIANGLES, indices);
            meshToExport.WithMaterial(material);
        }
        
        private void LogError(string message)
        {
            FileWriter.WriteToFile(message, pathHandler.FailGLBImporterFile);
        }
        

    }
        
    
}
