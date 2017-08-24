using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/SinCos")]
    public class SinCosNode : CodeFunctionNode
    {
        public SinCosNode()
        {
            name = "SinCos";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_SinCos", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_SinCos(
            [Slot(0, Binding.None)] Vector1 argument,
            [Slot(1, Binding.None)] out Vector1 sin,
            [Slot(2, Binding.None)] out Vector1 cos)
        {
            return
                @"
{
    sincos(argument, sin, cos);
}
";
        }
    }
}
