#ifndef HDRP_SPEEDTREE7_COMMON_PASSES_INCLUDED
#define HDRP_SPEEDTREE7_COMMON_PASSES_INCLUDED

// Attributes
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define VARYINGS_NEED_TANGENT_TO_WORLD      // Necessary to get interpolators for normal
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define VARYINGS_NEED_POSITION_WS
#define ATTRIBUTES_NEED_COLOR
#define VARYINGS_NEED_COLOR

#ifdef EFFECT_BUMP
#define ATTRIBUTES_NEED_TANGENT
#endif

// Branch detail UV needs 3 components, BUT using TEXCOORD1 and TEXCOORD0 gives us 4, and we
// are only using 3 out of those 4, so we can make do with just 2 on TEXCOORD2
#ifdef GEOM_TYPE_BRANCH_DETAIL
#define VARYINGS_NEED_TEXCOORD2
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(EFFECT_HUE_VARIATION) || defined(GEOM_TYPE_BRANCH_DETAIL)
#define ATTRIBUTES_NEED_TEXCOORD1
#define VARYINGS_NEED_TEXCOORD1
#endif

#if (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_DEPTH_ONLY)
//#undef ATTRIBUTES_NEED_TANGENT
//#undef VARYINGS_NEED_TANGENT_TO_WORLD
#undef VARYINGS_NEED_TEXCOORD2
#undef ATTRIBUTES_NEED_TEXCOORD2
#undef EFFECT_BUMP
#undef EFFECT_HUE_VARIATION
#endif

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"

#ifdef CUSTOM_UNPACK

FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    UNITY_SETUP_INSTANCE_ID(input);
    
    output.worldToTangent = k_identity3x3;

    output.positionSS = input.positionCS;   // input.positionCS is SV_Position
    output.positionRWS.xyz = input.interpolators0.xyz;
    
    // uvHueVariation.xy
    output.texCoord0.xy = input.interpolators3.xy;

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
#ifdef EFFECT_BUMP
    output.worldToTangent = BuildWorldToTangent(input.interpolators2, input.interpolators1);
#else
    output.worldToTangent = BuildWorldToTangent(input.interpolators2, input.interpolators1);
    //output.worldToTangent[0] = cross(output.worldToTangent[2], output.worldToTangent[1]);
#endif
    output.worldToTangent[0] = GetOddNegativeScale() * cross(output.worldToTangent[2], output.worldToTangent[1]);
#endif

    // Vertex Color
    output.color.rgba = input.interpolators5.rgba;

#if (SHADERPASS != SHADERPASS_SHADOWS) && (SHADERPASS != SHADERPASS_DEPTH_ONLY)
    // Z component of uvHueVariation
#ifdef EFFECT_HUE_VARIATION
    output.texCoord0.z = input.interpolators3.z;
#endif

#ifdef GEOM_TYPE_BRANCH_DETAIL
    output.texCoord2.xy = input.interpolators4.xy;
    output.texCoord2.z = input.interpolators3.w;
#endif

#endif

    return output;
}

#endif // CUSTOM_UNPACK

#endif // HDRP_SPEEDTREE7_COMMON_PASSES_INCLUDED
