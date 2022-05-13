using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class SampleTexture2DLODNode : IStandardNode
    {
        public static string Name = "SampleTexture2DLOD";
        public static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new (
                    1,
                    "Standard",
@"#if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
    RGBA = temp;
#else
    RGBA = SAMPLE_TEXTURE2D_LOD(Texture.tex, Sampler.samplerstate, Texture.GetTransformedUV(UV), LOD);
#endif
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;",
                    new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                    new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("temp", TYPE.Vec4, Usage.Local, new[] { 0.0f, 0.0f, 0.0f, 1.0f})
                ),
                new (
                    1,
                    "NormalObject",
@"#if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
    RGBA = temp;
#else
    RGBA = SAMPLE_TEXTURE2D_LOD(Texture.tex, Sampler.samplerstate, Texture.GetTransformedUV(UV), LOD);
#endif
    RGBA.rgb = UnpackNormalRGB(RGBA);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;",
                    new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                    new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("temp", TYPE.Vec4, Usage.Local, new[] { 0.0f, 0.0f, 0.0f, 1.0f})
                ),
                new (
                    1,
                    "NormalTangent",
@"
#if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
    RGBA = temp;
#else
    RGBA = SAMPLE_TEXTURE2D_LOD(Texture.tex, Sampler.samplerstate, Texture.GetTransformedUV(UV), LOD);
#endif
    RGBA.rgb = UnpackNormal(RGBA);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;",
                    new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                    new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("temp", TYPE.Vec4, Usage.Local, new[] { 0.0f, 0.0f, 0.0f, 1.0f})
                )
            }

        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Samples a 2D Texture with a specified level of detail (LOD).",
            categories: new string[2] { "Input", "Texture" },
            synonyms: new string[2] { "tex2dlod", "mip" },
            displayName: "Sample Texture 2D LOD",
            selectableFunctions: new ()
            {
                { "Standard", "Standard" },
                { "NormalTangent", "Normal Tangent" },
                { "NormalObject", "Normal Object" }
        },
            parameters: new ParameterUIDescriptor[10] {
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
                ),
                new ParameterUIDescriptor(
                    name: "LOD",
                    tooltip: "level of detail to sample"
                )
            }
        );
    }
}
