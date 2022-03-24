using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class FadeTransitionNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "FadeTransition",
            "Fade = saturate(FadeValue*(FadeContrast+1)+(NoiseValue-1)*FadeContrast)",
            new ParameterDescriptor("NoiseValue", TYPE.Float, Usage.In, new float[] { 0.5f }),
            new ParameterDescriptor("FadeValue", TYPE.Float, Usage.In, new float[] { 0.5f }),
            new ParameterDescriptor("FadeContrast", TYPE.Float, Usage.In, new float[] { 1f }), 
            new ParameterDescriptor("Fade", TYPE.Float, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Artistic, Filter" },
            { "DisplayName", "Fade Transition" },
            { "Tooltip", "a method of adding noise to add variation while a function transitions from on to off" },
            { "Parameters.NoiseValue.Tooltip", "the noise variation to apply to the fade function" },
            { "Parameters.FadeValue.Tooltip", "the amount of transition to apply" },
            { "Parameters.FadeContrast.Tooltip", "the contrast at which a single pixel goes from fully transparent to fully opaque" },
            { "Parameters.Fade.Tooltip", "noise to add variation while a function transitions from on to off" }
        };
    }
}
