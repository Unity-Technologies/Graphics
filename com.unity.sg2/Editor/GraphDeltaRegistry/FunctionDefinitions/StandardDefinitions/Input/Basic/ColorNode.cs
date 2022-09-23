using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class ColorNode : IStandardNode
    {
        public static string Name => "Color";
        public static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "Default",
                    //regular color picker assumes SRGB space
                    @"    Out = IsGammaSpace() ? Color : float4(SRGBToLinear(Color.rgb), Color.a);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Color", TYPE.Vec4, Usage.Static),
                        new ParameterDescriptor("Out", TYPE.Vec4, Usage.Out)
                    }
                ),
                new(
                    "HDR",
                    //HDR color picker assumes Linear space
                    @"    Out = IsGammaSpace() ? float4(LinearToSRGB(ColorHDR.rgb), Color.a) : ColorHDR;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("ColorHDR", TYPE.Vec4, Usage.Static),
                        new ParameterDescriptor("Out", TYPE.Vec4, Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Color",
            tooltip: "Creates an RGBA value.",
            category: "Input/Basic",
            synonyms: new string[1] { "rgba" },
            selectableFunctions: new()
            {
                { "Default", "Default" },
                { "HDR", "HDR" }
            },
            functionSelectorLabel: "Mode",
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "Color",
                    displayName: " ",
                    useColor: true
                ),
                new ParameterUIDescriptor(
                    name: "ColorHDR",
                    displayName: " ",
                    useColor: true
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: " ",
                    tooltip: "a constant RGBA color value"
                )
            }
        );
    }
}
