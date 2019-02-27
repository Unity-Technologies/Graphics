using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Power")]
    class PowerNode : CodeFunctionNode
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
            [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector A,
            [Slot(1, Binding.None, 2, 2, 2, 2)] DynamicDimensionVector B,
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
