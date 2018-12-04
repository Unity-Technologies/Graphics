using UnityEngine;

namespace UnityEditor.Experimental.Rendering.TestFramework
{
    public static class YAMLUtilities
    {
        public static string ToYAML(this Quaternion v) => $"{{x: {v.x}, y: {v.y}, z: {v.z}, w: {v.w}}}";
        public static string ToYAML(this Vector3 v) => $"{{x: {v.x}, y: {v.y}, z: {v.z}}}";
        public static string ToYAML(this Color v) => $"{{r: {v.r}, g: {v.g}, b: {v.b}, a: {v.a}}}";
    }
}
