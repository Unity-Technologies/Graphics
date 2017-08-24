using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/DDY")]
    public class DDYNode : CodeFunctionNode
    {
        public DDYNode()
        {
            name = "DDY";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DDY", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DDY(
            [Slot(0, Binding.None)] Vector1 argument,
            [Slot(1, Binding.None)] out Vector1 result)
        {
            return
                @"
{
    result = ddy(argument);
}
";
        }
    }
}
