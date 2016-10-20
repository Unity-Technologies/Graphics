// No guard header!

#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

//-------------------------------------------------------------------------------------
// Include
//-------------------------------------------------------------------------------------

#define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
#include "../../Lighting/Lighting.hlsl" // This include Material.hlsl
#include "../../ShaderVariables.hlsl"
#include "../../Debug/DebugViewMaterial.hlsl"

//-------------------------------------------------------------------------------------
// Attribute/Varying
//-------------------------------------------------------------------------------------

// Forward
struct Attributes
{
    float3 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv0          : TEXCOORD0;
    float2 uv1		    : TEXCOORD1;
#if SHADERPASS == SHADERPASS_LIGHT_TRANSPORT
    float2 uv2		    : TEXCOORD2;
#endif    
    float4 tangentOS    : TANGENT;  // Always present as we require it also in case of anisotropic lighting

    // UNITY_INSTANCE_ID
};

struct Varyings
{
    float4 positionHS;
    float3 positionWS;
    float2 texCoord0;
    float2 texCoord1;
#if SHADERPASS == SHADERPASS_LIGHT_TRANSPORT
    float2 texCoord2;
#endif
    float3 tangentToWorld[3];

#ifdef SHADER_STAGE_FRAGMENT
    #if defined(_DOUBLESIDED_LIGHTING_FLIP) || defined(_DOUBLESIDED_LIGHTING_MIRROR)
    FRONT_FACE_TYPE cullFace;
    #endif
#endif
};

void GetVaryingsDataDebug(uint paramId, Varyings input, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
    case DEBUGVIEW_VARYING_DEPTH:
        // TODO: provide a customize parameter (like a slider)
        float linearDepth = frac(LinearEyeDepth(input.positionHS.z, _ZBufferParams) * 0.1);
        result = linearDepth.xxx;
        break;
    case DEBUGVIEW_VARYING_TEXCOORD0:
        result = float3(input.texCoord0 * 0.5 + 0.5, 0.0);
        break;
    case DEBUGVIEW_VARYING_TEXCOORD1:
        result = float3(input.texCoord1 * 0.5 + 0.5, 0.0);
        break;
#if SHADERPASS == SHADERPASS_LIGHT_TRANSPORT
    case DEBUGVIEW_VARYING_TEXCOORD2:
        result = float3(input.texCoord2 * 0.5 + 0.5, 0.0);
        break;
#endif
    case DEBUGVIEW_VARYING_VERTEXTANGENTWS:
        result = input.tangentToWorld[0].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEW_VARYING_VERTEXBITANGENTWS:
        result = input.tangentToWorld[1].xyz * 0.5 + 0.5;
        break;
    case DEBUGVIEW_VARYING_VERTEXNORMALWS:
        result = input.tangentToWorld[2].xyz * 0.5 + 0.5;
        break;
    }
}

struct PackedVaryings
{
    float4 positionHS : SV_Position;
#if SHADERPASS == SHADERPASS_LIGHT_TANSPORT
    float4 interpolators[5] : TEXCOORD0;
#else
    float4 interpolators[4] : TEXCOORD0;
#endif

#ifdef SHADER_STAGE_FRAGMENT
    #if defined(_DOUBLESIDED_LIGHTING_FLIP) || defined(_DOUBLESIDED_LIGHTING_MIRROR)
    FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMATIC;
    #endif
#endif
};

// Function to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
PackedVaryings PackVaryings(Varyings input)
{
    PackedVaryings output;
    output.positionHS = input.positionHS;
    output.interpolators[0].xyz = input.positionWS.xyz;
    output.interpolators[1].xyz = input.tangentToWorld[0];
    output.interpolators[2].xyz = input.tangentToWorld[1];
    output.interpolators[3].xyz = input.tangentToWorld[2];

    output.interpolators[0].w = input.texCoord0.x;
    output.interpolators[1].w = input.texCoord0.y;
    output.interpolators[2].w = input.texCoord1.x;
    output.interpolators[3].w = input.texCoord1.y;

#if SHADERPASS == SHADERPASS_LIGHT_TRANSPORT
    output.interpolators[4] = float4(input.texCoord2.xy, 0.0, 0.0);
#endif

    return output;
}

