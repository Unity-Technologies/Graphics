using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class FadeTransitionNode : IStandardNode
    {
        public static string Name => "FadeTransition";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Fade = saturate(FadeValue*(FadeContrast+1)+(NoiseValue-1)*FadeContrast);",
            new ParameterDescriptor[]
                {
                    new ParameterDescriptor("NoiseValue", TYPE.Float, Usage.In, new float[] { 0.5f }),
                    new ParameterDescriptor("FadeValue", TYPE.Float, Usage.In, new float[] { 0.5f }),
                    new ParameterDescriptor("FadeContrast", TYPE.Float, Usage.In, new float[] { 1f }),
                    new ParameterDescriptor("Fade", TYPE.Float, Usage.Out)
                }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Fade Transition",
            tooltip: "Adds noise for variation while a function transitions from on to off in a graph.",
            category: "Artistic/Filter",
            synonyms: new string[0],
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "NoiseValue",
                    displayName: "Noise Value",
                    tooltip: "the noise variation to apply to the fade function"
                ),
                new ParameterUIDescriptor(
                    name: "FadeValue",
                    displayName: "Fade Value",
                    tooltip: "the amount of transition to apply"
                ),
                new ParameterUIDescriptor(
                    name: "FadeContrast",
                    displayName: "Fade Contrast",
                    tooltip: "the contrast at which a single pixel goes from fully transparent to fully opaque"
                ),
                new ParameterUIDescriptor(
                    name: "Fade",
                    tooltip: "noise to add variation while a function transitions from on to off"
                )
            }
        );
    }
}
