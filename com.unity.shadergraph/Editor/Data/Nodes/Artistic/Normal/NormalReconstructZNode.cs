using System.Reflection;
using UnityEngine;

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

        static string NormalReconstructZ(
            [Slot(0, Binding.None)] Vector2 In,
            [Slot(2, Binding.None, ShaderStageCapability.Fragment)] out Vector3 Out)
        {
            Out = Vector3.zero;
            return
                @"
{
    {precision} reconstructZ = sqrt(1.0 - saturate(dot(In.xy, In.xy)));
    {precision}3 normalVector = {precision}3(In.x, In.y, reconstructZ);
    Out = normalize(normalVector);
}";
        }
    }
}
