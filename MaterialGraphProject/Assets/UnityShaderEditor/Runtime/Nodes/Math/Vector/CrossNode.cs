using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/Cross Product")]
    public class CrossNode : CodeFunctionNode
    {
        public CrossNode()
        {
            name = "CrossProduct";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_CrossProduct", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_CrossProduct(
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] DynamicDimensionVector second,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = cross(first, second);
}
";
        }
    }
}
