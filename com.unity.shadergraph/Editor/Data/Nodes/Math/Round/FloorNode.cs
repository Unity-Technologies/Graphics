using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Round", "Floor")]
    class FloorNode : CodeFunctionNode
    {
        public FloorNode()
        {
            name = "Floor";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Floor", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Floor(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = floor(In);
}
";
        }
    }
}
