using System;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Channel", "Combine")]
    class CombineNode : CodeFunctionNode
    {
        public CombineNode()
        {
            name = "Combine";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Combine", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Combine(
            [Slot(0, Binding.None)] Vector1 R,
            [Slot(1, Binding.None)] Vector1 G,
            [Slot(2, Binding.None)] Vector1 B,
            [Slot(3, Binding.None)] Vector1 A,
            [Slot(4, Binding.None)] out Vector4 RGBA,
            [Slot(5, Binding.None)] out Vector3 RGB,
            [Slot(6, Binding.None)] out Vector2 RG)
        {
            RGBA = Vector4.zero;
            RGB = Vector3.zero;
            RG = Vector2.zero;
            return @"
{
    RGBA = $precision4(R, G, B, A);
    RGB = $precision3(R, G, B);
    RG = $precision2(R, G);
}
";
        }
    }
}
