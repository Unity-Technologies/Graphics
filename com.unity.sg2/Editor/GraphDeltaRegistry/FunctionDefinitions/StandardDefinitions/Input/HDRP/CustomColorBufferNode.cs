using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class CustomColorBufferNode : IStandardNode
    {
        public static string Name => "CustomColorBuffer";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
//SampleCustomColor() isn't found
@"    Out = SampleCustomColor(UV);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.ScreenPosition_Default),
                new ParameterDescriptor("Out", TYPE.Vec4, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Custom Color Buffer",
            tooltip: "Gets the custom color buffer.",
            category: "Input/HDRP",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "The screen coordinates to use for the sample",
                    options: REF.OptionList.ScreenPositions
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the sample of the custom buffer"
                )
            }
        );
    }
}
