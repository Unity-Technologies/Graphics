using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Range/RandomRange")]
    public class RandomRangeNode : CodeFunctionNode
    {
        public RandomRangeNode()
        {
            name = "RandomRange";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Randomrange", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Randomrange(
            [Slot(0, Binding.None)] Vector2 seed,
            [Slot(1, Binding.None)] Vector1 min,
            [Slot(2, Binding.None)] Vector1 max,
            [Slot(3, Binding.None)] out Vector1 result)
        {
            return
                @"
{
     {precision} randomno =  frac(sin(dot(seed, float2(12.9898, 78.233)))*43758.5453);
     retresult = floor(randomno * (max - min + 1)) + min;
}";
        }
    }
}
