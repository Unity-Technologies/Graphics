#ifndef HDRP_SPEEDTREE7_COMMON_PASSES_INCLUDED
#define HDRP_SPEEDTREE7_COMMON_PASSES_INCLUDED

// Attributes
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TANGENT
#define VARYINGS_NEED_TANGENT_TO_WORLD

#define VARYINGS_NEED_POSITION_WS

#ifndef EFFECT_BUMP
#undef ATTRIBUTES_NEED_TANGENT
#undef VARYINGS_NEED_TANGENT_TO_WORLD
#endif

#ifdef GEOM_TYPE_BRANCH_DETAIL
#define VARYINGS_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#if defined(SHADOW_CASTER) || defined(SHADOW_LOW)
#undef ATTRIBUTES_NEED_TANGENT
#undef VARYINGS_NEED_TANGENT_TO_WORLD
#undef VARYINGS_NEED_TEXCOORD1
#undef ATTRIBUTES_NEED_TEXCOORD1
#undef VARYINGS_NEED_TEXCOORD3
#undef ATTRIBUTES_NEED_TEXCOORD3
#endif

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
    
#ifdef EFFECT_BUMP
    output.worldToTangent = BuildWorldToTangent(input.interpolators2, input.interpolators1);
#else
    output.worldToTangent = BuildWorldToTangent(input.interpolators2, input.interpolators1);
    output.worldToTangent[0] = cross(input.interpolators1, output.worldToTangent[1]);
#endif  

    // Vertex Color
#ifdef VARYINGS_NEED_COLOR
    output.color.rgba = input.interpolators5.rgba;
#else
    output.color.rgba = _Color;
#endif

    // Z component of uvHueVariation
#ifdef EFFECT_HUE_VARIATION
    output.texCoord0.z = input.interpolators3.z;
#endif

#ifdef _MAIN_LIGHT_SHADOWS
    // TODO ...  where to put this?
    //output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    return output;
}

#endif // CUSTOM_UNPACK

#endif // HDRP_SPEEDTREE7_COMMON_PASSES_INCLUDED
