using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "Clamp")]
    class ClampNode : CodeFunctionNode
    {
        public ClampNode()
        {
            name = "Clamp";
        }


        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Clamp", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Clamp(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] DynamicDimensionVector Min,
            [Slot(2, Binding.None, 1, 1, 1, 1)] DynamicDimensionVector Max,
            [Slot(3, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = clamp(In, Min, Max);
}";
        }
    }
}
