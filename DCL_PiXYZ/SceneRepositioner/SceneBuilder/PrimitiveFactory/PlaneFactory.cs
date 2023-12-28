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
            vertices[0] = new Vector3(-halfSize.X, -halfSize.Y, 0);
            vertices[1] = new Vector3(-halfSize.X, halfSize.Y, 0);
            vertices[2] = new Vector3(halfSize.X, halfSize.Y, 0);
            vertices[3] = new Vector3(halfSize.X, -halfSize.Y, 0);

            vertices[4] = new Vector3(halfSize.X, -halfSize.Y, 0);
            vertices[5] = new Vector3(halfSize.X, halfSize.Y, 0);
            vertices[6] = new Vector3(-halfSize.X, halfSize.Y, 0);
            vertices[7] = new Vector3(-halfSize.X, -halfSize.Y, 0);

            float[] defaultUVs = PrimitivesBuffersPool.UVS.Rent(VERTICES_NUM);
            defaultUVs[0] = 0;
            defaultUVs[1] = 0;
            defaultUVs[2] = 0;
            defaultUVs[3] = 1;
            defaultUVs[4] = 1;
            defaultUVs[5] = 1;
            defaultUVs[6] = 1;
            defaultUVs[7] = 0;
            defaultUVs[8] = 1;
            defaultUVs[9] = 0;
            defaultUVs[10] = 1;
            defaultUVs[11] = 1;
            defaultUVs[12] = 0;
            defaultUVs[13] = 1;
            defaultUVs[14] = 0;
            defaultUVs[15] = 0;

            int[] tris = PrimitivesBuffersPool.TRIANGLES.Rent(TRIS_NUM);
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

            Vector3[] normals = PrimitivesBuffersPool.EQUAL_TO_VERTICES.Rent(VERTICES_NUM);
            normals[0] = new Vector3(0, 0, -1);
            normals[1] = new Vector3(0, 0, -1);
            normals[2] = new Vector3(0, 0, -1);
            normals[3] = new Vector3(0, 0, -1);

            normals[4] = new Vector3(0, 0, 1);
            normals[5] = new Vector3(0, 0, 1);
            normals[6] = new Vector3(0, 0, 1);
            normals[7] = new Vector3(0, 0, 1);

            OBJExporter.CreateOBJFile(fileName , VERTICES_NUM, TRIS_NUM,vertices,  tris, normals, customUvs.Length > 0  ? customUvs : defaultUVs);
            
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
