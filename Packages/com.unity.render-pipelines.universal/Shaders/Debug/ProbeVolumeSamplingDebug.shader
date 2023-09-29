Shader "Hidden/Universal Render Pipeline/ProbeVolumeSamplingDebug"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        LOD 100

        HLSLINCLUDE
        #pragma editor_sync_compilation
        #pragma target 4.5
        #pragma multi_compile_fragment _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2

        // Central render pipeline specific includes
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Debug/ProbeVolumeDebugBase.hlsl"

        #define PROBE_VOLUME_DEBUG_FUNCTION_SAMPLING
        #include "Packages/com.unity.render-pipelines.core/Runtime/Debug/ProbeVolumeDebugFunctions.hlsl"


        ENDHLSL

        Pass
        {
            Name "ForwardOnly"

            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
