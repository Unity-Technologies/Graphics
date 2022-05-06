using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SampleTexture3DNode : IStandardNode
    {
        public static string Name = "SampleTexture3D";
        public static int Version = 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Standard",
@"
{
    RGBA = SAMPLE_TEXTURE3D(Texture.tex, Sampler.samplerstate, UV);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;
}",
                    new ParameterDescriptor("Texture", TYPE.Texture3D, Usage.In),
                    new ParameterDescriptor("UV", TYPE.Vec3, Usage.In),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                    new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                ),
                new(
                    1,
                    "LOD",
@"
{
    RGBA = SAMPLE_TEXTURE3D_LOD(Texture.tex, Sampler.samplerstate, UV, LOD);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;
}",
                    new ParameterDescriptor("Texture", TYPE.Texture3D, Usage.In),
                    new ParameterDescriptor("UV", TYPE.Vec3, Usage.In),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                    new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Samples a 3D Texture.",
            categories: new string[2] { "Input", "Texture" },
            synonyms: new string[2] { "volume", "tex3d" },
            selectableFunctions: new()
            {
                { "Standard", "Standard" },
                { "LOD", "LOD" }
            },
            parameters: new ParameterUIDescriptor[10] {
                new ParameterUIDescriptor(
                    name: "Texture",
                    tooltip: "the texture asset to sample"
                ),
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the texture coordinates to use for sampling the texture"
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