Varyings UnpackVaryings(PackedVaryings input)
{
    Varyings output;
    output.positionHS = input.positionHS;
    output.positionWS.xyz = input.interpolators[0].xyz;
    output.tangentToWorld[0] = input.interpolators[1].xyz;
    output.tangentToWorld[1] = input.interpolators[2].xyz;
    output.tangentToWorld[2] = input.interpolators[3].xyz;

    output.texCoord0.xy = float2(input.interpolators[0].w, input.interpolators[1].w);
    output.texCoord1.xy = float2(input.interpolators[2].w, input.interpolators[3].w);

#if SHADERPASS == SHADERPASS_LIGHT_TRANSPORT
    output.texCoord2 = input.interpolators[4].xy;
#endif

#ifdef SHADER_STAGE_FRAGMENT
    #if defined(_DOUBLESIDED_LIGHTING_FLIP) || defined(_DOUBLESIDED_LIGHTING_MIRROR)
    output.cullFace = input.cullFace;
    #endif
#endif

    return output;
}

// TODO: Here we will also have all the vertex deformation (GPU skinning, vertex animation, morph target...) or we will need to generate a compute shaders instead (better! but require work to deal with unpacking like fp16)
PackedVaryings VertDefault(Attributes input)
{
    Varyings output;

    output.positionWS = TransformObjectToWorld(input.positionOS);
    // TODO deal with camera center rendering and instancing (This is the reason why we always perform tow steps transform to clip space + instancing matrix)
    output.positionHS = TransformWorldToHClip(output.positionWS);

    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

    output.texCoord0 = input.uv0;
    output.texCoord1 = input.uv1;

#if SHADERPASS == SHADERPASS_LIGHT_TRANSPORT
    output.texCoord2 = input.uv2;
#endif

    float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);

    float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    output.tangentToWorld[0] = tangentToWorld[0];
    output.tangentToWorld[1] = tangentToWorld[1];
    output.tangentToWorld[2] = tangentToWorld[2];

    return PackVaryings(output);
}

//-------------------------------------------------------------------------------------
// Fill SurfaceData/Lighting data function
//-------------------------------------------------------------------------------------

#if SHADER_STAGE_FRAGMENT

