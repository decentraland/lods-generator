using System.Collections.Generic;
using System.Numerics;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using DCL_PiXYZ.SceneRepositioner.SceneBuilder.PrimitiveFactory;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Core;

namespace DCL_PiXYZ.SceneRepositioner.SceneBuilder.Entities
{
    
    public abstract class DCLPrimitiveMesh : DCLMesh
    {
        protected abstract uint GetMesh(PiXYZAPI pxz);
        public override void InstantiateMesh(PiXYZAPI pxz, uint parent, uint material, Dictionary<string, string> sceneContent)
        {
            uint mesh = GetMesh(pxz);
            pxz.Scene.SetParent(mesh, parent);
            pxz.Scene.SetOccurrenceMaterial(mesh,material);

        }
    }

    public class Box : DCLPrimitiveMesh
    {
        public float[] uvs;

        protected override uint GetMesh(PiXYZAPI pxz)
        {
            return pxz.Scene.CreateCube(PrimitivesSize.CUBE_SIZE, PrimitivesSize.CUBE_SIZE, PrimitivesSize.CUBE_SIZE);
        }

    }

    public class Cylinder : DCLPrimitiveMesh
    {
        public int radiusTop;
        public int radiusBottom;

        protected override uint GetMesh(PiXYZAPI pxz)
        {
            if (radiusTop == 0)
                return pxz.Scene.CreateCone(radiusBottom, PrimitivesSize.CYLINDER_HEIGHT);
            else
                return pxz.Scene.CreateCylinder(radiusTop, PrimitivesSize.CYLINDER_HEIGHT);
        }
    }

    public class Plane : DCLPrimitiveMesh
    {
        public float[] uvs;

        protected override uint GetMesh(PiXYZAPI pxz) =>
            pxz.Scene.CreatePlane(PrimitivesSize.PLANE_SIZE.X, PrimitivesSize.PLANE_SIZE.Y);
    }

    public class Sphere : DCLPrimitiveMesh
    {
        protected override uint GetMesh(PiXYZAPI pxz) =>
            pxz.Scene.CreateSphere(PrimitivesSize.SPHERE_RADIUS);
    }
    
}
