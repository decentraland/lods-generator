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
        public override PXZModel InstantiateMesh(PiXYZAPI pxz, string entityID,uint parent, uint material, Dictionary<string, string> sceneContent)
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
            //return pxz.Scene.CreateCube(PrimitivesSize.CUBE_SIZE, PrimitivesSize.CUBE_SIZE, PrimitivesSize.CUBE_SIZE);
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
            /*if (radiusTop == 0)
                return pxz.Scene.CreateCone(radiusBottom, PrimitivesSize.CYLINDER_HEIGHT);
            else
                return pxz.Scene.CreateCylinder(radiusTop, PrimitivesSize.CYLINDER_HEIGHT);*/
        }
    }

    public class Plane : DCLPrimitiveMesh
    {
        public float[] uvs;

        protected override uint GetMesh(PiXYZAPI pxz, string entityID)
        {
            string planeCreated = PlaneFactory.Create(entityID, uvs);
            return pxz.IO.ImportScene(Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, planeCreated));
            //pxz.Scene.CreatePlane(PrimitivesSize.PLANE_SIZE.X, PrimitivesSize.PLANE_SIZE.Y);
        }
    }

    public class Sphere : DCLPrimitiveMesh
    {
        protected override uint GetMesh(PiXYZAPI pxz, string entityID)
        {
            string sphereCreated = SphereFactory.Create(entityID);
            return pxz.IO.ImportScene(Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, sphereCreated));
            //pxz.Scene.CreateSphere(PrimitivesSize.SPHERE_RADIUS);
        }
    }
    
}
