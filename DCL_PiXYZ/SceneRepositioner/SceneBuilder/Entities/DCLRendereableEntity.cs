using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public PXZModel PositionAndInstantiteMesh(Dictionary<string, string> contentTable, Dictionary<int, DCLRendereableEntity> renderableEntities)
        {
            InstantiateTransform(renderableEntities);
            return rendereableMesh.InstantiateMesh(pxz, entityID.ToString(), instantiatedEntity, dclMaterial.GetMaterial(pxz, entityID.ToString(),contentTable) ,contentTable);
        }

        private void InstantiateTransform(Dictionary<int, DCLRendereableEntity> renderableEntities)
        {
            if (transform.parent != 0)
                if (renderableEntities.TryGetValue((int)transform.parent, out DCLRendereableEntity rendereableEntity))
                    pxz.Scene.SetParent(instantiatedEntity, rendereableEntity.instantiatedEntity);

            Matrix4 matrix4 = new Matrix4();
            matrix4.Init();
            matrix4.Rotate(new Quaternion(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w));
            matrix4.Scale(new Vector3(transform.scale.x, transform.scale.y, transform.scale.z));
            matrix4.Translate(new Vector3(transform.position.x, transform.position.y, transform.position.z));
            pxz.Scene.ApplyTransformation(instantiatedEntity, matrix4);
        }

    }
}


