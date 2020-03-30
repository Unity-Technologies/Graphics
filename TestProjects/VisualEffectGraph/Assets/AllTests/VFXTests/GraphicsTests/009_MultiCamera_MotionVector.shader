Shader "FullScreen/009_MultiCamera_MotionVector"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"

    //TEXTURE2D_X(_CameraMotionVectorsTexture);
    float2 SampleMotionVectors(uint2 coords)
    {
        float2 motionVectorNDC;
        DecodeMotionVector(LOAD_TEXTURE2D_X(_CameraMotionVectorsTexture, coords), motionVectorNDC);
        return motionVectorNDC;
    }

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float2 mv = SampleMotionVectors(varyings.positionCS.xy);

        // Background color intensity - keep this low unless you want to make your eyes bleed
        const float kMinIntensity = 0.03f;
        const float kMaxIntensity = 0.50f;

        // Map motion vector direction to color wheel (hue between 0 and 360deg)
        float phi = atan2(mv.x, mv.y);
        float hue = (phi / PI + 1.0) * 0.5;
        float r = abs(hue * 6.0 - 3.0) - 1.0;
        float g = 2.0 - abs(hue * 6.0 - 2.0);
        float b = 2.0 - abs(hue * 6.0 - 4.0);

        float maxSpeed = 60.0f / 0.15f; // Admit that 15% of a move the viewport by second at 60 fps is really fast
        float absoluteLength = saturate(length(mv.xy) * maxSpeed);
        float4 color = float4(0.0, 0.0, 0.0, 0.0);
        color.rgb = float3(r, g, b) * lerp(kMinIntensity, kMaxIntensity, absoluteLength);
        color.rgb = saturate(color.rgb);

        color.a = length(mv) > 0.0f ? 1.0f : 0.0;

        return float4(color.rgb, color.a);
    }
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "Custom Pass 0"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}
