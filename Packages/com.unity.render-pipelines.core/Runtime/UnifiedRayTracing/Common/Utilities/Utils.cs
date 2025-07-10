
using System;
using System.Diagnostics;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal static class Utils
    {
        public static void Destroy(UnityEngine.Object obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying && !UnityEditor.EditorApplication.isPaused)
                    UnityEngine.Object.Destroy(obj);
                else
                    UnityEngine.Object.DestroyImmediate(obj);
#else
                UnityEngine.Object.Destroy(obj);
#endif
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void CheckArgIsNotNull(System.Object obj, string argName)
        {
            if (obj == null)
                throw new ArgumentNullException(argName);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void CheckArg(bool condition, string message)
        {
            if (!condition)
                throw new ArgumentException(message);
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void CheckArgRange<T>(T value, T minIncluded, T maxExcluded, string argName) where T: IComparable
        {
            if (value.CompareTo(minIncluded) < 0 || value.CompareTo(maxExcluded) >= 0)
            {
                var message = $"{argName}={value}, it must be in the range [{minIncluded}, {maxExcluded}[";
                throw new ArgumentOutOfRangeException(argName, message);
            }
        }
    }
}

