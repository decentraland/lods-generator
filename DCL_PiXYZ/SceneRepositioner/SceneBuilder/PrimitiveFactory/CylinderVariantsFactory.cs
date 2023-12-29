
using System;
using System.Numerics;

namespace DCL_PiXYZ.SceneRepositioner.SceneBuilder.PrimitiveFactory
{
    
    /// <summary>
    ///     Produces cylinder, cone, or truncated cone based on the arguments
    /// </summary>
    public static class CylinderVariantsFactory
    {
        public const int VERTICES_NUM = 50;
        public const float OPENING_ANGLE = 0f;
        public const float HEIGHT = PrimitivesSize.CYLINDER_HEIGHT;

        public static string defaultCylinder;

        public static string Create(
            string entityID,
            float radiusTop = 0.5f,
            float radiusBottom = 0.5f,
            int numVertices = VERTICES_NUM,
            float length = HEIGHT,
            float openingAngle = OPENING_ANGLE)
        {
            if (Math.Abs(radiusTop - 0.5f) < 0.01f && Math.Abs(radiusBottom - 0.5f) < 0.01f 
                                                   && !string.IsNullOrEmpty(defaultCylinder))
                return defaultCylinder;
            
            string fileName = $"Cylinder_{entityID}.obj";
            
            if ( openingAngle > 0 && openingAngle < 180)
            {
                radiusTop = 0;
                radiusBottom = (float) (length * Math.Tan(openingAngle * (Math.PI / 180) / 2));
            }

            var offsetPos = new Vector3(0f, -length / 2, 0f);

            int numVertices2 = numVertices + 1;


            int finalVerticesCount = 4 * numVertices2;

            Vector3[] vertices = PrimitivesBuffersPool.EQUAL_TO_VERTICES.Rent(finalVerticesCount);

            // 0..n-1: top, n..2n-1: bottom
            Vector3[] normals = PrimitivesBuffersPool.EQUAL_TO_VERTICES.Rent(finalVerticesCount);

            Vector2[] uvs = PrimitivesBuffersPool.UVS.Rent(finalVerticesCount);

            int[] tris;
            float slope = (float)Math.Atan((radiusBottom - radiusTop) / length); // (rad difference)/height
            float slopeSin = (float)Math.Sin(slope);
            float slopeCos = (float)Math.Cos(slope);
            int i;

            for (i = 0; i < numVertices; i++)
            {
                float angle = (float)(2 * Math.PI * i / numVertices);
                float angleSin = (float)Math.Sin(angle);
                float angleCos = (float)Math.Cos(angle);
                float angleHalf = (float)(2 * Math.PI * (i + 0.5f) / numVertices); // for degenerated normals at cone tips
                float angleHalfSin = (float)Math.Sin(angleHalf);
                float angleHalfCos = (float)Math.Cos(angleHalf);

                vertices[i] = new Vector3(radiusTop * angleCos, length, radiusTop * angleSin) + offsetPos;

                vertices[i + numVertices2] =
                    new Vector3(radiusBottom * angleCos, 0, radiusBottom * angleSin) + offsetPos;

                if (radiusTop == 0)
                    normals[i] = new Vector3(angleHalfCos * slopeCos, -slopeSin, angleHalfSin * slopeCos);
                else
                    normals[i] = new Vector3(angleCos * slopeCos, -slopeSin, angleSin * slopeCos);

                if (radiusBottom == 0)
                    normals[i + numVertices2] = new Vector3(angleHalfCos * slopeCos, -slopeSin, angleHalfSin * slopeCos);
                else
                    normals[i + numVertices2] = new Vector3(angleCos * slopeCos, -slopeSin, angleSin * slopeCos);

                uvs[i] = new Vector2(1.0f - (1.0f * i / numVertices), 1);
                uvs[i + numVertices2] = new Vector2(1.0f - (1.0f * i / numVertices), 0);
            }

            vertices[numVertices] = vertices[0];
            vertices[numVertices + numVertices2] = vertices[0 + numVertices2];
            uvs[numVertices] = new Vector2(1.0f - (1.0f * numVertices / numVertices), 1);
            uvs[numVertices + numVertices2] = new Vector2(1.0f - (1.0f * numVertices / numVertices), 0);
            normals[numVertices] = normals[0];
            normals[numVertices + numVertices2] = normals[0 + numVertices2];

            int coverTopIndexStart = 2 * numVertices2;
            int coverTopIndexEnd = (2 * numVertices2) + numVertices;

            for (i = 0; i < numVertices; i++)
            {
                float angle = (float)(2 * Math.PI * i / numVertices);
                float angleSin = (float)Math.Sin(angle);
                float angleCos = (float)Math.Cos(angle);

                vertices[coverTopIndexStart + i] =
                    new Vector3(radiusTop * angleCos, length, radiusTop * angleSin) + offsetPos;

                normals[coverTopIndexStart + i] = new Vector3(0, 1, 0);
                uvs[coverTopIndexStart + i] = new Vector2((angleCos / 2) + 0.5f, (angleSin / 2) + 0.5f);
            }

            vertices[coverTopIndexStart + numVertices] = new Vector3(0, length, 0) + offsetPos;
            normals[coverTopIndexStart + numVertices] = new Vector3(0, 1, 0);
            uvs[coverTopIndexStart + numVertices] = new Vector2(0.5f, 0.5f);

            int coverBottomIndexStart = coverTopIndexStart + numVertices + 1;
            int coverBottomIndexEnd = coverBottomIndexStart + numVertices;

            for (i = 0; i < numVertices; i++)
            {
                float angle = (float)(2 * Math.PI * i / numVertices);
                float angleSin = (float)Math.Sin(angle);
                float angleCos = (float)Math.Cos(angle);

                vertices[coverBottomIndexStart + i] =
                    new Vector3(radiusBottom * angleCos, 0f, radiusBottom * angleSin) +
                    offsetPos;

                normals[coverBottomIndexStart + i] = new Vector3(0, -1, 0);
                uvs[coverBottomIndexStart + i] = new Vector2((angleCos / 2) + 0.5f, (angleSin / 2) + 0.5f);
            }

            vertices[coverBottomIndexStart + numVertices] = new Vector3(0, 0f, 0) + offsetPos;
            normals[coverBottomIndexStart + numVertices] = new Vector3(0, -1, 0);
            uvs[coverBottomIndexStart + numVertices] = new Vector2(0.5f, 0.5f);



            // create triangles
            // here we need to take care of point order, depending on inside and outside
            var cnt = 0;

            var trianglesCount = 0;

            if (radiusTop == 0)
            {
                // top cone
                trianglesCount = (numVertices2 * 3) + (numVertices * 6);
                tris = PrimitivesBuffersPool.TRIANGLES.Rent(trianglesCount);

                for (i = 0; i < numVertices; i++)
                {
                    tris[cnt++] = i + numVertices2;
                    tris[cnt++] = i;
                    tris[cnt++] = i + 1 + numVertices2;
                }
            }
            else if (radiusBottom == 0)
            {
                // bottom cone
                trianglesCount = (numVertices2 * 3) + (numVertices * 6);
                tris = PrimitivesBuffersPool.TRIANGLES.Rent(trianglesCount);

                for (i = 0; i < numVertices; i++)
                {
                    tris[cnt++] = i;
                    tris[cnt++] = i + 1;
                    tris[cnt++] = i + numVertices2;
                }
            }
            else
            {
                // truncated cone
                trianglesCount = (numVertices * 6) + (numVertices * 6);
                tris = PrimitivesBuffersPool.TRIANGLES.Rent(trianglesCount);

                for (i = 0; i < numVertices; i++)
                {
                    int ip1 = i + 1;

                    tris[cnt++] = i;
                    tris[cnt++] = ip1;
                    tris[cnt++] = i + numVertices2;

                    tris[cnt++] = ip1 + numVertices2;
                    tris[cnt++] = i + numVertices2;
                    tris[cnt++] = ip1;
                }
            }

            for (i = 0; i < numVertices; ++i)
            {
                int next = coverTopIndexStart + i + 1;

                if (next == coverTopIndexEnd) next = coverTopIndexStart;

                tris[cnt++] = next;
                tris[cnt++] = coverTopIndexStart + i;
                tris[cnt++] = coverTopIndexEnd;
            }

            for (i = 0; i < numVertices; ++i)
            {
                int next = coverBottomIndexStart + i + 1;

                if (next == coverBottomIndexEnd) next = coverBottomIndexStart;

                tris[cnt++] = coverBottomIndexEnd;
                tris[cnt++] = coverBottomIndexStart + i;
                tris[cnt++] = next;
            }

            OBJExporter.CreateOBJFile(fileName , finalVerticesCount, trianglesCount,vertices,  tris, normals, uvs);

            PrimitivesBuffersPool.EQUAL_TO_VERTICES.Return(vertices);
            PrimitivesBuffersPool.EQUAL_TO_VERTICES.Return(normals);
            PrimitivesBuffersPool.UVS.Return(uvs);
            PrimitivesBuffersPool.TRIANGLES.Return(tris);
            
            if (Math.Abs(radiusTop - 0.5f) < 0.01f && Math.Abs(radiusBottom - 0.5f) < 0.01f )
                defaultCylinder = fileName;

            return fileName;

        }
        
    }
    
}
