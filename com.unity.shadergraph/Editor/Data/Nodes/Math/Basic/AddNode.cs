using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Add")]
    class AddNode : CodeFunctionNode
    {
        public AddNode()
        {
            name = "Add";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Add", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Add(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = A + B;
}
";
        }
    }
}
