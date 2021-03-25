using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Divide")]
    class DivideNode : CodeFunctionNode
    {
        public DivideNode()
        {
            name = "Divide";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Divide", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Divide(
            [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector A,
            [Slot(1, Binding.None, 2, 2, 2, 2)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return @"
{
    Out = A / B;
}
";
        }
    }
}
