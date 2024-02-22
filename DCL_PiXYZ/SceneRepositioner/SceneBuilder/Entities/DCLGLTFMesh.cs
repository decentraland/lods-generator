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
        private SceneConversionPathHandler _pathHandler;

        private string src;

        public DCLGLTFMesh(string src)
        {
            this.src = src;
        }

        public override PXZModel InstantiateMesh(PiXYZAPI pxz, string entityID, uint parent, uint material, Dictionary<string, string> contentTable, SceneConversionPathHandler pathHandler, int lodLevel)
        {
            if (!contentTable.TryGetValue(src.ToLower(), out string modelPath))
            {
                Console.WriteLine($"ERROR: GLTF {src} file not found in sceneContent");
                return PXYZConstants.EMPTY_MODEL;
            }

            bool modelWorkSuccessfull = true;
            try
            {
                var readSettings = new ReadSettings(ValidationMode.TryFix);
                var model = ModelRoot.Load(modelPath, readSettings);
                foreach (var gltfMaterial in model.LogicalMaterials)
                {
                    if (gltfMaterial.Alpha != AlphaMode.OPAQUE && !gltfMaterial.Name.Contains("FORCED_TRANSPARENT"))
                        gltfMaterial.Name += "FORCED_TRANSPARENT";
                }

                if (Path.GetExtension(modelPath).Contains("glb", StringComparison.OrdinalIgnoreCase))
                    model.SaveGLB(modelPath);
                else
                    model.SaveGLTF(modelPath);
            }
            catch (Exception e)
            {
                //PXZEntryPoint.WriteToFile($"MODEL FAILED A {modelPath} {e}", "FAILEDIMPORTMODELS.txt");
                Console.WriteLine($"ERROR pre-processing GLTF material: {e}");
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
                    modelRoot.SaveGLB(modelPath + "_EDITED.glb");
                }
                catch (Exception e)
                {
                    //PXZEntryPoint.WriteToFile($"MODEL FAILED B {modelPath} {e}", "FAILEDIMPORTMODELS.txt");
                    modelWorkSuccessfull =  false;
                    Console.WriteLine($"ERROR pre-processing GLTF: {e}");
                }
                try
                {
                    if (lodLevel != 0)
                    {
                        uint importedFileOccurrence = pxz.IO.ImportScene(modelPath);
                        pxz.Scene.SetParent(importedFileOccurrence, parent);
                        return new PXZModel(true, importedFileOccurrence);
                    }
                    else
                    {
                        uint importedFileOccurrence = pxz.IO.ImportScene(modelWorkSuccessfull ? modelPath + "_EDITED.glb" : modelPath);
                        pxz.Scene.SetParent(importedFileOccurrence, parent);
                        return new PXZModel(true, importedFileOccurrence);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine($"ERROR: Importing GLTF {src} failed with error {e}");
                    return PXYZConstants.EMPTY_MODEL;
                }

        }

        

    }
        
    
}
