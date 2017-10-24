using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/Tangent To World")]
    public class TangentToWorldNode : CodeFunctionNode
    {
        public TangentToWorldNode()
        {
            name = "Tangent To World";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_TangentToWorld", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_TangentToWorld(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] out Vector3 Out,
            [Slot(2, Binding.WorldSpaceTangent)] Vector3 Tangent,
            [Slot(3, Binding.WorldSpaceBitangent)] Vector3 Bitangent,
            [Slot(4, Binding.WorldSpaceNormal)] Vector3 Normal)
        {
            Out = Vector3.zero;
            return
                @"
{
    {precision}3x3 tangentToWorld = transpose({precision}3x3(Tangent, Bitangent, Normal));
    Out = saturate(mul(tangentToWorld, normalize(In)));
}
";
        }
    }
}
