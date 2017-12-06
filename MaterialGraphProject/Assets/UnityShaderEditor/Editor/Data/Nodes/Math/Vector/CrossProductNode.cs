using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Cross Product")]
    public class CrossProductNode : CodeFunctionNode
    {
        public CrossProductNode()
        {
            name = "Cross Product";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_CrossProduct", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_CrossProduct(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = cross(A, B);
}
";
        }
    }
}
