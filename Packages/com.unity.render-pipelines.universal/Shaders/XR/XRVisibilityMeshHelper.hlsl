// Helper include for using XRVisibilityMesh vertex shader 
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct AttributesXR
{
    float4 vertex : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings VertVisibilityMeshXR(AttributesXR IN)
{
    Varyings OUT;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    OUT.positionCS = mul(UNITY_MATRIX_M, float4(IN.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0), UNITY_NEAR_CLIP_VALUE, 1.0f));
    
#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    OUT.stereoTargetEyeIndexAsRTArrayIdx = IN.vertex.z;    
#elif defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    if (unity_StereoEyeIndex != uint(IN.vertex.z))
        OUT.positionCS = float4(0.0f, 0.0f, 0.0f, 0.0f);
#endif

    // Mimic same logic of GetFullScreenTriangleVertexPosition, where the v is flipped
    // This matches the orientation of the screen to avoid additional y-flips
#if UNITY_UV_STARTS_AT_TOP
    OUT.texcoord.xy = DYNAMIC_SCALING_APPLY_SCALEBIAS((OUT.positionCS.xy) * float2(0.5f, -0.5f) + float2(0.5f, 0.5f));
#else
    OUT.texcoord.xy = DYNAMIC_SCALING_APPLY_SCALEBIAS((OUT.positionCS.xy) * float2(0.5f, 0.5f) + float2(0.5f, 0.5f));
#endif
    
    return OUT;
}
