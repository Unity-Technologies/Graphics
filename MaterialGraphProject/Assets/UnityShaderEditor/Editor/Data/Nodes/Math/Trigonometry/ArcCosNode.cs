using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Trigonometry/ArcCos")]
    public class ACosNode : CodeFunctionNode
    {
        public ACosNode()
        {
            name = "ArcCos";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ACos", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ACos(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = acos(argument);
}
";
        }
    }
}
