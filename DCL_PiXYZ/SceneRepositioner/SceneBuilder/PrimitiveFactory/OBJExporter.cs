using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace DCL_PiXYZ.SceneRepositioner.SceneBuilder.PrimitiveFactory
{
    public static class OBJExporter
    {
        public static void CreateOBJFile(string filePath, int verticesNum, int trisNum, Vector3[] vertices, int[] tris, Vector3[] normals, Vector2[] customUvs)
        {
            var objContent = CreateOBJContent(verticesNum, trisNum, vertices, tris, normals, customUvs);
            File.WriteAllText(Path.Combine(PXYZConstants.RESOURCES_DIRECTORY, filePath), objContent);
        }

        private static string CreateOBJContent(int verticesNum, int trisNum, Vector3[] vertices, int[] tris, Vector3[] normals, Vector2[] uvs)
        {
            List<string> objLines = new List<string>();

            for (var index = 0; index < verticesNum; index++)
            {
                var v = vertices[index];
                objLines.Add($"v {v.X} {v.Y} {v.Z}");
            }
            
            for (var index = 0; index < verticesNum; index++)
            {
                string u = uvs[index].X.ToString("0.0");
                string v = uvs[index].Y.ToString("0.0");
                objLines.Add($"vt {u} {v}");
            }

            for (var index = 0; index < verticesNum; index++)
            {
                var n = normals[index];
                objLines.Add($"vn {n.X} {n.Y} {n.Z}");
            }

            for (int i = 0; i < trisNum; i += 3)
            {
                int v1 = tris[i] + 1;
                int v2 = tris[i + 1] + 1;
                int v3 = tris[i + 2] + 1;
                objLines.Add($"f {v1}/{v1}/{v1} {v2}/{v2}/{v2} {v3}/{v3}/{v3}");
            }

            return string.Join("\n", objLines);
        }
    }
}