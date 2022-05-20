using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class CalculateLevelOfDetailTexture2DNode : IStandardNode
    {
        public static string Name = "CalculateLevelOfDetailTexture2D";
        public static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"#if (SHADER_TARGET >= 41)
    if (Clamp)
    {
        LOD = Texture.tex.CalculateLevelOfDetail(Sampler.samplerstate, UV);
    }else
    {
        LOD = Texture.tex.CalculateLevelOfDetailUnclamped(Sampler.samplerstate, UV);
	}
#else
	LOD = 0.5f*log2(max(dot(ddx(UV * Texture.texelSize.zw), ddx(UV * Texture.texelSize.zw)), dot(ddy(UV * Texture.texelSize.zw), ddy(UV * Texture.texelSize.zw))));
	LOD = max(LOD, 0);
	#if defined(MIP_COUNT_SUPPORTED)
		LOD = min(LOD, GetMipCount(TEXTURE2D_ARGS(Texture.tex, Sampler.samplerstate))-1);
	#endif
#endif",
            new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
            new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
            new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
            new ParameterDescriptor("Clamp", TYPE.Bool, Usage.Static, new float[] { 1 }),
            new ParameterDescriptor("LOD", TYPE.Float, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Computes LOD values for use with texture sample nodes.",
            categories: new string[2] { "Input", "Texture" },
            synonyms: new string[0] {  },
            displayName: "Calculate Level Of Detail Texture 2D",

            parameters: new ParameterUIDescriptor[5] {
                new ParameterUIDescriptor(
                    name: "Texture",
                    tooltip: "the texture asset from which to calculate LODs"
                ),
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the texture coordinates to use for the texture",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "Sampler",
                    tooltip: "the texture sampler to use for the texture"
                ),
                new ParameterUIDescriptor(
                    name: "Clamp",
                    tooltip: "clamps the output to the mips on the input Texture or an ideal mip level for a texture with all mips present"
                ),
                new ParameterUIDescriptor(
                    name: "LOD",
                    tooltip: "the calculated level of detail"
                )
            }
        );
    }
}
