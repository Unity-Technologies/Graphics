using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Vector/Project")]
    public class ProjectNode : CodeFunctionNode
    {
        public ProjectNode()
        {
            name = "Project";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Project", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Project(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
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
