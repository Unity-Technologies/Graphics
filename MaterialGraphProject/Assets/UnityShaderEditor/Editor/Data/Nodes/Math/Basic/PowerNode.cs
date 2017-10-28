using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Basic/Power")]
    public class PowerNode : CodeFunctionNode
    {
        public PowerNode()
        {
            name = "Power";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Pow", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Pow(
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] DynamicDimensionVector second,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = pow(first, second);
}
";
        }
    }
}
