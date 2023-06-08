Shader "Unlit/custom_shader_for_mv_override_testing"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 300

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _BaseColor;

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float4 vert (float3 position : POSITION) : POSITION
            {
                return TransformObjectToHClip(position);
            }

            float4 frag () : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }

        UsePass "Hidden/Universal Render Pipeline/ObjectMotionVectorFallback/MOTIONVECTORS"
    }
}
