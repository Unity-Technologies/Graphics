#if USE_NORMAL_MAP
    #if LIGHT_QUALITY_FAST
        #define NORMALS_LIGHTING_COORDS(TEXCOORDA, TEXCOORDB) \
            half4   lightDirection  : TEXCOORDA;\
            half2   screenUV   : TEXCOORDB;

        #define TRANSFER_NORMALS_LIGHTING(output, worldSpacePos)\
            output.screenUV = ComputeNormalizedDeviceCoordinates(output.positionCS.xyz / output.positionCS.w);\
            half3 planeNormal = -GetViewForwardDir();\
            half3 projLightPos = _LightPosition.xyz - (dot(_LightPosition.xyz - worldSpacePos.xyz, planeNormal) - _LightZDistance) * planeNormal;\
            output.lightDirection.xyz = projLightPos - worldSpacePos.xyz;\
            output.lightDirection.w = 0;

        #define APPLY_NORMALS_LIGHTING(input, lightColor)\
            half4 normal = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.screenUV);\
            half3 normalUnpacked = UnpackNormalRGBNoScale(normal);\
            half3 dirToLight = normalize(input.lightDirection.xyz);\
            lightColor = lightColor * saturate(dot(dirToLight, normalUnpacked));
    #else
        #define NORMALS_LIGHTING_COORDS(TEXCOORDA, TEXCOORDB) \
            half4   positionWS : TEXCOORDA;\
            half2   screenUV   : TEXCOORDB;

        #define TRANSFER_NORMALS_LIGHTING(output, worldSpacePos) \
            output.screenUV = ComputeNormalizedDeviceCoordinates(output.positionCS.xyz / output.positionCS.w); \
            output.positionWS = worldSpacePos;

        #define APPLY_NORMALS_LIGHTING(input, lightColor)\
            half4 normal = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.screenUV);\
            half3 normalUnpacked = UnpackNormalRGBNoScale(normal);\
            half3 planeNormal = -GetViewForwardDir();\
            half3 projLightPos = _LightPosition.xyz - (dot(_LightPosition.xyz - input.positionWS.xyz, planeNormal) - _LightZDistance) * planeNormal;\
            half3 dirToLight = normalize(projLightPos - input.positionWS.xyz);\
            lightColor = lightColor * saturate(dot(dirToLight, normalUnpacked));
    #endif

    #define NORMALS_LIGHTING_VARIABLES \
            TEXTURE2D(_NormalMap); \
            SAMPLER(sampler_NormalMap); \
            half4       _LightPosition;\
            half        _LightZDistance;
#else
    #define NORMALS_LIGHTING_COORDS(TEXCOORDA, TEXCOORDB)
    #define NORMALS_LIGHTING_VARIABLES
    #define TRANSFER_NORMALS_LIGHTING(output, worldSpacePos)
    #define APPLY_NORMALS_LIGHTING(input, lightColor)
#endif

#define SHADOW_COORDS(TEXCOORDA)\
    float2  shadowUV    : TEXCOORDA;

#define SHADOW_VARIABLES\
    float  _ShadowIntensity;\
    float  _ShadowVolumeIntensity;\
    half4  _ShadowColorMask = 1;\
    TEXTURE2D(_ShadowTex);\
    SAMPLER(sampler_ShadowTex);

//#define APPLY_SHADOWS(input, color, intensity)\
//    if(intensity < 1)\
//    {\
//        half4 shadow = saturate(SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, input.shadowUV)); \
//        half  shadowIntensity = 1 - (shadow.r * saturate(2 * (shadow.g - 0.5f * shadow.b))); \
//        color.rgb = (color.rgb * shadowIntensity) + (color.rgb * intensity*(1 - shadowIntensity));\
//    }

//half  shadowIntensity = 1-dot(_ShadowColorMask, shadow); \
//color.rgb = (color.rgb * shadowIntensity) + (color.rgb * intensity*(1 - shadowIntensity));\

// Need to look at shadow caster to remove issue with shadows
#define APPLY_SHADOWS(input, color, intensity)\
    if(intensity < 1)\
    {\
        half4 shadow = saturate(SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, input.shadowUV)); \
        half  shadowIntensity = 1-dot(_ShadowColorMask, shadow.rgba) ; \
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
