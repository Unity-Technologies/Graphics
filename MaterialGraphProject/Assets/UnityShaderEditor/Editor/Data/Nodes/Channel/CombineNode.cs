using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Channel", "Combine")]
    public class CombineNode : CodeFunctionNode
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
            [Slot(4, Binding.None)] out Vector4 RGBA)
        {
            RGBA = Vector4.zero;
            return @"
{
    RGBA = float4(R, G, B, A);
}
";
        }
    }
}
