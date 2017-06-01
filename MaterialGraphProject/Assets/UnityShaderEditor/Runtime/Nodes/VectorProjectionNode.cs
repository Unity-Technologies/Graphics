using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Vector/Projection Node")]
    public class VectorProjectionNode : CodeFunctionNode
    {
        public VectorProjectionNode()
        {
            name = "VectorProjection";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_VectorProjection", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_VectorProjection(
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] DynamicDimensionVector second,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = second * dot(first, second) / dot(second, second);
}";
        }
    }
}
