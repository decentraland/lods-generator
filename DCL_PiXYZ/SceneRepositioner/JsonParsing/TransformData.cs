using System;

namespace DCL_PiXYZ.SceneRepositioner.JsonParsing
{
    [Serializable]
    public class TransformData : ComponentData
    {
        public Position position = new Position();
        public Rotation rotation = new Rotation();
        public Scale scale = new Scale();
        public double parent = 0;
    }

    [Serializable]
    public class Position
    {
        public float x;
        public float y;
        public float z;

        public Position()
        {
            x = 0;
            y = 0;
            z = 0;
        }

        public Position(float X, float Y, float Z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public override string ToString()
        {
            return $"({x},{y},{z})";
        }
    }

    [Serializable]
    public class Rotation
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Rotation()
        {
            x = 0;
            y = 0;
            z = 0;
            w = 1;
        }

        public Rotation(float X, float Y, float Z, float W)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        public override string ToString()
        {
            return $"({x},{y},{z},{w})";
        }
    }

    [Serializable]
    public class Scale
    {
        public float x;
        public float y;
        public float z;

        public Scale()
        {
            x = 1;
            y = 1;
            z = 1;
        }

        public Scale(float X, float Y, float Z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public override string ToString()
        {
            return $"({x},{y},{z})";
        }
    }
}
