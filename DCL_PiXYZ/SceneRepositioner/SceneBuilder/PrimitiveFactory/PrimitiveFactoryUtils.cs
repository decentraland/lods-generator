using System.Numerics;

namespace DCL_PiXYZ.SceneRepositioner.SceneBuilder.PrimitiveFactory
{
    public class PrimitiveFactoryUtils
    {
        public static Vector2[] FloatArrayToVector2Array(float[] floatArray, int length)
        {
            Vector2[] vectorArray = new Vector2[floatArray.Length / 2];
            for (int i = 0; i < vectorArray.Length; i++)
            {
                vectorArray[i] = new Vector2(floatArray[i * 2], floatArray[i * 2 + 1]);
            }

            return vectorArray;
        }
    }
}