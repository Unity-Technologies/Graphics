using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Channel/Combine")]
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
            [Slot(0, Binding.None)] Vector1 first,
            [Slot(1, Binding.None)] Vector1 second,
            [Slot(2, Binding.None)] Vector1 third,
            [Slot(3, Binding.None)] Vector1 fourth,
            [Slot(4, Binding.None)] out Vector4 result)
        {
            result = Vector4.zero;
            return @"
{
    result = float4(first, second, third, fourth);
}
";
        }
    }
}
