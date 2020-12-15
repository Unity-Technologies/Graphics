using UnityEngine;
using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "Random Range")]
    class RandomRangeNode : CodeFunctionNode
    {
        public RandomRangeNode()
        {
            name = "Random Range";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_RandomRange", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_RandomRange(
            [Slot(0, Binding.None)] Vector2 Seed,
            [Slot(1, Binding.None)] Vector1 Min,
            [Slot(2, Binding.None, 1, 1, 1, 1)] Vector1 Max,
            [Slot(3, Binding.None)] out Vector1 Out)
        {
            return
@"
{
     $precision randomno =  frac(sin(dot(Seed, $precision2(12.9898, 78.233)))*43758.5453);
     Out = lerp(Min, Max, randomno);
}";
        }
    }
}
