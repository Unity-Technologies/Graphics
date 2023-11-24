Shader "Hidden/Core/ProbeVolumeFragmentationDebug"
{
    SubShader
    {
        Tags{ }
        Pass
        {
            ZWrite On
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            // #pragma enable_d3d11_debug_symbols

            #pragma vertex Vert
            #pragma fragment Frag

            // Central render pipeline specific includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Debug/ProbeVolumeDebugBase.hlsl"

            #define PROBE_VOLUME_DEBUG_FUNCTION_FRAGMENTATION
            #include "Packages/com.unity.render-pipelines.core/Runtime/Debug/ProbeVolumeDebugFunctions.hlsl"
            ENDHLSL
        }

    }
        Fallback Off
}
