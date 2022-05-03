using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class NormalFromTextureNode : IStandardNode
    {
        static string Name = "NormalFromTexture";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "3Samples",
@"
{
    //3 sample version - only works on square textures with height in the red channel
    //Offset = pow(Offset, 3) * 0.1;
    //if (HeightChannel == 1) channeMask = float4(0,1,0,0);
    //if (HeightChannel == 2) channeMask = float4(0,0,1,0);
    //if (HeightChannel == 3) channeMask = float4(0,0,0,1);	
    //normalSample = dot(SAMPLE_TEXTURE2D(Texture, Sampler, UV), channeMask);
    //va.z = (dot(SAMPLE_TEXTURE2D(Texture, Sampler, (UV.x + Offset, UV.y)), channelMask) - normalSample) * Strength;
    //vb.z = (dot(SAMPLE_TEXTURE2D(Texture, Sampler, (UV.x, UV.y + Offset)), channelMask) - normalSample) * Strength;
    //Out = normalize(cross(va.xyz, vb.xyz));
    //if (OutputSpace==1)
    //{
    //    TangentMatrix[0] = IN.WorldSpaceTangent;
    //    TangentMatrix[1] = IN.WorldSpaceBiTangent;
    //    TangentMatrix[2] = IN.WorldSpaceNormal;
    //	Out = TransformWorldToTangent(Out, TangentMatrix);	
    //}
    Out = float3(0,0,1);
}",
                    new ParameterDescriptor("Texture", TYPE.Vec4, Usage.In),//fix type
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),//add default UVs
                    new ParameterDescriptor("Sampler", TYPE.Vec2, Usage.In),//fix type
                    new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] { 0.5f }),
                    new ParameterDescriptor("Strength", TYPE.Float, Usage.In, new float[] { 8.0f }),
                    new ParameterDescriptor("HeightChannel", TYPE.Int, Usage.Static),//TODO: Change this to a dropdown
                    new ParameterDescriptor("OutputSpace", TYPE.Int, Usage.Static),//TODO: Change this to a dropdown
                    new ParameterDescriptor("channelMask", TYPE.Vec4, Usage.Local, new float[] { 1f, 0f, 0f, 0f }),
                    new ParameterDescriptor("normalSample", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("va", TYPE.Vec4, Usage.Local, new float[] { 1f, 0f, 0f, 0f }),
                    new ParameterDescriptor("vb", TYPE.Vec4, Usage.Local, new float[] { 0f, 1f, 0f, 0f }),
                    new ParameterDescriptor("TangentMatrix", TYPE.Mat3, Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
                ),
                new(
                    1,
                    "4Samples",
@"
{
    //4 samples - only works on square textures with height in the red channel
    //Offset = pow(Offset, 3) * 0.1;//balance this so it matches the 3 sample version
    //if (HeightChannel == 1) channeMask = float4(0,1,0,0);
    //if (HeightChannel == 2) channeMask = float4(0,0,1,0);
    //if (HeightChannel == 3) channeMask = float4(0,0,0,1);	
    //va.x = dot(SAMPLE_TEXTURE2D(Texture, Sampler, (UV.x - Offset, UV.y)), channelMask);//Bottom
    //va.y = dot(SAMPLE_TEXTURE2D(Texture, Sampler, (UV.x + Offset, UV.y)), channelMask);//Top
    //vb.x = dot(SAMPLE_TEXTURE2D(Texture, Sampler, (UV.x, UV.y + Offset)), channelMask);//Right
    //vb.y = dot(SAMPLE_TEXTURE2D(Texture, Sampler, (UV.x, UV.y - Offset)), channelMask);//Left
    //Out.x = va.x - va.y;
    //Out.y = vb.x - vb.y;
    //Out.z = 1.0f / Strength;
    //Out = normalize(Out);//TODO: Check normal direction
    //if (OutputSpace==1)
    //{
    //	TangentMatrix[0] = IN.WorldSpaceTangent;
    //    TangentMatrix[1] = IN.WorldSpaceBiTangent;
    //    TangentMatrix[2] = IN.WorldSpaceNormal;
    //	Out = TransformWorldToTangent(Out, TangentMatrix);	
    //}
    Out = float3(0,0,1);
}",
                    new ParameterDescriptor("Texture", TYPE.Vec4, Usage.In),//fix type
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),//add default UVs
                    new ParameterDescriptor("Sampler", TYPE.Vec2, Usage.In),//fix type
                    new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] { 0.5f }),
                    new ParameterDescriptor("Strength", TYPE.Float, Usage.In, new float[] { 8.0f }),
                    new ParameterDescriptor("HeightChannel", TYPE.Int, Usage.Static),//TODO: Change this to a dropdown
                    new ParameterDescriptor("OutputSpace", TYPE.Int, Usage.Static),//TODO: Change this to a dropdown
                    new ParameterDescriptor("channelMask", TYPE.Vec4, Usage.Local, new float[] { 1f, 0f, 0f, 0f }),
                    new ParameterDescriptor("normalSample", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("va", TYPE.Vec4, Usage.Local, new float[] { 1f, 0f, 0f, 0f }),
                    new ParameterDescriptor("vb", TYPE.Vec4, Usage.Local, new float[] { 0f, 1f, 0f, 0f }),
                    new ParameterDescriptor("TangentMatrix", TYPE.Mat3, Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
                ),
                new(
                    1,
                    "8Samples",
@"
{
    //8 samples - only works on square textures with height in the red channel
    //Offset = pow(Offset, 3) * 0.1;//balance this so it matches the 3 sample version
    //if (HeightChannel == 1) channeMask = float4(0,1,0,0);
    //if (HeightChannel == 2) channeMask = float4(0,0,1,0);
    //if (HeightChannel == 3) channeMask = float4(0,0,0,1);	
    //va.x = dot(SAMPLE_TEXTURE2D(Texture, Sampler, UV - Offset), channelMask);                   // top left
    //va.y = dot(SAMPLE_TEXTURE2D(Texture, Sampler, (UV.x - Offset, UV.y)), channelMask);   	  // left
    //va.z = dot(SAMPLE_TEXTURE2D(Texture, Sampler, (UV.x - Offset, UV.y + Offset)), channelMask);// bottom left
    //va.w = dot(SAMPLE_TEXTURE2D(Texture, Sampler, (UV.x, UV.y - Offset)), channelMask);   	  // top
    //vb.x = dot(SAMPLE_TEXTURE2D(Texture, Sampler, (UV.x, UV.y + Offset)), channelMask);   	  // bottom
    //vb.y = dot(SAMPLE_TEXTURE2D(Texture, Sampler, (UV.x + Offset, UV.y - Offset)), channelMask);// top right
    //vb.z = dot(SAMPLE_TEXTURE2D(Texture, Sampler, (UV.x + Offset, UV.y)), channelMask);   	  // right
    //vb.w = dot(SAMPLE_TEXTURE2D(Texture, Sampler, UV + Offset), channelMask);  				  // bottom right
    // Compute dx using Sobel:
    //Out.x = -(vb.y + 2*vb.z + vb.w -va.x - 2*va.y - va.z);
    // Compute dy using Sobel:
    //Out.y = va.z + 2*vb.x + vb.w -va.x - 2*va.w - vb.y;
    // Build the normalized normal
    //Out.z = 1.0f / Strength;
    //Out = normalize(Out);//TODO: Check normal direction
    //if (OutputSpace==1)
    //{
    //    TangentMatrix[0] = IN.WorldSpaceTangent;
    //    TangentMatrix[1] = IN.WorldSpaceBiTangent;
    //    TangentMatrix[2] = IN.WorldSpaceNormal;
    //    Out = TransformWorldToTangent(Out, TangentMatrix);	
    //}
    Out = float3(0,0,1);
}",
                    new ParameterDescriptor("Texture", TYPE.Vec4, Usage.In),//fix type
                    new ParameterDescriptor("UV", TYPE.Vec2, Usage.In),//add default UVs
                    new ParameterDescriptor("Sampler", TYPE.Vec2, Usage.In),//fix type
                    new ParameterDescriptor("Offset", TYPE.Float, Usage.In, new float[] { 0.5f }),
                    new ParameterDescriptor("Strength", TYPE.Float, Usage.In, new float[] { 8.0f }),
                    new ParameterDescriptor("HeightChannel", TYPE.Int, Usage.Static),//TODO: Change this to a dropdown
                    new ParameterDescriptor("OutputSpace", TYPE.Int, Usage.Static),//TODO: Change this to a dropdown
                    new ParameterDescriptor("channelMask", TYPE.Vec4, Usage.Local, new float[] { 1f, 0f, 0f, 0f }),
                    new ParameterDescriptor("normalSample", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("va", TYPE.Vec4, Usage.Local, new float[] { 1f, 0f, 0f, 0f }),
                    new ParameterDescriptor("vb", TYPE.Vec4, Usage.Local, new float[] { 0f, 1f, 0f, 0f }),
                    new ParameterDescriptor("TangentMatrix", TYPE.Mat3, Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "creates a normal from multiple samples of a height map",
            categories: new string[2] { "Artistic", "Normal" },
            synonyms: new string[2] { "convert to normal", "bump map" },
            displayName: "Normal From Texture",
            selectableFunctions: new()
             {
                { "3Samples", "3 Samples" },
                { "4Samples", "4 Samples" },
                { "8Samples", "8 Samples" }
             },
            parameters: new ParameterUIDescriptor[6] {
                new ParameterUIDescriptor(
                    name: "Texture",
                    tooltip: "the height map texture asset to sample"
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
                    name: "Offset",
                    tooltip: "amount to offset samples in texels"
                ),
                new ParameterUIDescriptor(
                    name: "Strength",
                    tooltip: "strength multiplier"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "normal created from the input height texture"
                )
            }
        );
    }
}
