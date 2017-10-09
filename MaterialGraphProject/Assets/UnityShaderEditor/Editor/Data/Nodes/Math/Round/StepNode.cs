using System.Linq.Expressions;
using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Round/Step")]
    public class StepNode : CodeFunctionNode
    {
        public StepNode()
        {
            name = "Step";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Step", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Step(
            [Slot(0, Binding.None)] DynamicDimensionVector first,
            [Slot(1, Binding.None)] DynamicDimensionVector second,
            [Slot(2, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = step(first, second);
}
";
        }
    }
}
