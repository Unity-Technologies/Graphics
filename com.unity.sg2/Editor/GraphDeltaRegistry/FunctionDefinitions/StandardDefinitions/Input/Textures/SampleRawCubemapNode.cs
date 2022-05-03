using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class SampleRawCubemapNode : IStandardNode
    {
        public static string Name = "SampleCubemap";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
{
    //RGBA = SAMPLE_TEXTURECUBE_LOD(Cube.tex, Sampler.samplerstate, Dir, LOD);
    RGBA = float4(1,1,1,1);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;
}",
            new ParameterDescriptor("Cube", TYPE.Vec4, Usage.In),//fix type
            new ParameterDescriptor("Dir", TYPE.Vec3, Usage.In),//add default world space normal
            new ParameterDescriptor("Sampler", TYPE.Vec2, Usage.In),//fix type
            new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
            new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
            new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
            new ParameterDescriptor("R", TYPE.Float, Usage.Out),
            new ParameterDescriptor("G", TYPE.Float, Usage.Out),
            new ParameterDescriptor("B", TYPE.Float, Usage.Out),
            new ParameterDescriptor("A", TYPE.Float, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "samples a Cubemap and returns a Vector 4 color value",
            categories: new string[2] { "Input", "Texture" },
            synonyms: new string[1] { "texcube" },
            parameters: new ParameterUIDescriptor[10] {
                new ParameterUIDescriptor(
                    name: "Cube",
                    tooltip: "the cubemap asset to sample"
                ),
                new ParameterUIDescriptor(
                    name: "Dir",
                    tooltip: "the direction vector used to sample the cubemap"
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
