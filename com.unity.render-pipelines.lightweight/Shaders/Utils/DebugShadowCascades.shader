Shader "Lightweight Render Pipeline/ShadowCascadeDebug"
{
    Properties
    {}
    SubShader
    {
        Tags { "RenderType" = "Opaque" "IgnoreProjectors" = "True" "RenderPipeline" = "LightweightPipeline" }
        ZWrite On
        Cull Off

        Pass
        {
            //Name "Shadow Cascade Debug"
            Tags{"LightMode" = "LightweightForward"}
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex vert
            #pragma fragment frag
            // -------------------------------------
            // Lightweight Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS   : POSITION;
            };

            struct Varyings
            {
                float3 positionWS   : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
                return output;
            }

            half4 frag( Varyings input ) : SV_Target
            {
                half4 colors[] = {half4(0.5f, 0.5f, 0.6f, 1.0), half4(0.5f, 0.6f, 0.5f, 1.0), half4(0.6f, 0.6f, 0.5f, 1.0), half4(0.6f, 0.5f, 0.5f, 1.0), half4(0.0, 0.0, 0.0, 1.0)};

                half cascadeIndex = ComputeCascadeIndex(input.positionWS);
                return colors[cascadeIndex];
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
 }
