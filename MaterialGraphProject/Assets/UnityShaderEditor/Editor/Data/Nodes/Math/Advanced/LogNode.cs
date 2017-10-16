using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Advanced/Log")]
    public class LogNode : CodeFunctionNode
    {
        public LogNode()
        {
            name = "Log";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Log", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Log(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = log(argument);
}
";
        }
    }
}
