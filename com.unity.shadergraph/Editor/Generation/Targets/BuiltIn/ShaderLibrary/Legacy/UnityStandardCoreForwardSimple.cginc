#ifndef UNITY_STANDARD_CORE_FORWARD_SIMPLE_INCLUDED
#define UNITY_STANDARD_CORE_FORWARD_SIMPLE_INCLUDED

#include "UnityStandardCore.cginc"

//  Does not support: _PARALLAXMAP, DIRLIGHTMAP_COMBINED
#define GLOSSMAP (defined(_SPECGLOSSMAP) || defined(_METALLICGLOSSMAP))

#ifndef SPECULAR_HIGHLIGHTS
    #define SPECULAR_HIGHLIGHTS (!defined(_SPECULAR_HIGHLIGHTS_OFF))
#endif

struct VertexOutputBaseSimple
{
    UNITY_POSITION(pos);
    float4 tex                          : TEXCOORD0;
    half4 eyeVec                        : TEXCOORD1; // w: grazingTerm

    half4 ambientOrLightmapUV           : TEXCOORD2; // SH or Lightmap UV
    SHADOW_COORDS(3)
    UNITY_FOG_COORDS_PACKED(4, half4) // x: fogCoord, yzw: reflectVec

    half4 normalWorld                   : TEXCOORD5; // w: fresnelTerm

#ifdef _NORMALMAP
    half3 tangentSpaceLightDir          : TEXCOORD6;
    #if SPECULAR_HIGHLIGHTS
        half3 tangentSpaceEyeVec        : TEXCOORD7;
    #endif
#endif
#if UNITY_REQUIRE_FRAG_WORLDPOS
    float3 posWorld                     : TEXCOORD8;
#endif

    UNITY_VERTEX_OUTPUT_STEREO
};

// UNIFORM_REFLECTIVITY(): workaround to get (uniform) reflecivity based on UNITY_SETUP_BRDF_INPUT
half MetallicSetup_Reflectivity()
{
    return 1.0h - OneMinusReflectivityFromMetallic(_Metallic);
}

half SpecularSetup_Reflectivity()
{
    return SpecularStrength(_SpecColor.rgb);
}

half RoughnessSetup_Reflectivity()
{
    return MetallicSetup_Reflectivity();
}

#define JOIN2(a, b) a##b
#define JOIN(a, b) JOIN2(a,b)
#define UNIFORM_REFLECTIVITY JOIN(UNITY_SETUP_BRDF_INPUT, _Reflectivity)


#ifdef _NORMALMAP

half3 TransformToTangentSpace(half3 tangent, half3 binormal, half3 normal, half3 v)
{
    // Mali400 shader compiler prefers explicit dot product over using a half3x3 matrix
    return half3(dot(tangent, v), dot(binormal, v), dot(normal, v));
}

void TangentSpaceLightingInput(half3 normalWorld, half4 vTangent, half3 lightDirWorld, half3 eyeVecWorld, out half3 tangentSpaceLightDir, out half3 tangentSpaceEyeVec)
{
    half3 tangentWorld = UnityObjectToWorldDir(vTangent.xyz);
    half sign = half(vTangent.w) * half(unity_WorldTransformParams.w);
    half3 binormalWorld = cross(normalWorld, tangentWorld) * sign;
    tangentSpaceLightDir = TransformToTangentSpace(tangentWorld, binormalWorld, normalWorld, lightDirWorld);
    #if SPECULAR_HIGHLIGHTS
        tangentSpaceEyeVec = normalize(TransformToTangentSpace(tangentWorld, binormalWorld, normalWorld, eyeVecWorld));
    #else
        tangentSpaceEyeVec = 0;
    #endif
}

#endif // _NORMALMAP

