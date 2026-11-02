
using System;
using System.Diagnostics;

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

#if UNITY_EDITOR
        public static void EnsureMeshHasRawBufferTarget(Mesh mesh)
        {
            if (UnityEditor.Rendering.EditorGraphicsSettings.defaultMeshBufferTarget != UnityEditor.Rendering.DefaultMeshBufferTarget.Raw)
            {
                mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
                mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            }
        }
#endif

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
        public static void CheckArgRange<T>(T value, T minIncluded, T maxExcluded, string argName) where T: IComparable<T>
        {
            if (value.CompareTo(minIncluded) < 0 || value.CompareTo(maxExcluded) >= 0)
            {
                var message = $"{argName}={value}, it must be in the range [{minIncluded}, {maxExcluded}[";
                throw new ArgumentOutOfRangeException(argName, message);
            }
        }

        [Conditional("UNITY_ASSERTIONS")]
        public static void CheckSupport(bool value, string message)
        {
            if (!value)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

