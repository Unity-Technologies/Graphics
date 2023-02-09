using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class CustomDepthBufferNode : IStandardNode
    {
        public static string Name => "CustomDepthBuffer";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new (
                    "Linear01",
//SampleCustomDepth() not found
@"   Out = Linear01Depth(SampleCustomDepth(UV.xy), _ZBufferParams);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                    }
                ),
                new (
                    "Raw",
//SampleCustomDepth() not found
@"   Out = SampleCustomDepth(UV.xy);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                    }
                ),
                new (
                    "Eye",
//SampleCustomDepth() not found
@"   Out = LinearEyeDepth(SampleCustomDepth(UV.xy), _ZBufferParams)",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec4, Usage.In, REF.ScreenPosition_Default),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
                    }
                ),
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Custom Depth Buffer",
            tooltip: "Gets the custom depth buffer.",
            category: "Input/HDRP",
            synonyms: new string[2] { "z", "buffer" },
            description: "pkg://Documentation~/previews/CustomDepthBuffer.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "Linear01", "Linear01" },
                { "Raw", "Raw" },
                { "Eye", "Eye" }
            },
            hasModes: true,
            functionSelectorLabel: "Sampling Mode",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the screen coordinates to use for the sample",
                    options: REF.OptionList.ScreenPositions
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the sample of the custom depth buffer"
                )
            }
        );
    }
}
