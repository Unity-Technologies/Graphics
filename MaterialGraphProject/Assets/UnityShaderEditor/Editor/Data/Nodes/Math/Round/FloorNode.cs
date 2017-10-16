using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Round/Floor")]
    public class FloorNode : CodeFunctionNode
    {
        public FloorNode()
        {
            name = "Floor";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Floor", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Floor(
            [Slot(0, Binding.None)] DynamicDimensionVector argument,
            [Slot(1, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = floor(argument);
}
";
        }
    }
}
