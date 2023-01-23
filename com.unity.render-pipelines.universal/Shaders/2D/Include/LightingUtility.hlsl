// Must match: LightBatch.isBatchingSupported
#if !defined(SHADER_API_GLES3) && !defined(SHADER_API_GLCORE) && !defined(SHADER_API_SWITCH)
    #define USE_STRUCTURED_BUFFER_FOR_LIGHT2D_DATA  1
#endif

#if USE_NORMAL_MAP
    #if LIGHT_QUALITY_FAST
        #define NORMALS_LIGHTING_COORDS(TEXCOORDA, TEXCOORDB) \
            half4   lightDirection  : TEXCOORDA;\
            half2   screenUV   : TEXCOORDB;

        #define TRANSFER_NORMALS_LIGHTING(output, worldSpacePos, lightPosition, lightZDistance)\
            output.screenUV = ComputeNormalizedDeviceCoordinates(output.positionCS.xyz / output.positionCS.w);\
            half3 planeNormal = -GetViewForwardDir();\
            half3 projLightPos = lightPosition.xyz - (dot(lightPosition.xyz - worldSpacePos.xyz, planeNormal) - lightZDistance) * planeNormal;\
            output.lightDirection.xyz = normalize(projLightPos - worldSpacePos.xyz);\
            output.lightDirection.w = 0;

        #define APPLY_NORMALS_LIGHTING(input, lightColor, lightPosition, lightZDistance)\
            half4 normal = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.screenUV);\
            half3 normalUnpacked = UnpackNormalRGBNoScale(normal);\
            lightColor = lightColor * saturate(dot(input.lightDirection.xyz, normalUnpacked));
    #else
        #define NORMALS_LIGHTING_COORDS(TEXCOORDA, TEXCOORDB) \
            half4   positionWS : TEXCOORDA;\
            half2   screenUV   : TEXCOORDB;

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
    float  _ShadowIntensity;\
    float  _ShadowVolumeIntensity;\
    half4  _ShadowColor = 1;\
    half4  _UnshadowColor = 1;\
    TEXTURE2D(_ShadowTex);\
    SAMPLER(sampler_ShadowTex);

// Need to look at shadow caster to remove issue with shadows
#define APPLY_SHADOWS(input, color, intensity)\
    if(intensity < 1)\
    {\
        half4 shadowTex = SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, input.shadowUV); \
        half  shadowFinalValue   = dot(half4(1,0,0,0), shadowTex.rgba);\
        half  unshadowValue = dot(half4(0,1,0,0), shadowTex.rgba);\
        half  unshadowGTEOne = unshadowValue > 1;\
        half  spriteAlpha   = dot(half4(0,0,1,0), shadowTex.rgba);\
        half  unshadowFinalValue = unshadowGTEOne * (unshadowValue - (1-spriteAlpha)) + (1-unshadowGTEOne) * (unshadowValue * spriteAlpha);\
        half  shadowIntensity = 1-saturate(shadowFinalValue - unshadowFinalValue); \
        color.rgb = (color.rgb * shadowIntensity) + (color.rgb * intensity*(1 - shadowIntensity));\
     }

#define TRANSFER_SHADOWS(output)\
    output.shadowUV = ComputeNormalizedDeviceCoordinates(output.positionCS.xyz / output.positionCS.w);

#define SHAPE_LIGHT(index)\
    TEXTURE2D(_ShapeLightTexture##index);\
    SAMPLER(sampler_ShapeLightTexture##index);\
    half2 _ShapeLightBlendFactors##index;\
    half4 _ShapeLightMaskFilter##index;\
    half4 _ShapeLightInvertedFilter##index;

struct FragmentOutput
{
    half4 GLightBuffer0 : SV_Target0;
    half4 GLightBuffer1 : SV_Target1;
    half4 GLightBuffer2 : SV_Target2;
    half4 GLightBuffer3 : SV_Target3;
};

FragmentOutput ToFragmentOutput(half4 finalColor)
{
    FragmentOutput output;
    #if USE_SHAPE_LIGHT_TYPE_0
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
    #if !defined(USE_SHAPE_LIGHT_TYPE_0) && !defined(USE_SHAPE_LIGHT_TYPE_1) && !defined(USE_SHAPE_LIGHT_TYPE_2) && !defined(USE_SHAPE_LIGHT_TYPE_3)
    output.GLightBuffer0 = finalColor;
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

#if defined(USE_STRUCTURED_BUFFER_FOR_LIGHT2D_DATA)

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
                                                \
            PerLight2D GetPerLight2D(float4 color)                  \
            {                                                       \
                PerLight2D light;                                   \
                light.InvMatrix = L2DInvMatrix;                     \
                light.Color = L2DColor;                             \
                light.Position = L2DPosition;                       \
                light.FalloffIntensity = L2DFalloffIntensity;       \
                light.FalloffDistance = L2DFalloffDistance;         \
                light.OuterAngle = L2DOuterAngle;                   \
                light.InnerAngle = L2DInnerAngle;                   \
                light.InnerRadiusMult = L2DInnerRadiusMult;         \
                light.VolumeOpacity = L2DVolumeOpacity;             \
                light.ShadowIntensity = L2DShadowIntensity;         \
                light.LightType = L2DLightType;                     \
                return light;                                       \
            }

#endif
