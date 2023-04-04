using System;
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
@"    Out = SampleCustomColor(UV.xy);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                new ParameterDescriptor("Out", TYPE.Vec4, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Custom Color Buffer",
            tooltip: "Gets the custom color buffer.",
            category: "Input/HDRP",
            synonyms: Array.Empty<string>(),
            description: "pkg://Documentation~/previews/CustomColorBuffer.md",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the screen coordinates to use for the sample",
                    options: REF.OptionList.ScreenPositions
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "the sample of the custom buffer"
                )
            }
        );
    }
}
