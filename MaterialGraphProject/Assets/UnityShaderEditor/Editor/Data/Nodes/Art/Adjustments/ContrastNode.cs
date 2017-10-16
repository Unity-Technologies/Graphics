using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Art/Adjustments/Contrast")]
    public class ContrastNode : CodeFunctionNode
    {
        public ContrastNode()
        {
            name = "Contrast";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Contrast", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Contrast(
            [Slot(0, Binding.None)] Vector3 input,
            [Slot(1, Binding.None)] Vector1 contrast,
            [Slot(2, Binding.None)] Vector1 midPoint,
            [Slot(3, Binding.None)] out Vector3 result)
        {
            result = Vector2.zero;
            return
                @"
{
    // Contrast (reacts better when applied in log)
    // Optimal range: [0.0, 2.0]]
    // From PostProcessing
    result =  (input - midPoint) * contrast + midPoint;
}";
        }
    }
}
