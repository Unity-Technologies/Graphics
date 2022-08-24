using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class NormalFromTextureNode : IStandardNode
    {
        public static string Name => "NormalFromTexture";
        public static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "ThreeSamples",
@"    //3 sample version - only works on square textures
    UV = Texture.GetTransformedUV(UV);
    Offset = pow(Offset, 3) * 0.1;
    if (HeightChannel == 1) channelMask = float4(0,1,0,0);
    if (HeightChannel == 2) channelMask = float4(0,0,1,0);
    if (HeightChannel == 3) channelMask = float4(0,0,0,1);
    normalSample = dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, UV), channelMask);
    va.z = (dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, float2(UV.x + Offset, UV.y)), channelMask) - normalSample) * Strength;
    vb.z = (dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, float2(UV.x, UV.y + Offset)), channelMask) - normalSample) * Strength;
    Out = normalize(cross(va.xyz, vb.xyz));
    if (OutputSpace==1)
    {
        TangentMatrix[0] = TangentWS;
        TangentMatrix[1] = BitangentWS;
        TangentMatrix[2] = NormalWS;
    	Out = TransformWorldToTangent(Out, TangentMatrix);	
    }",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] { 0.5f }),
                        new ParameterDescriptor("Strength", TYPE.Float, Usage.In, new float[] { 8.0f }),
                        new ParameterDescriptor("HeightChannel", TYPE.Int, Usage.Static),//TODO: Change this to a dropdown
                        new ParameterDescriptor("OutputSpace", TYPE.Int, Usage.Static),//TODO: Change this to a dropdown
                        new ParameterDescriptor("channelMask", TYPE.Vec4, Usage.Local, new float[] { 1f, 0f, 0f, 0f }),
                        new ParameterDescriptor("normalSample", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("va", TYPE.Vec4, Usage.Local, new float[] { 1f, 0f, 0f, 0f }),
                        new ParameterDescriptor("vb", TYPE.Vec4, Usage.Local, new float[] { 0f, 1f, 0f, 0f }),
                        new ParameterDescriptor("TangentMatrix", TYPE.Mat3, Usage.Local),
                        new ParameterDescriptor("NormalWS", TYPE.Vec3, Usage.Local, REF.WorldSpace_Normal),
                        new ParameterDescriptor("TangentWS", TYPE.Vec3, Usage.Local, REF.WorldSpace_Tangent),
                        new ParameterDescriptor("BitangentWS", TYPE.Vec3, Usage.Local, REF.WorldSpace_Bitangent),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
                    }
                ),
                new(
                    "FourSamples",
@"    //4 samples - only works on square textures
    UV = Texture.GetTransformedUV(UV);
    Offset = pow(Offset, 3) * 0.1;//balance this so it matches the 3 sample version
    if (HeightChannel == 1) channelMask = float4(0,1,0,0);
    if (HeightChannel == 2) channelMask = float4(0,0,1,0);
    if (HeightChannel == 3) channelMask = float4(0,0,0,1);	
    va.x = dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, float2(UV.x - Offset, UV.y)), channelMask);//Bottom
    va.y = dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, float2(UV.x + Offset, UV.y)), channelMask);//Top
    vb.x = dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, float2(UV.x, UV.y + Offset)), channelMask);//Right
    vb.y = dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, float2(UV.x, UV.y - Offset)), channelMask);//Left
    Out.x = va.x - va.y;
    Out.y = vb.x - vb.y;
    Out.z = 1.0f / Strength;
    Out = normalize(Out);//TODO: Check normal direction
    if (OutputSpace==1)
    {
        TangentMatrix[0] = TangentWS;
        TangentMatrix[1] = BitangentWS;
        TangentMatrix[2] = NormalWS;
    	Out = TransformWorldToTangent(Out, TangentMatrix);	
    }",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] { 0.5f }),
                        new ParameterDescriptor("Strength", TYPE.Float, Usage.In, new float[] { 8.0f }),
                        new ParameterDescriptor("HeightChannel", TYPE.Int, Usage.Static),//TODO: Change this to a dropdown
                        new ParameterDescriptor("OutputSpace", TYPE.Int, Usage.Static),//TODO: Change this to a dropdown
                        new ParameterDescriptor("channelMask", TYPE.Vec4, Usage.Local, new float[] { 1f, 0f, 0f, 0f }),
                        new ParameterDescriptor("normalSample", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("va", TYPE.Vec4, Usage.Local, new float[] { 1f, 0f, 0f, 0f }),
                        new ParameterDescriptor("vb", TYPE.Vec4, Usage.Local, new float[] { 0f, 1f, 0f, 0f }),
                        new ParameterDescriptor("TangentMatrix", TYPE.Mat3, Usage.Local),
                        new ParameterDescriptor("NormalWS", TYPE.Vec3, Usage.Local, REF.WorldSpace_Normal),
                        new ParameterDescriptor("TangentWS", TYPE.Vec3, Usage.Local, REF.WorldSpace_Tangent),
                        new ParameterDescriptor("BitangentWS", TYPE.Vec3, Usage.Local, REF.WorldSpace_Bitangent),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
                    }
                ),
                new(
                    "EightSamples",
@"    //8 samples - only works on square textures
    UV = Texture.GetTransformedUV(UV);
    Offset = pow(Offset, 3) * 0.1;//balance this so it matches the 3 sample version
    if (HeightChannel == 1) channelMask = float4(0,1,0,0);
    if (HeightChannel == 2) channelMask = float4(0,0,1,0);
    if (HeightChannel == 3) channelMask = float4(0,0,0,1);	
    va.x = dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, UV - Offset), channelMask);                   // top left
    va.y = dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, float2(UV.x - Offset, UV.y)), channelMask);   	  // left
    va.z = dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, float2(UV.x - Offset, UV.y + Offset)), channelMask);// bottom left
    va.w = dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, float2(UV.x, UV.y - Offset)), channelMask);   	  // top
    vb.x = dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, float2(UV.x, UV.y + Offset)), channelMask);   	  // bottom
    vb.y = dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, float2(UV.x + Offset, UV.y - Offset)), channelMask);// top right
    vb.z = dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, float2(UV.x + Offset, UV.y)), channelMask);   	  // right
    vb.w = dot(SAMPLE_TEXTURE2D(Texture.tex, Sampler.samplerstate, UV + Offset), channelMask);  				  // bottom right
    // Compute dx using Sobel:
    Out.x = -(vb.y + 2*vb.z + vb.w -va.x - 2*va.y - va.z);
    // Compute dy using Sobel:
    Out.y = va.z + 2*vb.x + vb.w -va.x - 2*va.w - vb.y;
    // Build the normalized normal
    Out.z = 1.0f / Strength;
    Out = normalize(Out);//TODO: Check normal direction
    if (OutputSpace==1)
    {
        TangentMatrix[0] = TangentWS;
        TangentMatrix[1] = BitangentWS;
        TangentMatrix[2] = NormalWS;
        Out = TransformWorldToTangent(Out, TangentMatrix);	
    }",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] { 0.5f }),
                        new ParameterDescriptor("Strength", TYPE.Float, Usage.In, new float[] { 8.0f }),
                        new ParameterDescriptor("HeightChannel", TYPE.Int, Usage.Static),//TODO: Change this to a dropdown
                        new ParameterDescriptor("OutputSpace", TYPE.Int, Usage.Static),//TODO: Change this to a dropdown
                        new ParameterDescriptor("channelMask", TYPE.Vec4, Usage.Local, new float[] { 1f, 0f, 0f, 0f }),
                        new ParameterDescriptor("normalSample", TYPE.Float, Usage.Local),
                        new ParameterDescriptor("va", TYPE.Vec4, Usage.Local, new float[] { 1f, 0f, 0f, 0f }),
                        new ParameterDescriptor("vb", TYPE.Vec4, Usage.Local, new float[] { 0f, 1f, 0f, 0f }),
                        new ParameterDescriptor("TangentMatrix", TYPE.Mat3, Usage.Local),
                        new ParameterDescriptor("NormalWS", TYPE.Vec3, Usage.Local, REF.WorldSpace_Normal),
                        new ParameterDescriptor("TangentWS", TYPE.Vec3, Usage.Local, REF.WorldSpace_Tangent),
                        new ParameterDescriptor("BitangentWS", TYPE.Vec3, Usage.Local, REF.WorldSpace_Bitangent),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Creates a normal from multiple samples of a height map.",
            category: "Artistic/Normal",
            synonyms: new string[2] { "convert to normal", "bump map" },
            displayName: "Normal From Texture",
            selectableFunctions: new()
             {
                { "ThreeSamples", "3 Samples" },
                { "FourSamples", "4 Samples" },
                { "EightSamples", "8 Samples" }
             },
            functionSelectorLabel: " ",
            parameters: new ParameterUIDescriptor[8] {
                new ParameterUIDescriptor(
                    name: "Texture",
                    tooltip: "the height map texture asset to sample"
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
                    name: "Offset",
                    tooltip: "amount to offset samples in texels"
                ),
                new ParameterUIDescriptor(
                    name: "Strength",
                    tooltip: "strength multiplier"
                ),
                new ParameterUIDescriptor(
                    name: "HeightChannel",
                    tooltip: "select the texture channel the height data is in",
                    displayName: "Height Channel"
                ),
                new ParameterUIDescriptor(
                    name: "OutputSpace",
                    tooltip: "the space of the resulting normal",
                    displayName: "Output Space"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "normal created from the input height texture"
                )
            }
        );
    }
}
