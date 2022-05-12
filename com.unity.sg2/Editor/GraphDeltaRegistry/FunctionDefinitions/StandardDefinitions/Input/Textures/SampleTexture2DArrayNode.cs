 using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SampleTexture2DArrayNode : IStandardNode
    {
        public static string Name = "SampleTexture2DArray";
        public static int Version = 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "SampleTexture2DArrayStandard",
@"  RGBA = SAMPLE_TEXTURE2D_ARRAY(TextureArray.tex, Sampler.samplerstate, UV, Index);
    if(Type == 1) RGBA.rgb = UnpackNormal(RGBA);
    if(Type == 2) RGBA.rgb = UnpackNormalRGB(RGBA);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;",
                    new ParameterDescriptor("TextureArray", TYPE.Texture2DArray, Usage.In),
                    new ParameterDescriptor("Index", TYPE.Float, Usage.In),
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, defaultValue: REF.UV0),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("Type", TYPE.Int, Usage.Static),//convert this to a dropdown enum
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                    new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                ),
                new(
                    1,
                    "SampleTexture2DArrayLOD",
@"  RGBA = SAMPLE_TEXTURE2D_ARRAY_LOD(TextureArray.tex, Sampler.samplerstate, UV, Index, LOD);
    if(Type == 1) RGBA.rgb = UnpackNormal(RGBA);
    if(Type == 2) RGBA.rgb = UnpackNormalRGB(RGBA);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;",
                    new ParameterDescriptor("TextureArray", TYPE.Texture2DArray, Usage.In),
                    new ParameterDescriptor("Index", TYPE.Float, Usage.In),
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, defaultValue: REF.UV0),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("Type", TYPE.Int, Usage.Static),//convert this to a dropdown enum
                    new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                    new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                ),
                new(
                    1,
                    "SampleTexture2DArrayGradient",
@"  RGBA = SAMPLE_TEXTURE2D_ARRAY_GRAD(TextureArray.tex, Sampler.samplerstate, UV, Index, DDX, DDY);
    if(Type == 1) RGBA.rgb = UnpackNormal(RGBA);
    if(Type == 2) RGBA.rgb = UnpackNormalRGB(RGBA);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;",
                    new ParameterDescriptor("TextureArray", TYPE.Texture2DArray, Usage.In),
                    new ParameterDescriptor("Index", TYPE.Float, Usage.In),
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, defaultValue: REF.UV0),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("Type", TYPE.Int, Usage.Static),//convert this to a dropdown enum
                    new ParameterDescriptor("DDX", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("DDY", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                    new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                ),
                new(
                    1,
                    "SampleTexture2DArrayBias",
@"  RGBA = SAMPLE_TEXTURE2D_ARRAY_BIAS(TextureArray.tex, Sampler.samplerstate, UV, Index, Bias);
    if(Type == 1) RGBA.rgb = UnpackNormal(RGBA);
    if(Type == 2) RGBA.rgb = UnpackNormalRGB(RGBA);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;",
                    new ParameterDescriptor("TextureArray", TYPE.Texture2DArray, Usage.In),
                    new ParameterDescriptor("Index", TYPE.Float, Usage.In),
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, defaultValue: REF.UV0),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("Type", TYPE.Int, Usage.Static),//convert this to a dropdown enum
                    new ParameterDescriptor("Bias", TYPE.Float, Usage.In),
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
            tooltip: "Samples a 2D Texture Array.",
            categories: new string[2] { "Input", "Texture" },
            synonyms: new string[3] { "stack", "pile", "tex2darray" },
            selectableFunctions: new()
            {
                { "SampleTexture2DArrayStandard", "Standard" },
                { "SampleTexture2DArrayLOD", "LOD" },
                { "SampleTexture2DArrayGradient", "Gradient" },
                { "SampleTexture2DArrayBias", "Bias" }
            },
            parameters: new ParameterUIDescriptor[14] {
                new ParameterUIDescriptor(
                    name: "Texture",
                    tooltip: "the texture array asset to sample"
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
                    name: "Index",
                    tooltip: "the slice of the array to sample"
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
