using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Power")]
    public class PowerNode : CodeFunctionNode
    {
        public PowerNode()
        {
            name = "Power";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Power", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Power(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = pow(A, B);
}
";
        }
    }
}
