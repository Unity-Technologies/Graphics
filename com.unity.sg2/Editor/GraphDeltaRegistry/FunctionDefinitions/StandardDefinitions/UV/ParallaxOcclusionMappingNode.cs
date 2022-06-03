using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ParallaxOcclusionMappingNode : IStandardNode
    {
        public static string Name = "ParallaxOcclusionMapping";
        public static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "ParallaxOcclusionMappingRed",
                    //TODO: There's a lot of code that needs to exist outside this main body function in order for this to work.
@"  //TODO: Need to know how to handle float vs half version of GetDisplacementObjectScale
    ViewDir = ViewDirTS * GetDisplacementObjectScale_float().xzy;
    MaxHeight = Amplitude * 0.01; // cm in the interface
    MaxHeight *= 2.0 / ( abs(Tiling.x) + abs(Tiling.y) ); // reduce height based on the tiling values
    // Transform the view vector into the UV space.
    ViewDirUV.xy = ViewDir.xy * (MaxHeight * Tiling / PrimitiveSize);
    ViewDirUV.z = ViewDir.z;
    ViewDirUV = normalize(ViewDirUV); // TODO: skip normalize
    PerPixelHeightDisplacementParam POM;
    TransformedUVS = UVs * Tiling + Offset;
    POM.uv = Heightmap.GetTransformedUV(TransformedUVS);
    ParallaxUVs = POM.uv + ParallaxOcclusionMapping(LOD, LODThreshold, max(min(Steps, 256), 1), ViewDirUV, POM, OutHeight, Heightmap.tex, HeightmapSampler.samplerstate));
    PixelDepthOffset = (MaxHeight - OutHeight * MaxHeight) / max(ViewDir.z, 0.0001);",
                    new ParameterDescriptor("Heightmap", TYPE.Texture2D, Usage.In),
                    new ParameterDescriptor("HeightmapSampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("Amplitude", TYPE.Float, Usage.In, new float[] {1}),
                    new ParameterDescriptor("Steps", TYPE.Float, Usage.In, new float[] {5}),
                    new ParameterDescriptor("UVs", TYPE.Vec2, Usage.In, REF.UV0),
                    new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
                    new ParameterDescriptor("LODThreshold", TYPE.Float, Usage.In),
                    new ParameterDescriptor("Tiling", TYPE.Vec2, Usage.In, new float[] {1, 1}),
                    new ParameterDescriptor("Offset", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("PrimitiveSize", TYPE.Vec2, Usage.In, new float[] {1, 1}),
                    new ParameterDescriptor("PixelDepthOffset", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("ParallaxUVs", TYPE.Vec2, Usage.Out),
                    new ParameterDescriptor("channel", TYPE.Int, Usage.Local, new float[] {0}),
                    new ParameterDescriptor("ViewDir", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("ViewDirTS", TYPE.Vec3, Usage.Local, REF.TangentSpace_ViewDirection),
                    new ParameterDescriptor("MaxHeight", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("TransformedUVS", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("ViewDirUV", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("OutHeight", TYPE.Float, Usage.Local)
/*
// Required struct and function for the ParallaxOcclusionMapping function:
struct PerPixelHeightDisplacementParam
{
    float2 uv;
};
        
float3 GetDisplacementObjectScale_float()
{
    float3 objectScale = float3(1.0, 1.0, 1.0);
    float4x4 worldTransform = GetWorldToObjectMatrix();
    objectScale.x = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
    objectScale.z = length(float3(worldTransform._m20, worldTransform._m21, worldTransform._m22));
    return objectScale;
}
// Required struct and function for the ParallaxOcclusionMapping function:
float ComputePerPixelHeightDisplacement_ParallaxOcclusionMapping(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param, TEXTURE2D_PARAM(heightTexture, heightSampler))
{
    return SAMPLE_TEXTURE2D_LOD(heightTexture, heightSampler, param.uv + texOffsetCurrent, lod)[0];
}
#define ComputePerPixelHeightDisplacement ComputePerPixelHeightDisplacement_ParallaxOcclusionMapping
#define POM_NAME_ID ParallaxOcclusionMapping_float
#define POM_USER_DATA_PARAMETERS , TEXTURE2D_PARAM(heightTexture, samplerState)
#define POM_USER_DATA_ARGUMENTS , TEXTURE2D_ARGS(heightTexture, samplerState)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/PerPixelDisplacement.hlsl"
#undef ComputePerPixelHeightDisplacement
#undef POM_NAME_ID
#undef POM_USER_DATA_PARAMETERS
#undef POM_USER_DATA_ARGUMENTS
*/
                ),
                new(
                    1,
                    "ParallaxOcclusionMappingGreen",
@"  //TODO: Need to know how to handle float vs half version of GetDisplacementObjectScale
    ViewDir = ViewDirTS * GetDisplacementObjectScale_float().xzy;
    MaxHeight = Amplitude * 0.01; // cm in the interface
    MaxHeight *= 2.0 / ( abs(Tiling.x) + abs(Tiling.y) ); // reduce height based on the tiling values
    // Transform the view vector into the UV space.
    ViewDirUV.xy = ViewDir.xy * (MaxHeight * Tiling / PrimitiveSize);
    ViewDirUV.z = ViewDir.z;
    ViewDirUV = normalize(ViewDirUV); // TODO: skip normalize
    PerPixelHeightDisplacementParam POM;
    TransformedUVS = UVs * Tiling + Offset;
    POM.uv = Heightmap.GetTransformedUV(TransformedUVS);
    ParallaxUVs = POM.uv + ParallaxOcclusionMapping(LOD, LODThreshold, max(min(Steps, 256), 1), ViewDirUV, POM, OutHeight, Heightmap.tex, HeightmapSampler.samplerstate));
    PixelDepthOffset = (MaxHeight - OutHeight * MaxHeight) / max(ViewDir.z, 0.0001);",
                    new ParameterDescriptor("Heightmap", TYPE.Texture2D, Usage.In),
                    new ParameterDescriptor("HeightmapSampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("Amplitude", TYPE.Float, Usage.In, new float[] {1}),
                    new ParameterDescriptor("Steps", TYPE.Float, Usage.In, new float[] {5}),
                    new ParameterDescriptor("UVs", TYPE.Vec2, Usage.In, REF.UV0),
                    new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
                    new ParameterDescriptor("LODThreshold", TYPE.Float, Usage.In),
                    new ParameterDescriptor("Tiling", TYPE.Vec2, Usage.In, new float[] {1, 1}),
                    new ParameterDescriptor("Offset", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("PrimitiveSize", TYPE.Vec2, Usage.In, new float[] {1, 1}),
                    new ParameterDescriptor("PixelDepthOffset", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("ParallaxUVs", TYPE.Vec2, Usage.Out),
                    new ParameterDescriptor("channel", TYPE.Int, Usage.Local, new float[] {1}),
                    new ParameterDescriptor("ViewDir", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("ViewDirTS", TYPE.Vec3, Usage.Local, REF.TangentSpace_ViewDirection),
                    new ParameterDescriptor("MaxHeight", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("TransformedUVS", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("ViewDirUV", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("OutHeight", TYPE.Float, Usage.Local)
/*
// Required struct and function for the ParallaxOcclusionMapping function:
struct PerPixelHeightDisplacementParam
{
    float2 uv;
};
        
float3 GetDisplacementObjectScale_float()
{
    float3 objectScale = float3(1.0, 1.0, 1.0);
    float4x4 worldTransform = GetWorldToObjectMatrix();
    objectScale.x = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
    objectScale.z = length(float3(worldTransform._m20, worldTransform._m21, worldTransform._m22));
    return objectScale;
}
// Required struct and function for the ParallaxOcclusionMapping function:
float ComputePerPixelHeightDisplacement_ParallaxOcclusionMapping(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param, TEXTURE2D_PARAM(heightTexture, heightSampler))
{
    return SAMPLE_TEXTURE2D_LOD(heightTexture, heightSampler, param.uv + texOffsetCurrent, lod)[1];
}
#define ComputePerPixelHeightDisplacement ComputePerPixelHeightDisplacement_ParallaxOcclusionMapping
#define POM_NAME_ID ParallaxOcclusionMapping_float
#define POM_USER_DATA_PARAMETERS , TEXTURE2D_PARAM(heightTexture, samplerState)
#define POM_USER_DATA_ARGUMENTS , TEXTURE2D_ARGS(heightTexture, samplerState)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/PerPixelDisplacement.hlsl"
#undef ComputePerPixelHeightDisplacement
#undef POM_NAME_ID
#undef POM_USER_DATA_PARAMETERS
#undef POM_USER_DATA_ARGUMENTS
*/
                ),
                new(
                    1,
                    "ParallaxOcclusionMappingBlue",
@"  //TODO: Need to know how to handle float vs half version of GetDisplacementObjectScale
    ViewDir = ViewDirTS * GetDisplacementObjectScale_float().xzy;
    MaxHeight = Amplitude * 0.01; // cm in the interface
    MaxHeight *= 2.0 / ( abs(Tiling.x) + abs(Tiling.y) ); // reduce height based on the tiling values
    // Transform the view vector into the UV space.
    ViewDirUV.xy = ViewDir.xy * (MaxHeight * Tiling / PrimitiveSize);
    ViewDirUV.z = ViewDir.z;
    ViewDirUV = normalize(ViewDirUV); // TODO: skip normalize
    PerPixelHeightDisplacementParam POM;
    TransformedUVS = UVs * Tiling + Offset;
    POM.uv = Heightmap.GetTransformedUV(TransformedUVS);
    ParallaxUVs = POM.uv + ParallaxOcclusionMapping(LOD, LODThreshold, max(min(Steps, 256), 1), ViewDirUV, POM, OutHeight, Heightmap.tex, HeightmapSampler.samplerstate));
    PixelDepthOffset = (MaxHeight - OutHeight * MaxHeight) / max(ViewDir.z, 0.0001);",
                    new ParameterDescriptor("Heightmap", TYPE.Texture2D, Usage.In),
                    new ParameterDescriptor("HeightmapSampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("Amplitude", TYPE.Float, Usage.In, new float[] {1}),
                    new ParameterDescriptor("Steps", TYPE.Float, Usage.In, new float[] {5}),
                    new ParameterDescriptor("UVs", TYPE.Vec2, Usage.In, REF.UV0),
                    new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
                    new ParameterDescriptor("LODThreshold", TYPE.Float, Usage.In),
                    new ParameterDescriptor("Tiling", TYPE.Vec2, Usage.In, new float[] {1, 1}),
                    new ParameterDescriptor("Offset", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("PrimitiveSize", TYPE.Vec2, Usage.In, new float[] {1, 1}),
                    new ParameterDescriptor("PixelDepthOffset", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("ParallaxUVs", TYPE.Vec2, Usage.Out),
                    new ParameterDescriptor("channel", TYPE.Int, Usage.Local, new float[] {2}),
                    new ParameterDescriptor("ViewDir", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("ViewDirTS", TYPE.Vec3, Usage.Local, REF.TangentSpace_ViewDirection),
                    new ParameterDescriptor("MaxHeight", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("TransformedUVS", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("ViewDirUV", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("OutHeight", TYPE.Float, Usage.Local)
/*
// Required struct and function for the ParallaxOcclusionMapping function:
struct PerPixelHeightDisplacementParam
{
    float2 uv;
};
        
float3 GetDisplacementObjectScale_float()
{
    float3 objectScale = float3(1.0, 1.0, 1.0);
    float4x4 worldTransform = GetWorldToObjectMatrix();
    objectScale.x = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
    objectScale.z = length(float3(worldTransform._m20, worldTransform._m21, worldTransform._m22));
    return objectScale;
}
// Required struct and function for the ParallaxOcclusionMapping function:
float ComputePerPixelHeightDisplacement_ParallaxOcclusionMapping(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param, TEXTURE2D_PARAM(heightTexture, heightSampler))
{
    return SAMPLE_TEXTURE2D_LOD(heightTexture, heightSampler, param.uv + texOffsetCurrent, lod)[2];
}
#define ComputePerPixelHeightDisplacement ComputePerPixelHeightDisplacement_ParallaxOcclusionMapping
#define POM_NAME_ID ParallaxOcclusionMapping_float
#define POM_USER_DATA_PARAMETERS , TEXTURE2D_PARAM(heightTexture, samplerState)
#define POM_USER_DATA_ARGUMENTS , TEXTURE2D_ARGS(heightTexture, samplerState)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/PerPixelDisplacement.hlsl"
#undef ComputePerPixelHeightDisplacement
#undef POM_NAME_ID
#undef POM_USER_DATA_PARAMETERS
#undef POM_USER_DATA_ARGUMENTS
*/
                ),
                new(
                    1,
                    "ParallaxOcclusionMappingAlpha",
@"  //TODO: Need to know how to handle float vs half version of GetDisplacementObjectScale
    ViewDir = ViewDirTS * GetDisplacementObjectScale_float().xzy;
    MaxHeight = Amplitude * 0.01; // cm in the interface
    MaxHeight *= 2.0 / ( abs(Tiling.x) + abs(Tiling.y) ); // reduce height based on the tiling values
    // Transform the view vector into the UV space.
    ViewDirUV.xy = ViewDir.xy * (MaxHeight * Tiling / PrimitiveSize);
    ViewDirUV.z = ViewDir.z;
    ViewDirUV = normalize(ViewDirUV); // TODO: skip normalize
    PerPixelHeightDisplacementParam POM;
    TransformedUVS = UVs * Tiling + Offset;
    POM.uv = Heightmap.GetTransformedUV(TransformedUVS);
    ParallaxUVs = POM.uv + ParallaxOcclusionMapping(LOD, LODThreshold, max(min(Steps, 256), 1), ViewDirUV, POM, OutHeight, Heightmap.tex, HeightmapSampler.samplerstate));
    PixelDepthOffset = (MaxHeight - OutHeight * MaxHeight) / max(ViewDir.z, 0.0001);",
                    new ParameterDescriptor("Heightmap", TYPE.Texture2D, Usage.In),
                    new ParameterDescriptor("HeightmapSampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("Amplitude", TYPE.Float, Usage.In, new float[] {1}),
                    new ParameterDescriptor("Steps", TYPE.Float, Usage.In, new float[] {5}),
                    new ParameterDescriptor("UVs", TYPE.Vec2, Usage.In, REF.UV0),
                    new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
                    new ParameterDescriptor("LODThreshold", TYPE.Float, Usage.In),
                    new ParameterDescriptor("Tiling", TYPE.Vec2, Usage.In, new float[] {1, 1}),
                    new ParameterDescriptor("Offset", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("PrimitiveSize", TYPE.Vec2, Usage.In, new float[] {1, 1}),
                    new ParameterDescriptor("PixelDepthOffset", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("ParallaxUVs", TYPE.Vec2, Usage.Out),
                    new ParameterDescriptor("channel", TYPE.Int, Usage.Local, new float[] {3}),
                    new ParameterDescriptor("ViewDir", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("ViewDirTS", TYPE.Vec3, Usage.Local, REF.TangentSpace_ViewDirection),
                    new ParameterDescriptor("MaxHeight", TYPE.Float, Usage.Local),
                    new ParameterDescriptor("TransformedUVS", TYPE.Vec2, Usage.Local),
                    new ParameterDescriptor("ViewDirUV", TYPE.Vec3, Usage.Local),
                    new ParameterDescriptor("OutHeight", TYPE.Float, Usage.Local)
/*
// Required struct and function for the ParallaxOcclusionMapping function:
struct PerPixelHeightDisplacementParam
{
    float2 uv;
};
        
float3 GetDisplacementObjectScale_float()
{
    float3 objectScale = float3(1.0, 1.0, 1.0);
    float4x4 worldTransform = GetWorldToObjectMatrix();
    objectScale.x = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
    objectScale.z = length(float3(worldTransform._m20, worldTransform._m21, worldTransform._m22));
    return objectScale;
}
// Required struct and function for the ParallaxOcclusionMapping function:
float ComputePerPixelHeightDisplacement_ParallaxOcclusionMapping(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param, TEXTURE2D_PARAM(heightTexture, heightSampler))
{
    return SAMPLE_TEXTURE2D_LOD(heightTexture, heightSampler, param.uv + texOffsetCurrent, lod)[3];
}
#define ComputePerPixelHeightDisplacement ComputePerPixelHeightDisplacement_ParallaxOcclusionMapping
#define POM_NAME_ID ParallaxOcclusionMapping_float
#define POM_USER_DATA_PARAMETERS , TEXTURE2D_PARAM(heightTexture, samplerState)
#define POM_USER_DATA_ARGUMENTS , TEXTURE2D_ARGS(heightTexture, samplerState)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/PerPixelDisplacement.hlsl"
#undef ComputePerPixelHeightDisplacement
#undef POM_NAME_ID
#undef POM_USER_DATA_PARAMETERS
#undef POM_USER_DATA_ARGUMENTS
*/
                )
            }
        );
        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Parallax Occlusion Mapping",
            tooltip: "Creates a parallax effect that displaces a material's UVs and depth to create the illusion of depth.",
            categories: new string[1] { "UV" },
            synonyms: new string[1] { "pom" },
            hasPreview: false,
            selectableFunctions: new()
            {
                { "ParallaxOcclusionMappingRed", "Red" },
                { "ParallaxOcclusionMappingGreen", "Green" },
                { "ParallaxOcclusionMappingBlue", "Blue" },
                { "ParallaxOcclusionMappingAlpha", "Alpha" }
            },
            parameters: new ParameterUIDescriptor[11] {
                new ParameterUIDescriptor(
                    name: "Heightmap",
                    tooltip: "the texture that specifies the depth of the displacement"
                ),
                new ParameterUIDescriptor(
                    name: "HeightmapSampler",
                    tooltip: "the sampler to sample Heightmap with"
                ),
                new ParameterUIDescriptor(
                    name: "Amplitude",
                    tooltip: "a multiplier to apply to the height of the Heightmap (in centimeters)"
                ),
               new ParameterUIDescriptor(
                    name: "Steps",
                    tooltip: "The number of steps that the linear search of the algorithm performs."
                ),
                new ParameterUIDescriptor(
                    name: "UVs",
                    tooltip: "The UVs that the sampler uses to sample the Texture.",
                    options: REF.OptionList.UVs
                ),
               new ParameterUIDescriptor(
                    name: "LOD",
                    tooltip: "The level of detail to use to sample the Heightmap. This value should always be positive."
                ),
               new ParameterUIDescriptor(
                    name: "LODThreshold",
                    tooltip: "The Heightmap mip level where the Parallax Occlusion Mapping effect begins to fade out."
                ),
               new ParameterUIDescriptor(
                    name: "Tiling",
                    tooltip: "The tiling to apply to the input UVs."
                ),
               new ParameterUIDescriptor(
                    name: "Offset",
                    tooltip: "The offset to apply to the input UVs."
                ),
                new ParameterUIDescriptor(
                    name: "PixelDepthOffset",
                    tooltip: "The offset to apply to the depth buffer to produce the illusion of depth."
                ),
                new ParameterUIDescriptor(
                    name: "ParallaxUVs",
                    tooltip: "the UVs after adding the parallax offset"
                )
            }
        );
    }
}
