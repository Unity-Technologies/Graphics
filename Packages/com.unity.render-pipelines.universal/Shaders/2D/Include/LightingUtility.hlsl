// Must match: LightBatch.isBatchingSupported
#define USE_STRUCTURED_BUFFER_FOR_LIGHT2D_DATA 0

#if USE_NORMAL_MAP
    #if LIGHT_QUALITY_FAST
        #define NORMALS_LIGHTING_COORDS(TEXCOORDA, TEXCOORDB) \
            half4   lightDirection  : TEXCOORDA;\
            float2  screenUV   : TEXCOORDB;

        #define TRANSFER_NORMALS_LIGHTING(output, worldSpacePos, lightPosition, lightZDistance)\
            output.screenUV = ComputeNormalizedDeviceCoordinates(output.positionCS.xyz / output.positionCS.w);\
            half3 planeNormal = -GetViewForwardDir();\
            half3 projLightPos = lightPosition.xyz - (dot(lightPosition.xyz - worldSpacePos.xyz, planeNormal) - lightZDistance) * planeNormal;\
            output.lightDirection.xyz = projLightPos - worldSpacePos.xyz;\
            output.lightDirection.w = 0;

        #define APPLY_NORMALS_LIGHTING(input, lightColor, lightPosition, lightZDistance)\
            half4 normal = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.screenUV);\
            half3 normalUnpacked = UnpackNormalRGBNoScale(normal);\
            half3 dirToLight = normalize(input.lightDirection.xyz);\
            lightColor = lightColor * saturate(dot(dirToLight, normalUnpacked));
    #else
        #define NORMALS_LIGHTING_COORDS(TEXCOORDA, TEXCOORDB) \
            half4   positionWS : TEXCOORDA;\
            float2  screenUV   : TEXCOORDB;

        #define TRANSFER_NORMALS_LIGHTING(output, worldSpacePos, lightPosition, lightZDistance) \
            output.screenUV = ComputeNormalizedDeviceCoordinates(output.positionCS.xyz / output.positionCS.w); \
            output.positionWS = worldSpacePos;

        #define APPLY_NORMALS_LIGHTING(input, lightColor, lightPosition, lightZDistance)\
            half4 normal = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.screenUV);\
            half3 normalUnpacked = UnpackNormalRGBNoScale(normal);\
            half3 planeNormal = -GetViewForwardDir();\
            half3 projLightPos = lightPosition.xyz - (dot(lightPosition.xyz - input.positionWS.xyz, planeNormal) - lightZDistance) * planeNormal;\
            half3 dirToLight = normalize(projLightPos - input.positionWS.xyz);\
            lightColor = lightColor * saturate(dot(dirToLight, normalUnpacked));
    #endif

    #define NORMALS_LIGHTING_VARIABLES \
            TEXTURE2D(_NormalMap); \
            SAMPLER(sampler_NormalMap);
#else
    #define NORMALS_LIGHTING_COORDS(TEXCOORDA, TEXCOORDB)
    #define NORMALS_LIGHTING_VARIABLES
    #define TRANSFER_NORMALS_LIGHTING(output, worldSpacePos, lightPosition, lightZDistance)
    #define APPLY_NORMALS_LIGHTING(input, lightColor, lightPosition, lightZDistance)
#endif

#define SHADOW_COORDS(TEXCOORDA)\
    float2  shadowUV    : TEXCOORDA;

#define SHADOW_VARIABLES\
    TEXTURE2D(_ShadowTex);\
    SAMPLER(sampler_ShadowTex);

// Need to look at shadow caster to remove issue with shadows
#define APPLY_SHADOWS(input, color, intensity)\
    if(intensity < 1)\
    {\
        half4 shadowTex = SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, input.shadowUV); \
        half4 shadowIntensity = 1-max(shadowTex.r, shadowTex.g * 1-shadowTex.b);\
        color.rgb = (color.rgb * shadowIntensity.rgb) + (color.rgb * intensity*(1 - shadowIntensity.rgb));\
     }

#define TRANSFER_SHADOWS(output)\
    output.shadowUV = ComputeNormalizedDeviceCoordinates(output.positionCS.xyz / output.positionCS.w);

