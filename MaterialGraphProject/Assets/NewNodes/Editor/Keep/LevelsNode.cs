using UnityEditor.Graphing;
using System.Collections.Generic;
using System.Reflection;

/*namespace UnityEditor.ShaderGraph
{
    [Title("Art", "Adjustments", "Levels")]
    public class LevelsNode : CodeFunctionNode
    {
        public LevelsNode()
        {
            name = "Levels";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Levels", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Levels(
            [Slot(0, Binding.None)] DynamicDimensionVector input,
            [Slot(1, Binding.None)] Vector1 inputMin,
            [Slot(2, Binding.None, 1, 1, 1, 1)] Vector1 inputMax,
            [Slot(3, Binding.None, 1, 1, 1, 1)] Vector1 inputInvGamma,
            [Slot(4, Binding.None)] Vector1 outputMin,
            [Slot(5, Binding.None, 1, 1, 1, 1)] Vector1 outputMax,
            [Slot(6, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
           {precision}{slot6dimension} colorMinClamped = max(arg1 - inputMin, 0.0);
           {precision}{slot6dimension} colorMinMaxClamped = min(colorMinClamped / (inputMax - inputMin), 1.0);
           return lerp(outputMin, outputMax, pow(colorMinMaxClamped, inputInvGamma));
}
";
        }
    }
}*/
