#ifndef UNIVERSAL_COPY_DEPTH_PASS_INCLUDED
#define UNIVERSAL_COPY_DEPTH_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

#if defined(_DEPTH_MSAA_2)
    #define MSAA_SAMPLES 2
#elif defined(_DEPTH_MSAA_4)
    #define MSAA_SAMPLES 4
#elif defined(_DEPTH_MSAA_8)
    #define MSAA_SAMPLES 8
#else
    #define MSAA_SAMPLES 1
#endif

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
#define DEPTH_TEXTURE_MS(name, samples) Texture2DMSArray<float, samples> name
#define DEPTH_TEXTURE(name) TEXTURE2D_ARRAY_FLOAT(name)
#define LOAD_MSAA(coord, sampleIndex) LOAD_TEXTURE2D_ARRAY_MSAA(_CameraDepthAttachment, coord, unity_StereoEyeIndex, sampleIndex) 
#define LOAD(coord) LOAD_TEXTURE2D_ARRAY(_CameraDepthAttachment, coord, unity_StereoEyeIndex) 
#else
#define DEPTH_TEXTURE_MS(name, samples) Texture2DMS<float, samples> name
#define DEPTH_TEXTURE(name) TEXTURE2D_FLOAT(name)
#define LOAD_MSAA(coord, sampleIndex) LOAD_TEXTURE2D_MSAA(_CameraDepthAttachment, coord, sampleIndex) 
#define LOAD(coord) LOAD_TEXTURE2D(_CameraDepthAttachment, coord) 
#endif

#if MSAA_SAMPLES == 1
    DEPTH_TEXTURE(_CameraDepthAttachment);
#else
    DEPTH_TEXTURE_MS(_CameraDepthAttachment, MSAA_SAMPLES);
#endif

#if UNITY_REVERSED_Z
    #define DEPTH_DEFAULT_VALUE 1.0
    #define DEPTH_OP min
#else
    #define DEPTH_DEFAULT_VALUE 0.0
    #define DEPTH_OP max
#endif

float SampleDepth(float2 pixelCoords) 
{
    int2 coord = int2(pixelCoords); 
#if MSAA_SAMPLES == 1
    return LOAD(coord); 
#else
    float outDepth = DEPTH_DEFAULT_VALUE;

    UNITY_UNROLL
    for (int i = 0; i < MSAA_SAMPLES; ++i)
        outDepth = DEPTH_OP(LOAD_MSAA(coord, i), outDepth); 
    return outDepth;
#endif
}

#if defined(_OUTPUT_DEPTH)
float frag(Varyings input) : SV_Depth
#else
float frag(Varyings input) : SV_Target
#endif
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    return SampleDepth(input.positionCS.xy);
}

#endif
