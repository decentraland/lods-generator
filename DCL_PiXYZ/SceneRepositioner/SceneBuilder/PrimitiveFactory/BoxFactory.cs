
using System.Numerics;

namespace DCL_PiXYZ.SceneRepositioner.SceneBuilder.PrimitiveFactory
{
    
    public static class BoxFactory
    {
        internal const int VERTICES_NUM = 24;
        internal const int TRIS_NUM = 36;

        public static string defaultBox;
        
        public static string Create(string entityID, float[] customUvs)
        {
            if (customUvs.Length == 0 && !string.IsNullOrEmpty(defaultBox))
                return defaultBox;

            string fileName = $"Box_{entityID}.obj";

            Vector3[] vertices = PrimitivesBuffersPool.EQUAL_TO_VERTICES.Rent(VERTICES_NUM); //top bottom left right front back
            Vector3[] normals = PrimitivesBuffersPool.EQUAL_TO_VERTICES.Rent(VERTICES_NUM);
            var defaultUVs =  PrimitivesBuffersPool.UVS.Rent(VERTICES_NUM);
            //TODO:  Do I need uv2?
            Vector2[] uvs2 = PrimitivesBuffersPool.UVS.Rent(VERTICES_NUM);
            int[] tris = PrimitivesBuffersPool.TRIANGLES.Rent(TRIS_NUM);

            var vIndex = 0;

            float size = PrimitivesSize.CUBE_SIZE;

            Vector3 right = new Vector3(1, 0, 0);
            Vector3 left = new Vector3(-1, 0, 0);
            Vector3 forward = new Vector3(0, 0, 1);
            Vector3 back = new Vector3(0, 0, -1);
            Vector3 up = new Vector3(0, 1, 0);
            Vector3 down = new Vector3(0, -1, 0);

            
            //top and bottom
            var start = new Vector3(-size / 2, size / 2, size / 2);
            vertices[vIndex++] = start;
            vertices[vIndex++] = start + (right * size);
            vertices[vIndex++] = start + (right * size) + (back * size);
            vertices[vIndex++] = start + (back * size);

            start = new Vector3(-size / 2, -size / 2, size / 2);
            vertices[vIndex++] = start;
            vertices[vIndex++] = start + (right * size);
            vertices[vIndex++] = start + (right * size) + (back * size);
            vertices[vIndex++] = start + (back * size);

            //left and right
            start = new Vector3(-size / 2, size / 2, size / 2);
            vertices[vIndex++] = start;
            vertices[vIndex++] = start + (back * size);
            vertices[vIndex++] = start + (back * size) + (down * size);
            vertices[vIndex++] = start + (down * size);

            start = new Vector3(size / 2, size / 2, size / 2);
            vertices[vIndex++] = start;
            vertices[vIndex++] = start + (back * size);
            vertices[vIndex++] = start + (back * size) + (down * size);
            vertices[vIndex++] = start + (down * size);

            //front and back
            start = new Vector3(-size / 2, size / 2, size / 2);
            vertices[vIndex++] = start;
            vertices[vIndex++] = start + (right * size);
            vertices[vIndex++] = start + (right * size) + (down * size);
            vertices[vIndex++] = start + (down * size);

            start = new Vector3(-size / 2, size / 2, -size / 2);
            vertices[vIndex++] = start;
            vertices[vIndex++] = start + (right * size);
            vertices[vIndex++] = start + (right * size) + (down * size);
            vertices[vIndex++] = start + (down * size);

            //uv
            var uvIndex = 0;

            //top and bottom
            defaultUVs[uvIndex++] = new Vector2(1f, 1f);
            defaultUVs[uvIndex++] = new Vector2(1f, 0f);
            defaultUVs[uvIndex++] = new Vector2(0f, 0f);
            defaultUVs[uvIndex++] = new Vector2(0f, 1f);

            defaultUVs[uvIndex++] = new Vector2(1f, 0f);
            defaultUVs[uvIndex++] = new Vector2(1f, 1f);
            defaultUVs[uvIndex++] = new Vector2(0f, 1f);
            defaultUVs[uvIndex++] = new Vector2(0f, 0f);

            //left and right
            defaultUVs[uvIndex++] = new Vector2(1f, 1f);
            defaultUVs[uvIndex++] = new Vector2(1f, 0f);
            defaultUVs[uvIndex++] = new Vector2(0f, 0f);
            defaultUVs[uvIndex++] = new Vector2(0f, 1f);

            defaultUVs[uvIndex++] = new Vector2(1f, 0f);
            defaultUVs[uvIndex++] = new Vector2(1f, 1f);
            defaultUVs[uvIndex++] = new Vector2(0f, 1f);
            defaultUVs[uvIndex++] = new Vector2(0f, 0f);

            //front and back
            defaultUVs[uvIndex++] = new Vector2(0f, 0f);
            defaultUVs[uvIndex++] = new Vector2(1f, 0f);
            defaultUVs[uvIndex++] = new Vector2(1f, 1f);
            defaultUVs[uvIndex++] = new Vector2(0f, 1f);

            defaultUVs[uvIndex++] = new Vector2(0f, 1f);
            defaultUVs[uvIndex++] = new Vector2(1f, 1f);
            defaultUVs[uvIndex++] = new Vector2(1f, 0f);
            defaultUVs[uvIndex++] = new Vector2(0f, 0f);

            //uv2
            vIndex = 0;

            //top and bottom
            uvs2[vIndex++] = new Vector2(1f, 1f);
            uvs2[vIndex++] = new Vector2(1f, 0f);
            uvs2[vIndex++] = new Vector2(0f, 0f);
            uvs2[vIndex++] = new Vector2(0f, 1f);

            uvs2[vIndex++] = new Vector2(1f, 0f);
            uvs2[vIndex++] = new Vector2(1f, 1f);
            uvs2[vIndex++] = new Vector2(0f, 1f);
            uvs2[vIndex++] = new Vector2(0f, 0f);

            //left and right
            uvs2[vIndex++] = new Vector2(1f, 1f);
            uvs2[vIndex++] = new Vector2(1f, 0f);
            uvs2[vIndex++] = new Vector2(0f, 0f);
            uvs2[vIndex++] = new Vector2(0f, 1f);

            uvs2[vIndex++] = new Vector2(1f, 0f);
            uvs2[vIndex++] = new Vector2(1f, 1f);
            uvs2[vIndex++] = new Vector2(0f, 1f);
            uvs2[vIndex++] = new Vector2(0f, 0f);

            //front and back
            uvs2[vIndex++] = new Vector2(0f, 0f);
            uvs2[vIndex++] = new Vector2(1f, 0f);
            uvs2[vIndex++] = new Vector2(1f, 1f);
            uvs2[vIndex++] = new Vector2(0f, 1f);

            uvs2[vIndex++] = new Vector2(0f, 1f);
            uvs2[vIndex++] = new Vector2(1f, 1f);
            uvs2[vIndex++] = new Vector2(1f, 0f);
            uvs2[vIndex++] = new Vector2(0f, 0f);

            //normal
            vIndex = 0;

            //top and bottom
            normals[vIndex++] = up;
            normals[vIndex++] = up;
            normals[vIndex++] = up;
            normals[vIndex++] = up;

            normals[vIndex++] = down;
            normals[vIndex++] = down;
            normals[vIndex++] = down;
            normals[vIndex++] = down;

            //left and right
            normals[vIndex++] = left;
            normals[vIndex++] = left;
            normals[vIndex++] = left;
            normals[vIndex++] = left;

            normals[vIndex++] = right;
            normals[vIndex++] = right;
            normals[vIndex++] = right;
            normals[vIndex++] = right;

            //front and back
            normals[vIndex++] = forward;
            normals[vIndex++] = forward;
            normals[vIndex++] = forward;
            normals[vIndex++] = forward;

            normals[vIndex++] = back;
            normals[vIndex++] = back;
            normals[vIndex++] = back;
            normals[vIndex++] = back;

            var cnt = 0;

            //top and bottom
            tris[cnt++] = 0;
            tris[cnt++] = 1;
            tris[cnt++] = 2;
            tris[cnt++] = 0;
            tris[cnt++] = 2;
            tris[cnt++] = 3;

            tris[cnt++] = 4 + 0;
            tris[cnt++] = 4 + 2;
            tris[cnt++] = 4 + 1;
            tris[cnt++] = 4 + 0;
            tris[cnt++] = 4 + 3;
            tris[cnt++] = 4 + 2;

            //left and right
            tris[cnt++] = 8 + 0;
            tris[cnt++] = 8 + 1;
            tris[cnt++] = 8 + 2;
            tris[cnt++] = 8 + 0;
            tris[cnt++] = 8 + 2;
            tris[cnt++] = 8 + 3;

            tris[cnt++] = 12 + 0;
            tris[cnt++] = 12 + 2;
            tris[cnt++] = 12 + 1;
            tris[cnt++] = 12 + 0;
            tris[cnt++] = 12 + 3;
            tris[cnt++] = 12 + 2;

            //front and back
            tris[cnt++] = 16 + 0;
            tris[cnt++] = 16 + 2;
            tris[cnt++] = 16 + 1;
            tris[cnt++] = 16 + 0;
            tris[cnt++] = 16 + 3;
            tris[cnt++] = 16 + 2;

            tris[cnt++] = 20 + 0;
            tris[cnt++] = 20 + 1;
            tris[cnt++] = 20 + 2;
            tris[cnt++] = 20 + 0;
            tris[cnt++] = 20 + 2;
            tris[cnt++] = 20 + 3;

            
            OBJExporter.CreateOBJFile(fileName , VERTICES_NUM, TRIS_NUM,vertices,  tris, normals, 
                customUvs.Length > 0  ? PrimitiveFactoryUtils.FloatArrayToVector2Array(customUvs, VERTICES_NUM) : defaultUVs);
            
            PrimitivesBuffersPool.EQUAL_TO_VERTICES.Return(vertices);
            PrimitivesBuffersPool.TRIANGLES.Return(tris);
            PrimitivesBuffersPool.EQUAL_TO_VERTICES.Return(normals);
            PrimitivesBuffersPool.UVS.Return(defaultUVs);

            if (customUvs.Length == 0)
                defaultBox = fileName;

            return fileName;
        }
    }
    
}
