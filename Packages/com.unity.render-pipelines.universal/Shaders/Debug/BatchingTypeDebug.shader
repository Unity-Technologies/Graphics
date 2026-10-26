Shader "Hidden/Universal Render Pipeline/Debug/BatchingTypeDebug"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "BatchingType"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma target 4.5

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl" // GPU Instancing — DOTS variant
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                nointerpolation uint state : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.state = unity_RendererUserValue;
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                if (input.state == 0) return half4(0.5, 0.5, 0.5, 1); // Untracked   (Grey)
                if (input.state == 1) return half4(1, 0.5, 0, 1);     // Unbatched   (Orange)
                if (input.state == 2) return half4(0, 1, 0, 1);       // GRD         (Green)
                return half4(0, 0, 1, 1);                             // SRP Batcher (Blue)
            }
            ENDHLSL
        }
    }

    Fallback Off
}
