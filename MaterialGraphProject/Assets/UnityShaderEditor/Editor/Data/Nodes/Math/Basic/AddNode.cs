using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Basic/Add")]
    public class AddNode : CodeFunctionNode
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
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] DynamicDimensionVector second,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = first + second;
}
";
        }
    }
}
