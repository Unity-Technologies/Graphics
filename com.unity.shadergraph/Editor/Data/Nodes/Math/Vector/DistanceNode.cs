using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Distance")]
    class DistanceNode : CodeFunctionNode
    {
        public DistanceNode()
        {
            name = "Distance";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Distance", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Distance(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out Vector1 Out)
        {
            return
@"
{
    Out = distance(A, B);
}
";
        }
    }
}
