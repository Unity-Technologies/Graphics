using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Vector/DDXY")]
    public class DDXYNode : CodeFunctionNode
    {
        public DDXYNode()
        {
            name = "DDXY";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DDXY", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DDXY(
            [Slot(0, Binding.None)] Vector1 argument,
            [Slot(1, Binding.None)] out Vector1 result)
        {
            return
                @"
{
    result = abs(ddx(argument) + ddy(argument));
}
";
        }
    }
}