void GetSurfaceAndBuiltinData(float3 V, Varyings input, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef _HEIGHTMAP
    #ifdef _HEIGHTMAP_AS_DISPLACEMENT
    // TODO
    #else
    float height = UNITY_SAMPLE_TEX2D(_HeightMap, input.texCoord0).r * _HeightScale + _HeightBias;
    // Transform view vector in tangent space
    TransformWorldToTangent(V, input.tangentToWorld);
    float2 offset = ParallaxOffset(viewDirTS, height);
    input.texCoord0 += offset;
    input.texCoord1 += offset;
    #endif
#endif

    surfaceData.baseColor = UNITY_SAMPLE_TEX2D(_BaseColorMap, input.texCoord0).rgb * _BaseColor.rgb;
#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    float alpha = _BaseColor.a;
#else
    float alpha = UNITY_SAMPLE_TEX2D(_BaseColorMap, input.texCoord0).a * _BaseColor.a;
#endif

#ifdef _ALPHATEST_ON
    clip(alpha - _AlphaCutoff);
#endif

#ifdef _SPECULAROCCLUSIONMAP
    // TODO: Do something. For now just take alpha channel
    surfaceData.specularOcclusion = UNITY_SAMPLE_TEX2D(_SpecularOcclusionMap, input.texCoord0).a;
#else
    // Horizon Occlusion for Normal Mapped Reflections: http://marmosetco.tumblr.com/post/81245981087
    //surfaceData.specularOcclusion = saturate(1.0 + horizonFade * dot(r, input.tangentToWorld[2].xyz);
    // smooth it
    //surfaceData.specularOcclusion *= surfaceData.specularOcclusion;
    surfaceData.specularOcclusion = 1.0;
#endif

    // TODO: think about using BC5
    float3 vertexNormalWS = input.tangentToWorld[2].xyz;

#ifdef _NORMALMAP
    #ifdef _NORMALMAP_TANGENT_SPACE
    float3 normalTS = UnpackNormalAG(UNITY_SAMPLE_TEX2D(_NormalMap, input.texCoord0));
    surfaceData.normalWS = TransformTangentToWorld(normalTS, input.tangentToWorld);
    #else // Object space (TODO: We need to apply the world rotation here! - Require to pass world transform)
    surfaceData.normalWS = UNITY_SAMPLE_TEX2D(_NormalMap, input.texCoord0).rgb;
    #endif
#else
    surfaceData.normalWS = vertexNormalWS;
#endif

#if defined(_DOUBLESIDED_LIGHTING_FLIP) || defined(_DOUBLESIDED_LIGHTING_MIRROR)
    #ifdef _DOUBLESIDED_LIGHTING_FLIP
    float3 oppositeNormalWS = -surfaceData.normalWS;
    #else
    // Mirror the normal with the plane define by vertex normal
    float3 oppositeNormalWS = reflect(surfaceData.normalWS, vertexNormalWS);
#endif
    // TODO : Test if GetOdddNegativeScale() is necessary here in case of normal map, as GetOdddNegativeScale is take into account in CreateTangentToWorld();
    surfaceData.normalWS = IS_FRONT_VFACE(input.cullFace, GetOdddNegativeScale() >= 0.0 ? surfaceData.normalWS : oppositeNormalWS, -GetOdddNegativeScale() >= 0.0 ? surfaceData.normalWS : oppositeNormalWS);
#endif

#ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    surfaceData.perceptualSmoothness = UNITY_SAMPLE_TEX2D(_BaseColorMap, input.texCoord0).a;
#elif defined(_MASKMAP)
    surfaceData.perceptualSmoothness = UNITY_SAMPLE_TEX2D(_MaskMap, input.texCoord0).a;
#else
    surfaceData.perceptualSmoothness = 1.0;
#endif
    surfaceData.perceptualSmoothness *= _Smoothness;

    surfaceData.materialId = 0;

    // MaskMap is Metalic, Ambient Occlusion, (Optional) - emissive Mask, Optional - Smoothness (in alpha)
#ifdef _MASKMAP
    surfaceData.metalic = UNITY_SAMPLE_TEX2D(_MaskMap, input.texCoord0).r;
    surfaceData.ambientOcclusion = UNITY_SAMPLE_TEX2D(_MaskMap, input.texCoord0).g;
#else
    surfaceData.metalic = 1.0;
    surfaceData.ambientOcclusion = 1.0;
#endif
    surfaceData.metalic *= _Metalic;


    surfaceData.tangentWS = input.tangentToWorld[0].xyz; // TODO: do with tangent same as with normal, sample into texture etc...
    surfaceData.anisotropy = 0;
    surfaceData.specular = 0.04;

    surfaceData.subSurfaceRadius = 1.0;
    surfaceData.thickness = 0.0;
    surfaceData.subSurfaceProfile = 0;

    surfaceData.coatNormalWS = float3(1.0, 0.0, 0.0);
    surfaceData.coatPerceptualSmoothness = 1.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);


    // Builtin Data
    builtinData.opacity = alpha;

    // TODO: Sample lightmap/lightprobe/volume proxy
    // This should also handle projective lightmap
    // Note that data input above can be use to sample into lightmap (like normal)
    builtinData.bakeDiffuseLighting = UNITY_SAMPLE_TEX2D(_DiffuseLightingMap, input.texCoord0).rgb;

    // If we chose an emissive color, we have a dedicated texture for it and don't use MaskMap
#ifdef _EMISSIVE_COLOR
    #ifdef _EMISSIVE_COLOR_MAP
    builtinData.emissiveColor = UNITY_SAMPLE_TEX2D(_EmissiveColorMap, input.texCoord0).rgb * _EmissiveColor;
    #else
    builtinData.emissiveColor = _EmissiveColor;
    #endif
#elif defined(_MASKMAP) // If we have a MaskMap, use emissive slot as a mask on baseColor
    builtinData.emissiveColor = surfaceData.baseColor * UNITY_SAMPLE_TEX2D(_MaskMap, input.texCoord0).bbb;
#else
    builtinData.emissiveColor = float3(0.0, 0.0, 0.0);
#endif

    builtinData.emissiveIntensity = _EmissiveIntensity;

    builtinData.velocity = float2(0.0, 0.0);

    builtinData.distortion = float2(0.0, 0.0);
    builtinData.distortionBlur = 0.0;
}

#endif // #if SHADER_STAGE_FRAGMENT
