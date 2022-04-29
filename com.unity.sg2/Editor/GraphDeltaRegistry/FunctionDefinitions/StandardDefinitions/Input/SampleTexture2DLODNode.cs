using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class SampleTexture2DLODNode : IStandardNode
    {
        public static string Name = "SampleTexture2DLOD";
        public static int Version = 1;
        //TODO: this node has two drop-downs, need to figure out how to do it
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new (
                    1,
                    "Tangent",
                    @"
                    {
                    #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
                        RGBA = float4(1,1,1,1);
                    #else
                       // RGBA = SAMPLE_TEXTURE2D_LOD(Texture.tex, Sampler.samplerstate, Texture.GetTransformedUV(UV), LOD);
                        RGBA = float4(1,1,1,1);
                    #endif
                    #if (Type == 2)
                        //RGBA.rgb = UnpackNormal(RGBA);
                    #endif
                        RGB = RGBA.rgb;
                        R = RGBA.r;
                        G = RGBA.g;
                        B = RGBA.b;
                        A = RGBA.a;
                    }",
                    new ParameterDescriptor("Texture", TYPE.Vec4, Usage.In),//fix type
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),//add default UVs
                    new ParameterDescriptor("Sampler", TYPE.Vec2, Usage.In),//fix type
                    new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                    new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("Type", TYPE.Int, Usage.Static, new float[]{ 1f })//1 -> default, 2 -> normal 
                ),
                new (
                    1,
                    "Object",
                    @"
                    {
                    #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
                        RGBA = float4(1,1,1,1);
                    #else
                        //RGBA = SAMPLE_TEXTURE2D_LOD(Texture.tex, Sampler.samplerstate, Texture.GetTransformedUV(UV), LOD);
                        RGBA = float4(1,1,1,1);
                    #endif
                    #if (Type == 2)
                        //RGBA.rgb = UnpackNormalRGB(RGBA);
                    #endif
                        RGB = RGBA.rgb;
                        R = RGBA.r;
                        G = RGBA.g;
                        B = RGBA.b;
                        A = RGBA.a;
                    }",
                    new ParameterDescriptor("Texture", TYPE.Vec4, Usage.In),//fix type
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),//add default UVs
                    new ParameterDescriptor("Sampler", TYPE.Vec2, Usage.In),//fix type
                    new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                    new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("Type", TYPE.Int, Usage.Static, new float[]{ 1f })//1 -> default, 2 -> normal 
                )
            }

        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "samples a 2D Texture with customizable LOD level and returns a Vector 4 color value",
            categories: new string[2] { "Input", "Texture" },
            synonyms: new string[2] { "tex2dlod", "mip" },
            displayName: "Sample Texture 2D LOD",
            selectableFunctions: new ()
            {
                { "Tangent", "Tangent" },
                { "Object", "Object" }
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
