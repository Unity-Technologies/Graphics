using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/DDX")]
    public class DDXNode : CodeFunctionNode
    {
        public DDXNode()
        {
            name = "DDX";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DDX", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DDX(
            [Slot(0, Binding.None)] Vector1 argument,
            [Slot(1, Binding.None)] out Vector1 result)
        {
            return
                @"
{
    result = ddx(argument);
}
";
        }
    }
}
