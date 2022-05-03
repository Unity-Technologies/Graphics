using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class SampleTexture2DNode : IStandardNode
    {
        public static string Name = "SampleTexture2D";
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
    //RGBA = SAMPLE_TEXTURE2D(Texture.tex, Texture.samplerstate, Texture.GetTransformedUV(UV));
    RGBA = float4(1,1,1,1);
    if(Type == 1) RGBA.rgb = UnpackNormal(RGBA);
    if(Type == 2) RGBA.rgb = UnpackNormalRGB(RGBA);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;
}",
                    new ParameterDescriptor("Texture", TYPE.Vec4, Usage.In),//fix type
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),//add default UVs
                    new ParameterDescriptor("Sampler", TYPE.Vec2, Usage.In),//fix type
                    new ParameterDescriptor("Type", TYPE.Int, Usage.Static),
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
    //RGBA = SAMPLE_TEXTURE2D_LOD(Texture.tex, Texture.samplerstate, Texture.GetTransformedUV(UV));
    RGBA = float4(1,1,1,1);
    if(Type == 1) RGBA.rgb = UnpackNormal(RGBA);
    if(Type == 2) RGBA.rgb = UnpackNormalRGB(RGBA);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;
}",
                    new ParameterDescriptor("Texture", TYPE.Vec4, Usage.In),//fix type
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),//add default UVs
                    new ParameterDescriptor("Sampler", TYPE.Vec2, Usage.In),//fix type
                    new ParameterDescriptor("Type", TYPE.Int, Usage.Static),
                    new ParameterDescriptor("LOD", TYPE.Float, Usage.In),//only show when Mip Sampling Mode is set to LOD
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                    new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                ),
                new(
                    1,
                    "Gradient",
@"
{
    //RGBA = SAMPLE_TEXTURE2D_GRAD(Texture.tex, Texture.samplerstate, Texture.GetTransformedUV(UV));
    RGBA = float4(1,1,1,1);
    if(Type == 1) RGBA.rgb = UnpackNormal(RGBA);
    if(Type == 2) RGBA.rgb = UnpackNormalRGB(RGBA);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;
}",
                    new ParameterDescriptor("Texture", TYPE.Vec4, Usage.In),//fix type
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),//add default UVs
                    new ParameterDescriptor("Sampler", TYPE.Vec2, Usage.In),//fix type
                    new ParameterDescriptor("Type", TYPE.Int, Usage.Static),
                    new ParameterDescriptor("DDX", TYPE.Vec2, Usage.In),//only show when Mip Sampling Mode is set to Gradient
                    new ParameterDescriptor("DDY", TYPE.Vec2, Usage.In),//only show when Mip Sampling Mode is set to Gradient
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                    new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                ),
                new(
                    1,
                    "Bias",
@"
{
    //RGBA = SAMPLE_TEXTURE2D_BIAS(Texture.tex, Texture.samplerstate, Texture.GetTransformedUV(UV));
    RGBA = float4(1,1,1,1);
    if(Type == 1) RGBA.rgb = UnpackNormal(RGBA);
    if(Type == 2) RGBA.rgb = UnpackNormalRGB(RGBA);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;
}",
                    new ParameterDescriptor("Texture", TYPE.Vec4, Usage.In),//fix type
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),//add default UVs
                    new ParameterDescriptor("Sampler", TYPE.Vec2, Usage.In),//fix type
                    new ParameterDescriptor("Type", TYPE.Int, Usage.Static),
                    new ParameterDescriptor("Bias", TYPE.Float, Usage.In),//only show when Mip Sampling Mode is set to Bias
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
            tooltip: "samples a 2D Texture and returns a Vector 4 color value",
            categories: new string[2] { "Input", "Texture" },
            synonyms: new string[1] { "tex2d" },
            selectableFunctions: new()
            {
                { "Standard", "Standard" },
                { "LOD", "LOD" },
                { "Gradient", "Gradient" },
                { "Bias", "Bias" }
            },
            parameters: new ParameterUIDescriptor[13] {
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
