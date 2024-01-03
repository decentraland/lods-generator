using System;
using System.Numerics;
using UnityEngine.Pixyz.Geom;
using Vector3 = System.Numerics.Vector3;

namespace DCL_PiXYZ
{
    public static class PiXYZMathUtils
    {
        public static void Init(this Matrix4 matrix4)
        {
            for (int i = 0; i < 4; i++)
                matrix4.tab[i].tab[i] = 1;
        }
        
        public static void Translate(this Matrix4 matrix4, Vector3 translation)
        {
            //Transformation from Unity coordinates to PiXYZ coordinates
            Vector3 rightHandedVector = new Vector3(-translation.X, translation.Y, translation.Z) * 1000;
            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(rightHandedVector);
            matrix4.ApplyResult(Matrix4x4.Multiply(matrix4.ToMatrix4x4(), translationMatrix));
        }
        
        public static void Rotate(this Matrix4 matrix4, Quaternion rotation)
        {
            //Transformation from Unity coordinates to PiXYZ coordinates
            Quaternion rightHandedQuaternion = new Quaternion(rotation.X, -rotation.Y, -rotation.Z, rotation.W);
            matrix4.ApplyResult(Matrix4x4.Multiply(matrix4.ToMatrix4x4(), Matrix4x4.CreateFromQuaternion(rightHandedQuaternion)));
        }
        
        public static void Scale(this Matrix4 matrix4, Vector3 scaleFactors)
        {
            //TODO: Ask Pravus. There are values set as 0 anyways. Whats your opinion here? (Like 828 in Genesis Plaza)
            // Replace zero values with 1 to avoid collapsing the object
            scaleFactors = new Vector3(
                scaleFactors.X == 0 ? 1 : scaleFactors.X,
                scaleFactors.Y == 0 ? 1 : scaleFactors.Y,
                scaleFactors.Z == 0 ? 1 : scaleFactors.Z);

            Matrix4x4 scalingMatrix = Matrix4x4.CreateScale(scaleFactors);
            matrix4.ApplyResult(Matrix4x4.Multiply(matrix4.ToMatrix4x4(), scalingMatrix));
        }
        
        
        private static Matrix4x4 ToMatrix4x4(this Matrix4 matrix)
        {
            return new Matrix4x4(
                (float)matrix.tab[0].tab[0], (float)matrix.tab[0].tab[1], (float)matrix.tab[0].tab[2], (float)matrix.tab[0].tab[3],
                (float)matrix.tab[1].tab[0], (float)matrix.tab[1].tab[1], (float)matrix.tab[1].tab[2], (float)matrix.tab[1].tab[3],
                (float)matrix.tab[2].tab[0], (float)matrix.tab[2].tab[1], (float)matrix.tab[2].tab[2], (float)matrix.tab[2].tab[3],
                (float)matrix.tab[3].tab[0], (float)matrix.tab[3].tab[1], (float)matrix.tab[3].tab[2], (float)matrix.tab[3].tab[3]);
        }
        
        private static void ApplyResult(this Matrix4 result, Matrix4x4 resultToApply)
        {
            result.tab[0].tab[0] = resultToApply.M11;
            result.tab[0].tab[1] = resultToApply.M21;
            result.tab[0].tab[2] = resultToApply.M31;
            result.tab[0].tab[3] = resultToApply.M41;
            
            result.tab[1].tab[0] = resultToApply.M12;
            result.tab[1].tab[1] = resultToApply.M22;
            result.tab[1].tab[2] = resultToApply.M32;
            result.tab[1].tab[3] = resultToApply.M42;

            result.tab[2].tab[0] = resultToApply.M13;
            result.tab[2].tab[1] = resultToApply.M23;
            result.tab[2].tab[2] = resultToApply.M33;
            result.tab[2].tab[3] = resultToApply.M43;

            result.tab[3].tab[0] = resultToApply.M14;
            result.tab[3].tab[1] = resultToApply.M24;
            result.tab[3].tab[2] = resultToApply.M34;
            result.tab[3].tab[3] = resultToApply.M44;
           
        }
        
    }
}