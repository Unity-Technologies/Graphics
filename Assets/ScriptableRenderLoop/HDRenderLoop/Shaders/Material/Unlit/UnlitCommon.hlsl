#define UNITY_MATERIAL_UNLIT // Need to be define before including Material.hlsl
// no need to include lighting
#include "../Material.hlsl"
#include "../../ShaderVariables.hlsl"
#include "../../Debug/DebugViewMaterial.hlsl"

//-------------------------------------------------------------------------------------
// variable declaration
//-------------------------------------------------------------------------------------

float4  _Color;
UNITY_DECLARE_TEX2D(_ColorMap);
float3 _EmissiveColor;
UNITY_DECLARE_TEX2D(_EmissiveColorMap);
float _EmissiveIntensity;

float _AlphaCutoff;

//-------------------------------------------------------------------------------------
// Lighting architecture
//-------------------------------------------------------------------------------------

// TODO: Check if we will have different Varyings based on different pass, not sure about that...

// Forward
struct Attributes
{
    float3 positionOS   : POSITION;
    float2 uv0          : TEXCOORD0;
};

struct Varyings
{
    float4 positionHS;
    float2 texCoord0;

#ifdef SHADER_STAGE_FRAGMENT
#if defined(_DOUBLESIDED_LIGHTING_FLIP) || defined(_DOUBLESIDED_LIGHTING_MIRROR)
    FRONT_FACE_TYPE cullFace;
#endif
#endif
};

struct PackedVaryings
{
    float4 positionHS : SV_Position;
    float4 interpolators[1] : TEXCOORD0;

#ifdef SHADER_STAGE_FRAGMENT
#if defined(_DOUBLESIDED_LIGHTING_FLIP) || defined(_DOUBLESIDED_LIGHTING_MIRROR)
    FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMATIC;
#endif
#endif
};

PackedVaryings PackVaryings(Varyings input)
{
    PackedVaryings output;
    output.positionHS = input.positionHS;
    output.interpolators[0].xy = input.texCoord0.xy;
    output.interpolators[0].zw = float2(0.0, 0.0);

    return output;
}

Varyings UnpackVaryings(PackedVaryings input)
{
    Varyings output;
    output.positionHS = input.positionHS;
    output.texCoord0.xy = input.interpolators[0].xy;

#ifdef SHADER_STAGE_FRAGMENT
#if defined(_DOUBLESIDED_LIGHTING_FLIP) || defined(_DOUBLESIDED_LIGHTING_MIRROR)
    output.cullFace = input.cullFace;
#endif
#endif

    return output;
}

PackedVaryings VertDefault(Attributes input)
{
    Varyings output;

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionHS = TransformWorldToHClip(positionWS);

    output.texCoord0 = input.uv0;

    return PackVaryings(output);
}

//-------------------------------------------------------------------------------------
// Fill SurfaceData/Lighting data function
//-------------------------------------------------------------------------------------

#if SHADER_STAGE_FRAGMENT

void GetSurfaceAndBuiltinData(float3 V, Varyings input, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    surfaceData.color = UNITY_SAMPLE_TEX2D(_ColorMap, input.texCoord0).rgb * _Color.rgb;
    float alpha = UNITY_SAMPLE_TEX2D(_ColorMap, input.texCoord0).a * _Color.a;

#ifdef _ALPHATEST_ON
    clip(alpha - _AlphaCutoff);
#endif

    // Builtin Data
    builtinData.opacity = alpha;

    builtinData.bakeDiffuseLighting = float3(0.0, 0.0, 0.0);

#ifdef _EMISSIVE_COLOR_MAP
    builtinData.emissiveColor = UNITY_SAMPLE_TEX2D(_EmissiveColorMap, input.texCoord0).rgb * _EmissiveColor;
#else
    builtinData.emissiveColor = _EmissiveColor;
#endif

    builtinData.emissiveIntensity = _EmissiveIntensity;

    builtinData.velocity = float2(0.0, 0.0);

    builtinData.distortion = float2(0.0, 0.0);
    builtinData.distortionBlur = 0.0;
}

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
        // TODO: require a remap
        result = float3(input.texCoord0, 0.0);
        break;
    }
}

#endif // #if SHADER_STAGE_FRAGMENT
