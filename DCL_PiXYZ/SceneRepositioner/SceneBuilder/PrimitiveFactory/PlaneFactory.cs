using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace DCL_PiXYZ.SceneRepositioner.SceneBuilder.PrimitiveFactory
{
    
    public static class PlaneFactory
    {
        public const int VERTICES_NUM = 8;
        public const int TRIS_NUM = 12;

        public static string defaultPlane;

        // Creates a two-sided quad (clockwise)
        public static string Create(string entityID, float[] customUvs)
        {
            if (customUvs.Length == 0 && !string.IsNullOrEmpty(defaultPlane))
                return defaultPlane;

            string fileName = $"Plane_{entityID}.obj";
            
            Vector3 halfSize = PrimitivesSize.PLANE_SIZE / 2;

            Vector3[] vertices = PrimitivesBuffersPool.EQUAL_TO_VERTICES.Rent(VERTICES_NUM);
            Vector2[] defaultUVs = PrimitivesBuffersPool.UVS.Rent(VERTICES_NUM);
            int[] tris = PrimitivesBuffersPool.TRIANGLES.Rent(TRIS_NUM);
            Vector3[] normals = PrimitivesBuffersPool.EQUAL_TO_VERTICES.Rent(VERTICES_NUM);

            
            vertices[0] = new Vector3(-halfSize.X, -halfSize.Y, 0);
            vertices[1] = new Vector3(-halfSize.X, halfSize.Y, 0);
            vertices[2] = new Vector3(halfSize.X, halfSize.Y, 0);
            vertices[3] = new Vector3(halfSize.X, -halfSize.Y, 0);

            vertices[4] = new Vector3(halfSize.X, -halfSize.Y, 0);
            vertices[5] = new Vector3(halfSize.X, halfSize.Y, 0);
            vertices[6] = new Vector3(-halfSize.X, halfSize.Y, 0);
            vertices[7] = new Vector3(-halfSize.X, -halfSize.Y, 0);

            defaultUVs = new Vector2[VERTICES_NUM];

            defaultUVs[0] = new Vector2(0f, 0f);
            defaultUVs[1] = new Vector2(0f, 1f);
            defaultUVs[2] = new Vector2(1f, 1f);
            defaultUVs[3] = new Vector2(1f, 0f);

            defaultUVs[4] = new Vector2(1f, 0f);
            defaultUVs[5] = new Vector2(1f, 1f);
            defaultUVs[6] = new Vector2(0f, 1f);
            defaultUVs[7] = new Vector2(0f, 0f);

            tris[0] = 0;
            tris[1] = 1;
            tris[2] = 2;
            tris[3] = 2;
            tris[4] = 3;
            tris[5] = 0;

            tris[6] = 4;
            tris[7] = 5;
            tris[8] = 6;
            tris[9] = 6;
            tris[10] = 7;
            tris[11] = 4;

            normals[0] = new Vector3(0, 0, -1);
            normals[1] = new Vector3(0, 0, -1);
            normals[2] = new Vector3(0, 0, -1);
            normals[3] = new Vector3(0, 0, -1);

            normals[4] = new Vector3(0, 0, 1);
            normals[5] = new Vector3(0, 0, 1);
            normals[6] = new Vector3(0, 0, 1);
            normals[7] = new Vector3(0, 0, 1);

            OBJExporter.CreateOBJFile(fileName , VERTICES_NUM, TRIS_NUM,vertices,  tris, normals, 
                customUvs.Length > 0  ? PrimitiveFactoryUtils.FloatArrayToVector2Array(customUvs, VERTICES_NUM) : defaultUVs);
            
            PrimitivesBuffersPool.EQUAL_TO_VERTICES.Return(vertices);
            PrimitivesBuffersPool.TRIANGLES.Return(tris);
            PrimitivesBuffersPool.EQUAL_TO_VERTICES.Return(normals);
            PrimitivesBuffersPool.UVS.Return(defaultUVs);

            if (customUvs.Length == 0)
                defaultPlane = fileName;

            return fileName;
        }
    }
    
}
