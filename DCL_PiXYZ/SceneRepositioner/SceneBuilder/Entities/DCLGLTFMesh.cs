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
        private SceneConversionDebugInfo debugInfo;

        private string src;

        public DCLGLTFMesh(string src)
        {
            this.src = src;
        }

        public override PXZModel InstantiateMesh(PiXYZAPI pxz, string entityID , uint parent, uint material, Dictionary<string, string> sceneContent, SceneConversionDebugInfo debugInfo)
        {
            this.debugInfo = debugInfo;
            
            if (!sceneContent.TryGetValue(src.ToLower(), out string modelPath))
            {
                Console.WriteLine($"GLTF {entityID} file not found in sceneContent");
                return PXYZConstants.EMPTY_MODEL;
            }
            
            bool isModelProcessingSuccessful = TryProcessModel(modelPath);

            if (!TryImportGLTF(pxz, parent, modelPath, isModelProcessingSuccessful, out PXZModel pxzModel))
            {
                Console.WriteLine($"ERROR: Importing GLTF {entityID} failed");
                return PXYZConstants.EMPTY_MODEL;
            }

            return pxzModel;
        }
        
        private bool TryProcessModel(string modelPath)
        {
            try
            {
                ModifyModelMaterials(modelPath);
                ExportModifiedModel(modelPath);
                return true;
            }
            catch (Exception e)
            {
                LogError($"MODEL PROCESSING FAILED {modelPath} {e}");
                return false;
            }
        }
        
        private void ModifyModelMaterials(string modelPath)
        {
            var readSettings = new ReadSettings(ValidationMode.TryFix);
            var model = ModelRoot.Load(modelPath, readSettings);
            foreach (var gltfMaterial in model.LogicalMaterials)
            {
                if (gltfMaterial.Alpha != AlphaMode.OPAQUE && !gltfMaterial.Name.Contains("FORCED_TRANSPARENT"))
                    gltfMaterial.Name += "FORCED_TRANSPARENT";
            }
            SaveModel(model, modelPath);
        }

        private void SaveModel(ModelRoot model, string modelPath)
        {
            if (Path.GetExtension(modelPath).Contains("glb", StringComparison.OrdinalIgnoreCase))
                model.SaveGLB(modelPath);
            else
                model.SaveGLTF(modelPath);
        }

        private bool TryImportGLTF(PiXYZAPI pxz, uint parent, string modelPath, bool isModelProcessed, out PXZModel pxzModel)
        {
            try
            {
                string finalModelPath = isModelProcessed ? $"{modelPath}_EDITED.glb" : modelPath;

                uint importedFileOccurrence = pxz.IO.ImportScene(finalModelPath);
                pxz.Scene.SetParent(importedFileOccurrence, parent);
                pxzModel = new PXZModel(true, importedFileOccurrence);
                return true;
            }
            catch (Exception e)
            {
                LogError($"ERROR importing GLTF: {e}");
                pxzModel = PXYZConstants.EMPTY_MODEL;
                return false;
            }
        }
        
        private void ExportModifiedModel(string modelPath)
        {
            // Determine the output file path based on the original model path
            string outputFile = $"{modelPath}_EDITED.glb";

            // Check if the file already exists
            if (File.Exists(outputFile))
            {
                Console.WriteLine($"The file {outputFile} already exists. Skipping export.");
                return; 
            }

            try
            {
                ReadSettings readSettings = new ReadSettings(ValidationMode.TryFix);
                var model = ModelRoot.Load(modelPath, readSettings);
                
                ModelRoot modelRoot = ModelRoot.CreateModel();
                Scene sceneModel = modelRoot.UseScene("Default");
                foreach (var modelLogicalNode in model.LogicalNodes)
                {
                    if (modelLogicalNode.Mesh != null)
                    {
                        foreach(MeshPrimitive meshPrimitive in modelLogicalNode.Mesh.Primitives)
                        {
                            Mesh meshToExport = modelRoot.CreateMesh(modelLogicalNode.Name);

                            BuildMesh(meshToExport.CreatePrimitive(), meshPrimitive.GetVertexAccessor("POSITION"), 
                                meshPrimitive.GetVertexAccessor("NORMAL"), 
                                meshPrimitive.GetVertexAccessor("TEXCOORD_0"),
                                (int[])(object)meshPrimitive.GetIndexAccessor().AsIndicesArray().ToArray(), 
                                modelRoot.CreateMaterial(meshPrimitive.Material.ToMaterialBuilder()));
                         
                            Node node = sceneModel.CreateNode(modelLogicalNode.Name).WithMesh(meshToExport);
                            node.WorldMatrix = modelLogicalNode.WorldMatrix;
                        }
                    }
                }
                SaveModel(model, modelPath + "_EDITED.glb");
            }
            catch (Exception e)
            {
                LogError($"Failed to export modified model {modelPath} due to error: {e}");
            }
        }

        private void BuildMesh(MeshPrimitive meshToExport, Accessor positions, Accessor normals, Accessor texcoord, int[] indices, Material material)
        {
            if (positions != null)
                meshToExport.WithVertexAccessor("POSITION", positions.AsVector3Array().ToArray());
            
            if (normals != null)
                meshToExport.WithVertexAccessor("NORMAL", normals.AsVector3Array().ToArray());
            
            if(texcoord != null)
                meshToExport.WithVertexAccessor("TEXCOORD_0", texcoord.AsVector2Array().ToArray());

            meshToExport.WithIndicesAccessor(PrimitiveType.TRIANGLES, indices);
            meshToExport.WithMaterial(material);
        }

        private void LogError(string message)
        {
            FileWriter.WriteToFile(message, debugInfo.FailGLBImporterFile);
        }
        
    }
}
