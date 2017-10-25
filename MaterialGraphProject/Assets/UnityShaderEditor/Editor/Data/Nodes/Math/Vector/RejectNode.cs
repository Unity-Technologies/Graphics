using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/Reject")]
    public class RejectNode : CodeFunctionNode
    {
        public RejectNode()
        {
            name = "Reject";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Reject", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Reject(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = A - (B * dot(A, B) / dot(B, B));
}
";
        }
    }
}
