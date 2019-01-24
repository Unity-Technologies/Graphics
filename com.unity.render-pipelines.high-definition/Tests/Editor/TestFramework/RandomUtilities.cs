using NUnit.Framework;
using System;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.TestFramework
{
    public static class RandomUtilities
    {
        public static float RandomFloat(float i, float seed)
        {
            var f = Mathf.Sin((i + 1) * 2.0f) * seed;
            f = f - (int)f;
            if (f < 0) f += 1;
            return f;
        }
        public static Color RandomColor(float i)
        {
            return new Color(
                RandomFloat(i, 1634.3643f),
                RandomFloat(i, 5938.1651f),
                RandomFloat(i, 8315.3246f)
            );
        }

        public static T RandomEnumIndex<T>(float i)
            where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new InvalidOperationException();
            var length = Enum.GetValues(typeof(T)).Length;
            return (T)(object)(int)(RandomFloat(i, 6142.1359f) * (length - 1));
        }

        public static T RandomEnumValue<T>(float i)
            where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new InvalidOperationException();
            var values = Enum.GetValues(typeof(T));
            var length = values.Length;
            return (T)values.GetValue((int)(RandomFloat(i, 6142.1359f) * (length - 1)));
        }

        public static bool RandomBool(float i)
        {
            return RandomFloat(i, 26756.25634f) > 0.5f;
        }

        public static int RandomInt(float i)
        {
            return (int)(RandomFloat(i, 7325.7824f) * 100000);
        }

        public static Vector3 RandomVector3(float i)
        {
            return new Vector3(
                RandomFloat(i, 62054.6842f) * 10000.0f,
                RandomFloat(i, 78645.9785f) * 10000.0f,
                RandomFloat(i, 13056.8760f) * 10000.0f
            );
        }

        public static Quaternion RandomQuaternion(float i)
        {
            return Quaternion.LookRotation(RandomVector3(i));
        }
    }
}
