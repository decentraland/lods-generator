using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Geom;
using Vector3 = System.Numerics.Vector3;

namespace DCL_PiXYZ.SceneRepositioner.SceneBuilder.Entities
{
    public class DCLRendereableEntity
    {

        private int entityID;
        private TransformData transform = new TransformData();
        private DCLMesh rendereableMesh;
        private DCLMaterial dclMaterial = new EmptyMaterial();
        private PiXYZAPI pxz;


        private uint instantiatedEntity;

        public void SetComponentData(RenderableEntity renderableEntity)
        {
            entityID = renderableEntity.entityId;

            switch (renderableEntity.componentName)
            {
                case RenderableEntityConstants.Transform:
                    transform = (TransformData)renderableEntity.data;
                    break;
                case RenderableEntityConstants.MeshRenderer:
                    rendereableMesh = ((MeshRendererData)renderableEntity.data).mesh;
                    break;
                case RenderableEntityConstants.GLTFContainer:
                    rendereableMesh = ((GLTFContainerData)renderableEntity.data).mesh;
                    break;
                case RenderableEntityConstants.Material:
                    dclMaterial = ((MaterialData)renderableEntity.data).material;
                    break;
            }
        }

        public void InitEntity(PiXYZAPI pxz, uint pxzRootOcurrence)
        {
            instantiatedEntity = pxz.Scene.CreateOccurrence($"Entity_{entityID}", pxzRootOcurrence);
            this.pxz = pxz;
        }

        public PXZModel PositionAndInstantiteMesh(Dictionary<string, string> contentTable, Dictionary<int, DCLRendereableEntity> renderableEntities, SceneConversionPathHandler pathHandler)
        {
            InstantiateTransform(renderableEntities);
            if (rendereableMesh != null)
            {
                uint material = dclMaterial.GetMaterial(pxz, entityID.ToString(), contentTable);
                return rendereableMesh.InstantiateMesh(pxz, entityID.ToString(), instantiatedEntity, material, contentTable, pathHandler);
            }
            else
                return PXYZConstants.EMPTY_MODEL;
        }

        private void InstantiateTransform(Dictionary<int, DCLRendereableEntity> renderableEntities)
        {
            if (transform.parent != 0)
                if (renderableEntities.TryGetValue((int)transform.parent, out DCLRendereableEntity rendereableEntity))
                    pxz.Scene.SetParent(instantiatedEntity, rendereableEntity.instantiatedEntity);

            Matrix4 matrix4 = new Matrix4();
            matrix4.Init();
            matrix4.Scale(new Vector3(transform.scale.x, transform.scale.y, transform.scale.z));
            Quaternion quaternion = ValidateOrNormalize(new Quaternion(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w));
            matrix4.Rotate(quaternion);
            matrix4.Translate(new Vector3(transform.position.x, transform.position.y, transform.position.z));
            pxz.Scene.ApplyTransformation(instantiatedEntity, matrix4);
        }
        
        public static Quaternion ValidateOrNormalize(Quaternion q)
        {
            double magnitudeSquared = q.W * q.W + q.X * q.X + q.Y * q.Y + q.Z * q.Z;
        
            // Considering a small tolerance for floating point arithmetic errors
            const double tolerance = 1E-4;
            if (Math.Abs(magnitudeSquared - 1.0) <= tolerance)
            {
                // It's a valid unit quaternion
                return q;
            }

            return Quaternion.Normalize(q);
        }

    }
}