#define SHAPE_LIGHT(index)\
    TEXTURE2D(_ShapeLightTexture##index);\
    SAMPLER(sampler_ShapeLightTexture##index);\
    half2 _ShapeLightBlendFactors##index;\
    half4 _ShapeLightMaskFilter##index;\
    half4 _ShapeLightInvertedFilter##index;

#if !defined(USE_SHAPE_LIGHT_TYPE_0) && !defined(USE_SHAPE_LIGHT_TYPE_1) && !defined(USE_SHAPE_LIGHT_TYPE_2) && !defined(USE_SHAPE_LIGHT_TYPE_3)
#define USE_DEFAULT_LIGHT_TYPE 1
#endif

struct FragmentOutput
{
#if USE_SHAPE_LIGHT_TYPE_0 || USE_DEFAULT_LIGHT_TYPE
   half4 GLightBuffer0 : SV_Target0;
#endif
#if USE_SHAPE_LIGHT_TYPE_1
    half4 GLightBuffer1 : SV_Target1;
#endif
#if USE_SHAPE_LIGHT_TYPE_2
    half4 GLightBuffer2 : SV_Target2;
#endif
#if USE_SHAPE_LIGHT_TYPE_3
    half4 GLightBuffer3 : SV_Target3;
#endif
};

FragmentOutput ToFragmentOutput(half4 finalColor)
{
    FragmentOutput output;

    #if USE_SHAPE_LIGHT_TYPE_0 || USE_DEFAULT_LIGHT_TYPE
    output.GLightBuffer0 = finalColor;
    #endif
    #if USE_SHAPE_LIGHT_TYPE_1
    output.GLightBuffer1 = finalColor;
    #endif
    #if USE_SHAPE_LIGHT_TYPE_2
    output.GLightBuffer2 = finalColor;
    #endif
    #if USE_SHAPE_LIGHT_TYPE_3
    output.GLightBuffer3 = finalColor;
    #endif

    return output;
}

#define LIGHT_OFFSET(TEXCOORD)\
    float4 lightOffset : TEXCOORD;

// Light-Batcher mapping for Reference.
// OuterAngle;                  // 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
// InnerAngle;                  // 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
// InnerRadiusMult;             // 1-0 where 1 is the value at the center and 0 is the value at the outer radius

// Note: IsFullSpotlight        // Is no longer fed but deduced within the shader. Value basically is test for InnerAngle = 1.0f
// Likewise InnerAngleMult is also deduced and is basically 1 / (Outer - Inner)
// Position.xyz         => _LightPosition
// Position.w           => _LightZDistance
// ShadowIntensity      => In case of Volumetric Lighting this represents ShadowVolumeIntensity

struct PerLight2D
{
    float4x4    InvMatrix;
    float4      Color;
    float4      Position;
    float       FalloffIntensity;
    float       FalloffDistance;
    float       OuterAngle;
    float       InnerAngle;
    float       InnerRadiusMult;
    float       VolumeOpacity;
    float       ShadowIntensity;
    int         LightType;
};

#if USE_STRUCTURED_BUFFER_FOR_LIGHT2D_DATA

    #define UNITY_LIGHT2D_DATA                  \
                                                \
        uniform StructuredBuffer<PerLight2D> _Light2DBuffer;    \
                                                                \
        int _BatchBufferOffset;                                 \
                                                                \
        PerLight2D GetPerLight2D(float4 color)                  \
        {                                                       \
            int idx = (int)(color.b * 64) + _BatchBufferOffset; \
            return _Light2DBuffer[idx];                         \
        }

    #define _L2D_INVMATRIX          light.InvMatrix
    #define _L2D_COLOR              light.Color
    #define _L2D_POSITION           light.Position
    #define _L2D_FALLOFF_INTENSITY  light.FalloffIntensity
    #define _L2D_FALLOFF_DISTANCE   light.FalloffDistance
    #define _L2D_OUTER_ANGLE        light.OuterAngle
    #define _L2D_INNER_ANGLE        light.InnerAngle
    #define _L2D_INNER_RADIUS_MULT  light.InnerRadiusMult
    #define _L2D_VOLUME_OPACITY     light.VolumeOpacity
    #define _L2D_SHADOW_INTENSITY   light.ShadowIntensity
    #define _L2D_LIGHT_TYPE         light.LightType

#else

    #define UNITY_LIGHT2D_DATA                  \
                                                \
            float4x4    L2DInvMatrix;           \
            float4      L2DColor;               \
            float4      L2DPosition;            \
            float       L2DFalloffIntensity;    \
            float       L2DFalloffDistance;     \
            float       L2DOuterAngle;          \
            float       L2DInnerAngle;          \
            float       L2DInnerRadiusMult;     \
            float       L2DVolumeOpacity;       \
            float       L2DShadowIntensity;     \
            int         L2DLightType;           \

    #define _L2D_INVMATRIX          L2DInvMatrix
    #define _L2D_COLOR              L2DColor
    #define _L2D_POSITION           L2DPosition
    #define _L2D_FALLOFF_INTENSITY  L2DFalloffIntensity
    #define _L2D_FALLOFF_DISTANCE   L2DFalloffDistance
    #define _L2D_OUTER_ANGLE        L2DOuterAngle
    #define _L2D_INNER_ANGLE        L2DInnerAngle
    #define _L2D_INNER_RADIUS_MULT  L2DInnerRadiusMult
    #define _L2D_VOLUME_OPACITY     L2DVolumeOpacity
    #define _L2D_SHADOW_INTENSITY   L2DShadowIntensity
    #define _L2D_LIGHT_TYPE         L2DLightType

#endif
