using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Vector/Rejection Node")]
    public class VectorRejectionNode : CodeFunctionNode
    {
        public VectorRejectionNode()
        {
            name = "VectorRejection";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_VectorRejection", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_VectorRejection(
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] DynamicDimensionVector second,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = first - (second * dot(first, second) / dot(second, second));
}
";
        }
    }
}
