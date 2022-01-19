Shader "Hidden/HDRP/BFProbeDebug"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            Name "DepthForwardOnly"
            Tags { "LightMode" = "DepthForwardOnly" }

            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/BFProbeDebug.hlsl"

            #pragma vertex Vert
            #pragma fragment FragDepthOnly

            #pragma editor_sync_compilation
            ENDHLSL
        }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/BFProbeDebug.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma editor_sync_compilation
            ENDHLSL
        }
    }
}
