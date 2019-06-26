using System.Reflection;
using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Normal", "Normal Reconstruct Z")]
    class NormalReconstructZNode : CodeFunctionNode
    {
        public NormalReconstructZNode()
        {
            name = "Normal Reconstruct Z";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("NormalReconstructZ", BindingFlags.Static | BindingFlags.NonPublic);
        }

        [HlslCodeGen]
        static void NormalReconstructZ(
            [Slot(0, Binding.None)] Float2 In,
            [Slot(2, Binding.None, ShaderStageCapability.Fragment)] out Float3 Out)
        {
            var reconstructZ = sqrt(1.0 - saturate(dot(In, In)));
            var normalVector = Float3(In, reconstructZ);
            Out = normalize(normalVector);
        }
    }
}
