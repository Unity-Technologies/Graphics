using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Projection")]
    class ProjectionNode : CodeFunctionNode
    {
        public ProjectionNode()
        {
            name = "Projection";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Projection", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Projection(
            [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector A,
            [Slot(1, Binding.None, 0, 1, 0, 0)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = B * dot(A, B) / dot(B, B);
}";
        }
    }
}
