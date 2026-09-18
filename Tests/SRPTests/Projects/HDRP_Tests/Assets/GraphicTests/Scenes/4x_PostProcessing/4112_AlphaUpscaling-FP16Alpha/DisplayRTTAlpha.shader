Shader "FullScreen/DisplayRTTAlpha"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    float4 CopyAlphaPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        float alpha = LOAD_TEXTURE2D_X_LOD(_AfterPostProcessColorBuffer, varyings.positionCS.xy, 0).a;

        return float4(alpha.xxx, 1.0);
    }

    float4 DisplayAlphaPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        return float4(CustomPassLoadCustomColor(varyings.positionCS.xy).rgb, 1.0);
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "Copy Alpha"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment CopyAlphaPass
            ENDHLSL
        }

        Pass
        {
            Name "Display Alpha"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment DisplayAlphaPass
            ENDHLSL
        }
    }
    Fallback Off
}
