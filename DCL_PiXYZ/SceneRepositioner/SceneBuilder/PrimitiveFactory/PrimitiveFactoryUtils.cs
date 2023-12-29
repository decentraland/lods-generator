using System.Collections.Generic;
using System.Numerics;

namespace DCL_PiXYZ.SceneRepositioner.SceneBuilder.PrimitiveFactory
{
    public class PrimitiveFactoryUtils
    {
        public static Vector2[] FloatArrayToVector2Array(IList<float> uvs, int length)
        {
            var uvsResult = new Vector2[length];
            var uvsResultIndex = 0;

            for (var i = 0; i < uvs.Count && uvsResultIndex < uvsResult.Length;)
                uvsResult[uvsResultIndex++] = new Vector2(uvs[i++], uvs[i++]);

            return uvsResult;
        }
    }
}