using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ColorNodeV1 : IStandardNode
    {
        public static string Name => "Color";
        public static int Version => 0;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "Default",
                    //regular color picker assumes SRGB space
@"    LinearColor.rgb = SRGBToLinear(Color.rgb); LinearColor.a = Color.a;
    Out = IsGammaSpace() ? Color : LinearColor;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Color", TYPE.Vec4, Usage.Static),
                        new ParameterDescriptor("LinearColor", TYPE.Vec4, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Vec4, Usage.Out)
                    }
                ),
                new(
                    "HDR",
                    //older version used the same code for both pickers but it was wrong because the HDR picker assumes linear
@"    LinearColor.rgb = SRGBToLinear(Color.rgb); LinearColor.a = Color.a;
    Out = IsGammaSpace() ? Color : LinearColor;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("ColorHDR", TYPE.Vec4, Usage.Static),
                        new ParameterDescriptor("SRGBColor", TYPE.Vec4, Usage.Local),
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
            hasPreview: false,
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "Color",
                    displayName: " ",
                    tooltip: "Default color picker",
                    useColor: true
                ),
                new ParameterUIDescriptor(
                    name: "ColorHDR",
                    displayName: " ",
                    tooltip: "HDR color picker",
                    useColor: true,
                    isHdr: true
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: " ",
                    tooltip: "a constant RGBA color value"
                )
            }
        );
    }

    internal class ColorNodeV2 : IStandardNode
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
@"    LinearColor.rgb = SRGBToLinear(Color.rgb); LinearColor.a = Color.a;
    Out = IsGammaSpace() ? Color : LinearColor;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Color", TYPE.Vec4, Usage.Static),
                        new ParameterDescriptor("LinearColor", TYPE.Vec4, Usage.Local),
                        new ParameterDescriptor("Out", TYPE.Vec4, Usage.Out)
                    }
                ),
                new(
                    "HDR",
                    //HDR color picker assumes Linear space
@"    SRGBColor.rgb = LinearToSRGB(ColorHDR.rgb); SRGBColor.a = ColorHDR.a;
    Out = IsGammaSpace() ? SRGBColor : ColorHDR;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("ColorHDR", TYPE.Vec4, Usage.Static),
                        new ParameterDescriptor("SRGBColor", TYPE.Vec4, Usage.Local),
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
            description: "pkg://Documentation~/previews/Color.md",
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
                    tooltip: "Default color picker",
                    useColor: true
                ),
                new ParameterUIDescriptor(
                    name: "ColorHDR",
                    displayName: " ",
                    tooltip: "HDR color picker",
                    useColor: true,
                    isHdr: true
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