VertexOutputBaseSimple vertForwardBaseSimple (VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputBaseSimple o;
    UNITY_INITIALIZE_OUTPUT(VertexOutputBaseSimple, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.tex = TexCoords(v);

    half3 eyeVec = normalize(posWorld.xyz - _WorldSpaceCameraPos);
    half3 normalWorld = UnityObjectToWorldNormal(v.normal);

    o.normalWorld.xyz = normalWorld;
    o.eyeVec.xyz = eyeVec;

    #ifdef _NORMALMAP
        half3 tangentSpaceEyeVec;
        TangentSpaceLightingInput(normalWorld, v.tangent, _WorldSpaceLightPos0.xyz, eyeVec, o.tangentSpaceLightDir, tangentSpaceEyeVec);
        #if SPECULAR_HIGHLIGHTS
            o.tangentSpaceEyeVec = tangentSpaceEyeVec;
        #endif
    #endif

    //We need this for shadow receiving
    TRANSFER_SHADOW(o);

    o.ambientOrLightmapUV = VertexGIForward(v, posWorld, normalWorld);

    o.fogCoord.yzw = reflect(eyeVec, normalWorld);

    o.normalWorld.w = Pow4(1 - saturate(dot(normalWorld, -eyeVec))); // fresnel term
    #if !GLOSSMAP
        o.eyeVec.w = saturate(_Glossiness + UNIFORM_REFLECTIVITY()); // grazing term
    #endif

    UNITY_TRANSFER_FOG(o, o.pos);
    return o;
}


FragmentCommonData FragmentSetupSimple(VertexOutputBaseSimple i)
{
    half alpha = Alpha(i.tex.xy);
    #if defined(_ALPHATEST_ON)
        clip (alpha - _Cutoff);
    #endif

    FragmentCommonData s = UNITY_SETUP_BRDF_INPUT (i.tex);

    // NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
    s.diffColor = PreMultiplyAlpha (s.diffColor, alpha, s.oneMinusReflectivity, /*out*/ s.alpha);

    s.normalWorld = i.normalWorld.xyz;
    s.eyeVec = i.eyeVec.xyz;
    s.posWorld = IN_WORLDPOS(i);
    s.reflUVW = i.fogCoord.yzw;

    #ifdef _NORMALMAP
        s.tangentSpaceNormal =  NormalInTangentSpace(i.tex);
    #else
        s.tangentSpaceNormal =  0;
    #endif

    return s;
}

UnityLight MainLightSimple(VertexOutputBaseSimple i, FragmentCommonData s)
{
    UnityLight mainLight = MainLight();
    return mainLight;
}

half PerVertexGrazingTerm(VertexOutputBaseSimple i, FragmentCommonData s)
{
    #if GLOSSMAP
        return saturate(s.smoothness + (1-s.oneMinusReflectivity));
    #else
        return i.eyeVec.w;
    #endif
}

half PerVertexFresnelTerm(VertexOutputBaseSimple i)
{
    return i.normalWorld.w;
}

#if !SPECULAR_HIGHLIGHTS
#   define REFLECTVEC_FOR_SPECULAR(i, s) half3(0, 0, 0)
#elif defined(_NORMALMAP)
#   define REFLECTVEC_FOR_SPECULAR(i, s) reflect(i.tangentSpaceEyeVec, s.tangentSpaceNormal)
#else
#   define REFLECTVEC_FOR_SPECULAR(i, s) s.reflUVW
#endif

half3 LightDirForSpecular(VertexOutputBaseSimple i, UnityLight mainLight)
{
    #if SPECULAR_HIGHLIGHTS && defined(_NORMALMAP)
        return i.tangentSpaceLightDir;
    #else
        return mainLight.dir;
    #endif
}

half3 BRDF3DirectSimple(half3 diffColor, half3 specColor, half smoothness, half rl)
{
    #if SPECULAR_HIGHLIGHTS
        return BRDF3_Direct(diffColor, specColor, Pow4(rl), smoothness);
    #else
        return diffColor;
    #endif
}

half4 fragForwardBaseSimpleInternal (VertexOutputBaseSimple i)
{
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

    FragmentCommonData s = FragmentSetupSimple(i);

    UnityLight mainLight = MainLightSimple(i, s);

    #if !defined(LIGHTMAP_ON) && defined(_NORMALMAP)
    half ndotl = saturate(dot(s.tangentSpaceNormal, i.tangentSpaceLightDir));
    #else
    half ndotl = saturate(dot(s.normalWorld, mainLight.dir));
    #endif

    //we can't have worldpos here (not enough interpolator on SM 2.0) so no shadow fade in that case.
    half shadowMaskAttenuation = UnitySampleBakedOcclusion(i.ambientOrLightmapUV, 0);
    half realtimeShadowAttenuation = SHADOW_ATTENUATION(i);
    half atten = UnityMixRealtimeAndBakedShadows(realtimeShadowAttenuation, shadowMaskAttenuation, 0);

    half occlusion = Occlusion(i.tex.xy);
    half rl = dot(REFLECTVEC_FOR_SPECULAR(i, s), LightDirForSpecular(i, mainLight));

    UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, mainLight);
    half3 attenuatedLightColor = gi.light.color * ndotl;

    half3 c = BRDF3_Indirect(s.diffColor, s.specColor, gi.indirect, PerVertexGrazingTerm(i, s), PerVertexFresnelTerm(i));
    c += BRDF3DirectSimple(s.diffColor, s.specColor, s.smoothness, rl) * attenuatedLightColor;
    c += Emission(i.tex.xy);

    UNITY_APPLY_FOG(i.fogCoord, c);

    return OutputForward (half4(c, 1), s.alpha);
}

half4 fragForwardBaseSimple (VertexOutputBaseSimple i) : SV_Target  // backward compatibility (this used to be the fragment entry function)
{
    return fragForwardBaseSimpleInternal(i);
}

