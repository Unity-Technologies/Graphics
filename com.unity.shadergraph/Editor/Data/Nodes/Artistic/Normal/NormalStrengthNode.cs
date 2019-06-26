using System.Reflection;
using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Normal", "Normal Strength")]
    internal class NormalStrengthNode : CodeFunctionNode
    {
        public NormalStrengthNode()
        {
            name = "Normal Strength";
        }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_NormalStrength", BindingFlags.Static | BindingFlags.NonPublic);
        }

        [HlslCodeGen]
        static void Unity_NormalStrength(
            [Slot(0, Binding.None, 0, 0, 1, 0)] Float3 In,
            [Slot(1, Binding.None, 1, 1, 1, 1)] Float Strength,
            [Slot(2, Binding.None)] out Float3 Out)
        {
            Out = Float3(In.xy * Strength, lerp(1, In.z, saturate(Strength)));
        }
    }
}
