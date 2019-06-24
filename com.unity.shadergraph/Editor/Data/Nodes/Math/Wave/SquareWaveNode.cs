using System.Reflection;
using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Wave", "Square Wave")]
    class SquareWaveNode : CodeFunctionNode
    {
        public SquareWaveNode()
        {
            name = "Square Wave";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("SquareWave", BindingFlags.Static | BindingFlags.NonPublic);
        }

        [HlslCodeGen]
        static void SquareWave(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = 1.0 - 2.0 * round(frac(In));
        }
    }
}
