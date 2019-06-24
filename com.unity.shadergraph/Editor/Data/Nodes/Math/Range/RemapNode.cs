using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph.Hlsl;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "Remap")]
    class RemapNode : CodeFunctionNode
    {
        public RemapNode()
        {
            name = "Remap";
        }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Remap", BindingFlags.Static | BindingFlags.NonPublic);
        }

        [HlslCodeGen]
        static void Unity_Remap(
            [Slot(0, Binding.None, -1, -1, -1, -1)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None, -1, 1, 0, 0)] Float2 InMinMax,
            [Slot(2, Binding.None, 0, 1, 0, 0)] Float2 OutMinMax,
            [Slot(3, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
        }
    }
}
