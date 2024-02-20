using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using DCL_PiXYZ;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
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

        private string src;

        public DCLGLTFMesh(string src)
        {
            this.src = src;
        }

        public override PXZModel InstantiateMesh(PiXYZAPI pxz, string entityID , uint parent, uint material, Dictionary<string, string> sceneContent)
        {
            if (!sceneContent.TryGetValue(entityID.ToLower(), out string modelPath))
            {
                Console.WriteLine($"ERROR: GLTF {entityID} file not found in sceneContent");
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
                            var positions = meshPrimitive.GetVertexAccessor("POSITION").AsVector3Array().ToArray();
                            var normals = meshPrimitive.GetVertexAccessor("NORMAL").AsVector3Array().ToArray();
                            var indices = (int[])(object)meshPrimitive.GetIndexAccessor().AsIndicesArray().ToArray();
                            //TODO: Fix this if
                            if (meshPrimitive.GetVertexAccessor("TEXCOORD_0") != null)
                            {
                                var texcoord = meshPrimitive.GetVertexAccessor("TEXCOORD_0").AsVector2Array().ToArray();
                                meshToExport.CreatePrimitive()
                                    .WithVertexAccessor("POSITION", positions)
                                    .WithVertexAccessor("NORMAL", normals)
                                    .WithVertexAccessor("TEXCOORD_0", texcoord)
                                    .WithIndicesAccessor(PrimitiveType.TRIANGLES, indices)
                                    .WithMaterial(modelRoot.CreateMaterial(meshPrimitive.Material.ToMaterialBuilder()));
                            }
                            else
                            {
                                meshToExport.CreatePrimitive()
                                    .WithVertexAccessor("POSITION", positions)
                                    .WithVertexAccessor("NORMAL", normals)
                                    .WithIndicesAccessor(PrimitiveType.TRIANGLES, indices)
                                    .WithMaterial(modelRoot.CreateMaterial(meshPrimitive.Material.ToMaterialBuilder()));
                            }
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
        
        private void LogError(string message)
        {
            PXZEntryPoint.WriteToFile(message, "FAILEDIMPORTMODELS.txt");
            Console.WriteLine(message);
        }
        
    }
}