struct VertexOutputForwardAddSimple
{
    UNITY_POSITION(pos);
    float4 tex                          : TEXCOORD0;
    float3 posWorld                     : TEXCOORD1;

#if !defined(_NORMALMAP) && SPECULAR_HIGHLIGHTS
    UNITY_FOG_COORDS_PACKED(2, half4) // x: fogCoord, yzw: reflectVec
#else
    UNITY_FOG_COORDS_PACKED(2, half1)
#endif

    half3 lightDir                      : TEXCOORD3;

#if defined(_NORMALMAP)
    #if SPECULAR_HIGHLIGHTS
        half3 tangentSpaceEyeVec        : TEXCOORD4;
    #endif
#else
    half3 normalWorld                   : TEXCOORD4;
#endif

    UNITY_LIGHTING_COORDS(5, 6)

    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutputForwardAddSimple vertForwardAddSimple (VertexInput v)
{
    VertexOutputForwardAddSimple o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_OUTPUT(VertexOutputForwardAddSimple, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.tex = TexCoords(v);
    o.posWorld = posWorld.xyz;

    //We need this for shadow receiving and lighting
    UNITY_TRANSFER_LIGHTING(o, v.uv1);

    half3 lightDir = _WorldSpaceLightPos0.xyz - posWorld.xyz * _WorldSpaceLightPos0.w;
    #ifndef USING_DIRECTIONAL_LIGHT
        lightDir = NormalizePerVertexNormal(lightDir);
    #endif

    #if SPECULAR_HIGHLIGHTS
        half3 eyeVec = normalize(posWorld.xyz - _WorldSpaceCameraPos);
    #endif

    half3 normalWorld = UnityObjectToWorldNormal(v.normal);

    #ifdef _NORMALMAP
        #if SPECULAR_HIGHLIGHTS
            TangentSpaceLightingInput(normalWorld, v.tangent, lightDir, eyeVec, o.lightDir, o.tangentSpaceEyeVec);
        #else
            half3 ignore;
            TangentSpaceLightingInput(normalWorld, v.tangent, lightDir, 0, o.lightDir, ignore);
        #endif
    #else
        o.lightDir = lightDir;
        o.normalWorld = normalWorld;
        #if SPECULAR_HIGHLIGHTS
            o.fogCoord.yzw = reflect(eyeVec, normalWorld);
        #endif
    #endif

    UNITY_TRANSFER_FOG(o,o.pos);
    return o;
}

FragmentCommonData FragmentSetupSimpleAdd(VertexOutputForwardAddSimple i)
{
    half alpha = Alpha(i.tex.xy);
    #if defined(_ALPHATEST_ON)
        clip (alpha - _Cutoff);
    #endif

    FragmentCommonData s = UNITY_SETUP_BRDF_INPUT (i.tex);

    // NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
    s.diffColor = PreMultiplyAlpha (s.diffColor, alpha, s.oneMinusReflectivity, /*out*/ s.alpha);

    s.eyeVec = 0;
    s.posWorld = i.posWorld;

    #ifdef _NORMALMAP
        s.tangentSpaceNormal = NormalInTangentSpace(i.tex);
        s.normalWorld = 0;
    #else
        s.tangentSpaceNormal = 0;
        s.normalWorld = i.normalWorld;
    #endif

    #if SPECULAR_HIGHLIGHTS && !defined(_NORMALMAP)
        s.reflUVW = i.fogCoord.yzw;
    #else
        s.reflUVW = 0;
    #endif

    return s;
}

half3 LightSpaceNormal(VertexOutputForwardAddSimple i, FragmentCommonData s)
{
    #ifdef _NORMALMAP
        return s.tangentSpaceNormal;
    #else
        return i.normalWorld;
    #endif
}

half4 fragForwardAddSimpleInternal (VertexOutputForwardAddSimple i)
{
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

    FragmentCommonData s = FragmentSetupSimpleAdd(i);

    half3 c = BRDF3DirectSimple(s.diffColor, s.specColor, s.smoothness, dot(REFLECTVEC_FOR_SPECULAR(i, s), i.lightDir));

    #if SPECULAR_HIGHLIGHTS // else diffColor has premultiplied light color
        c *= _LightColor0.rgb;
    #endif

    UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld)
    c *= atten * saturate(dot(LightSpaceNormal(i, s), i.lightDir));

    UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass
    return OutputForward (half4(c, 1), s.alpha);
}

half4 fragForwardAddSimple (VertexOutputForwardAddSimple i) : SV_Target // backward compatibility (this used to be the fragment entry function)
{
    return fragForwardAddSimpleInternal(i);
}

#endif // UNITY_STANDARD_CORE_FORWARD_SIMPLE_INCLUDED
