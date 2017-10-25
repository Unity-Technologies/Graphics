using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/Sin")]
    class SinNode : CodeFunctionNode
    {
        public SinNode()
        {
            name = "Sin";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Sin", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Sin(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = sin(In);
}
";
        }
    }
}
