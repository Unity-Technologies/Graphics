using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class FadeTransitionNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "FadeTransition",
            "Fade = saturate(FadeValue*(FadeContrast+1)+(NoiseValue-1)*FadeContrast)",
            new ParameterDescriptor("NoiseValue", TYPE.Float, Usage.In, new float[] { 0.5f }),//ShaderStageCapability.Fragment 
            new ParameterDescriptor("FadeValue", TYPE.Float, Usage.In, new float[] { 0.5f }),//ShaderStageCapability.Fragment 
            new ParameterDescriptor("FadeContrast", TYPE.Float, Usage.In, new float[] { 1f }),//ShaderStageCapability.Fragment 
            new ParameterDescriptor("Fade", TYPE.Float, Usage.Out)//ShaderStageCapability.Fragment 
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Artistic, Filter" },
            { "DisplayName", "Fade Transition" },
            { "Tooltip", "a method of adding noise to add variation while a function transitions from on to off" },
            { "Parameters.NoiseValue.Tooltip", "The noise variation to apply to the fade function" },
            { "Parameters.FadeValue.Tooltip", "The amount of transition to apply" },
            { "Parameters.FadeContrast.Tooltip", "The contrast at which a single pixel goes from fully transparent to fully opaque" },
            { "Parameters.Fade.Tooltip", "noise to add variation while a function transitions from on to off" }
        };
    }
}
