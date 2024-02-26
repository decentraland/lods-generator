using System.Collections.Generic;
using System.IO;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using DCL_PiXYZ.SceneRepositioner.SceneBuilder.PrimitiveFactory;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Geom;
using Vector3 = System.Numerics.Vector3;


namespace DCL_PiXYZ.SceneRepositioner.SceneBuilder.Entities
{
    
    public abstract class DCLPrimitiveMesh : DCLMesh
    {
        protected abstract uint GetMesh(PiXYZAPI pxz, string entityID);

        public override PXZModel InstantiateMesh(PiXYZAPI pxz, string entityID, uint parent, uint material, Dictionary<string, string> sceneContent, SceneConversionPathHandler pathHandler)
        {
            uint mesh = GetMesh(pxz, entityID);
            Matrix4 matrix4 = new Matrix4();
            matrix4.Init();
            //TODO: Check this one. Mirroring on x axis to transform from PiXYZ space to Unity Space
            matrix4.Scale(new Vector3(-100, 100, 100));
            pxz.Scene.ApplyTransformation(mesh, matrix4);
            
            pxz.Scene.SetParent(mesh, parent);
            pxz.Scene.SetOccurrenceMaterial(mesh,material);

            return new PXZModel(false, mesh);
        }
    }

    public class Box : DCLPrimitiveMesh
    {
        public float[] uvs;

        protected override uint GetMesh(PiXYZAPI pxz, string entityID)
        {
            string boxCreated = BoxFactory.Create(entityID, uvs);
            return pxz.IO.ImportScene(Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, boxCreated));
        }

    }

    public class Cylinder : DCLPrimitiveMesh
    {
        public int radiusTop;
        public int radiusBottom;

        protected override uint GetMesh(PiXYZAPI pxz, string entityID)
        {
            string cylinderCreated = CylinderVariantsFactory.Create(entityID, radiusTop, radiusBottom);
            return pxz.IO.ImportScene(Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, cylinderCreated));
        }
    }

    public class Plane : DCLPrimitiveMesh
    {
        public float[] uvs;

        protected override uint GetMesh(PiXYZAPI pxz, string entityID)
        {
            string planeCreated = PlaneFactory.Create(entityID, uvs);
            return pxz.IO.ImportScene(Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, planeCreated));
        }
    }

    public class Sphere : DCLPrimitiveMesh
    {
        protected override uint GetMesh(PiXYZAPI pxz, string entityID)
        {
            string sphereCreated = SphereFactory.Create(entityID);
            return pxz.IO.ImportScene(Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, sphereCreated));
        }
    }
    
}
