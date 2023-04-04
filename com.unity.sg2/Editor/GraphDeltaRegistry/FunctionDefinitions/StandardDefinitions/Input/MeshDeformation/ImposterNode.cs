using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ImposterNode : IStandardNode
    {
        public static string Name => "Imposter";
        public static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            "SampleTexture2DStandard",
            functions: new FunctionDescriptor[] {
                new(
                    "OneFrame",
@"",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Albedo&Alpha", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("Normal&Depth", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("Specular&Smoothness", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("Emission&Oclussion", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("Type", TYPE.Int, Usage.Static),//convert this to a dropdown enum
                        new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                        new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                        new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                    }
                ),
                new(
                    "ThreeFrame",
@"",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("Type", TYPE.Int, Usage.Static),//convert this to a dropdown enum
                        new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
                        new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                        new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                        new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Samples a 2D Texture.",
            category: "Input/Texture",
            synonyms: new string[1] { "tex2d" },
            displayName: "Sample Texture 2D",
            selectableFunctions: new()
        {
            { "Standard", "Standard" },
            { "LODfunction", "LOD" },
            { "Gradient", "Gradient" },
            { "Biasfunction", "Bias" }
        },
            functionSelectorLabel: "Mip Sampling Mode",
            parameters: new ParameterUIDescriptor[13] {
                new ParameterUIDescriptor(
                    name: "Texture",
                    tooltip: "the texture asset to sample"
                ),
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the texture coordinates to use for sampling the texture",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "Sampler",
                    tooltip: "the texture sampler to use for sampling the texture"
                ),
                new ParameterUIDescriptor(
                    name: "LOD",
                    tooltip: "explicitly defines the mip level to sample"
                ),
                new ParameterUIDescriptor(
                    name: "DDX",
                    tooltip: "the horizontal derivitive used to calculate the mip level"
                ),
                new ParameterUIDescriptor(
                    name: "DDY",
                    tooltip: "the vertical derivitive used to calculate the mip level"
                ),
                new ParameterUIDescriptor(
                    name: "Bias",
                    tooltip: "adds or substracts from the auto-generated mip level"
                ),
                new ParameterUIDescriptor(
                    name: "RGBA",
                    tooltip: "A vector4 from the sampled texture"
                ),
                new ParameterUIDescriptor(
                    name: "RGB",
                    tooltip: "A vector3 from the sampled texture"
                ),
                new ParameterUIDescriptor(
                    name: "R",
                    tooltip: "the red channel of the sampled texture"
                ),
                new ParameterUIDescriptor(
                    name: "G",
                    tooltip: "the green channel of the sampled texture"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "the blue channel of the sampled texture"
                ),
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "the alpha channel of the sampled texture"
                )
            }
        );
    }
}
