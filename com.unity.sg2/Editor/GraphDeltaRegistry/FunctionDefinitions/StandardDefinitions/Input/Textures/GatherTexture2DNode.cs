using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class GatherTexture2DNode : IStandardNode
    {
        public static string Name = "GatherTexture2D";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"#if (SHADER_TARGET >= 41)
    RGBA = Texture.tex.Gather(Sampler.samplerstate, UV, Offset);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;
#else
    R = SAMPLE_TEXTURE2D_LOD(Texture.tex, Sampler.samplerstate, (floor(UV * Texture.texelSize.zw + temp1) + trunc(Offset) + temp2) * Texture.texelSize.xy, 0).r;
    G = SAMPLE_TEXTURE2D_LOD(Texture.tex, Sampler.samplerstate, (floor(UV * Texture.texelSize.zw + temp2) + trunc(Offset) + temp2) * Texture.texelSize.xy, 0).r;
    B = SAMPLE_TEXTURE2D_LOD(Texture.tex, Sampler.samplerstate, (floor(UV * Texture.texelSize.zw + temp3) + trunc(Offset) + temp2) * Texture.texelSize.xy, 0).r;
    A = SAMPLE_TEXTURE2D_LOD(Texture.tex, Sampler.samplerstate, (floor(UV * Texture.texelSize.zw + temp4) + trunc(Offset) + temp2) * Texture.texelSize.xy, 0).r;
    RGBA.r = R;
    RGBA.g = G;
    RGBA.b = B;
    RGBA.a = A;
    RGB = RGBA.rgb;
#endif",
            new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
            new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
            new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
            new ParameterDescriptor("Offset", TYPE.Vec2, Usage.In),
            new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
            new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
            new ParameterDescriptor("R", TYPE.Float, Usage.Out),
            new ParameterDescriptor("G", TYPE.Float, Usage.Out),
            new ParameterDescriptor("B", TYPE.Float, Usage.Out),
            new ParameterDescriptor("A", TYPE.Float, Usage.Out),
            new ParameterDescriptor("temp1", TYPE.Vec2, Usage.Local, new float[] { -0.5f, 0.5f }),
            new ParameterDescriptor("temp2", TYPE.Vec2, Usage.Local, new float[] { 0.5f, 0.5f }),
            new ParameterDescriptor("temp3", TYPE.Vec2, Usage.Local, new float[] { 0.5f, -0.5f }),
            new ParameterDescriptor("temp4", TYPE.Vec2, Usage.Local, new float[] { -0.5f, -0.5f })
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Takes 4 samples (red channel only) to use for bilinear interpolation during sampling.",
            categories: new string[2] { "Input", "Texture" },
            synonyms: new string[0],
            displayName: "Gather Textuire 2D",
            parameters: new ParameterUIDescriptor[10] {
                new ParameterUIDescriptor(
                    name: "Texture",
                    tooltip: "the texture to sample"
                ),
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the UV coordinates to use for sampling the texture",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "Sampler",
                    tooltip: "the Sampler State and its corresponding settings to use for the sample"
                ),
                new ParameterUIDescriptor(
                    name: "RGBA",
                    tooltip: "the red channels of the 4 neighboring pixels from the specified sample position"
                ),
                new ParameterUIDescriptor(
                    name: "RGB",
                    tooltip: "the red channels of the first three neighboring pixels"
                ),
                new ParameterUIDescriptor(
                    name: "R",
                    tooltip: "the first neighboring pixel's red channel"
                ),
                new ParameterUIDescriptor(
                    name: "G",
                    tooltip: "the second neighboring pixel's red channel"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "the third neighboring pixel's red channel"
                ),
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "the fourth neighboring pixel's red channel"
                ),
                new ParameterUIDescriptor(
                    name: "Offset",
                    tooltip: "the pixel offset to apply to the sample's UV coordinates"
                )
            }
        );
    }
}
