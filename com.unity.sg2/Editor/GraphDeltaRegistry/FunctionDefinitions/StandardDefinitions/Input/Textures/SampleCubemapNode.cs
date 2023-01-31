using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SampleCubemapNode : IStandardNode
    {
        public static string Name => "SampleCubemap";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "LODfunction",
@"    RGBA = SAMPLE_TEXTURECUBE_LOD(Cube.tex, Sampler.samplerstate, Dir, LOD);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Cube", TYPE.TextureCube, Usage.In),
                        new ParameterDescriptor("Dir", TYPE.Vec3, Usage.In, REF.WorldSpace_Normal),
                        new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
                        new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                        new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                        new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                    }
                ),
                new(
                    "Standard",
@"    RGBA = SAMPLE_TEXTURECUBE(Cube.tex, Sampler.samplerstate, Dir);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Cube", TYPE.TextureCube, Usage.In),
                        new ParameterDescriptor("Dir", TYPE.Vec3, Usage.In, REF.WorldSpace_Normal),
                        new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                        new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                        new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                    }
                ),
                new(
                    "Biasfunction",
@"    RGBA = SAMPLE_TEXTURECUBE_BIAS(Cube.tex, Sampler.samplerstate, Dir, Bias);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Cube", TYPE.TextureCube, Usage.In),
                        new ParameterDescriptor("Dir", TYPE.Vec3, Usage.In, REF.WorldSpace_Normal),
                        new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("Bias", TYPE.Float, Usage.In),
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
            tooltip: "Samples a Cubemap.",
            category: "Input/Texture",
            synonyms: new string[1] { "texcube" },
            displayName: "Sample Cubemap",
            description: "pkg://Documentation~/previews/SampleCubemap.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "LODfunction", "LOD" },
                { "Standard", "Standard" },
                { "Biasfunction", "Bias" }
            },
            functionSelectorLabel: "Mip Sampling Mode",
            parameters: new ParameterUIDescriptor[11] {
                new ParameterUIDescriptor(
                    name: "Cube",
                    tooltip: "the cubemap asset to sample"
                ),
                new ParameterUIDescriptor(
                    name: "Dir",
                    tooltip: "the direction vector used to sample the cubemap",
                    options: REF.OptionList.Normals
                ),
                new ParameterUIDescriptor(
                    name: "Sampler",
                    tooltip: "the sampler to use for sampling the cubemap"
                ),
                new ParameterUIDescriptor(
                    name: "LOD",
                    tooltip: "the mip level to sample"
                ),
                new ParameterUIDescriptor(
                    name: "Bias",
                    tooltip: "adds or substracts from the auto-generated mip level"
                ),
                new ParameterUIDescriptor(
                    name: "RGBA",
                    tooltip: "A vector4 from the sampled cubemap"
                ),
                new ParameterUIDescriptor(
                    name: "RGB",
                    tooltip: "A vector3 from the sampled cubemap"
                ),
                new ParameterUIDescriptor(
                    name: "R",
                    tooltip: "the red channel of the sampled cubemap"
                ),
                new ParameterUIDescriptor(
                    name: "G",
                    tooltip: "the green channel of the sampled cubemap"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "the blue channel of the sampled cubemap"
                ),
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "the alpha channel of the sampled cubemap"
                )
            }
        );
    }
}
