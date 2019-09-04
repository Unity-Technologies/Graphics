Shader "Hidden/Universal Render Pipeline/StencilDeferred"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Deferred.hlsl"

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        // 0 - Stencil pass
        Pass
        {
            Name "Stencil Volume"

            ZTest LEQual
            ZWrite Off
            Cull Off
            ColorMask 0

            Stencil {
                Ref 0
                WriteMask 16
                ReadMask 255
                CompFront always
                PassFront keep
                ZFailFront invert
                CompBack always
                PassBack keep
                ZFailBack invert
            }

            HLSLPROGRAM

            #pragma vertex Vertex
            #pragma fragment Frag
//            #pragma enable_d3d11_debug_symbols

            struct Attributes
            {
                float4 vertex : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vertex(Attributes input)
            {
                float4 csPos = mul(UNITY_MATRIX_MVP, input.vertex);

                Varyings output;
                output.positionCS = csPos;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(1.0, 1.0, 1.0, 1.0);
            }
            ENDHLSL
        }

        // 1 - Point Light
        Pass
        {
            Name "Deferred Point Light"

            ZTest GEqual
            ZWrite Off
            Cull Front
            Blend One One, Zero One
            BlendOp Add, Add

            Stencil {
                Ref 16
                WriteMask 16
                ReadMask 16
                CompFront Equal
                PassFront zero
                FailFront zero
                ZFailFront zero
                CompBack Equal
                PassBack zero
                FailBack zero
                ZFailBack zero
            }

            HLSLPROGRAM

            #pragma vertex Vertex
            #pragma fragment PointLightShading
//            #pragma enable_d3d11_debug_symbols

            struct Attributes
            {
                float4 vertex : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vertex(Attributes input)
            {
                float4 csPos = mul(UNITY_MATRIX_MVP, input.vertex);

                Varyings output;
                output.positionCS = csPos;
                return output;
            }

            Texture2D g_DepthTex;
            float3 _LightWsPos;
            float _LightRadius;
            float3 _LightColor;

            half4 PointLightShading(Varyings input) : SV_Target
            {
                PointLightData light;
                light.WsPos = _LightWsPos;
                light.Radius= _LightRadius;
                light.Color = _LightColor;

                float2 clipCoord = input.positionCS.xy * _ScreenSize.zw * 2.0 - 1.0;

                #if UNITY_REVERSED_Z // TODO: can fold reversed_z into g_unproject parameters.
                float d = 1.0 - g_DepthTex.Load(int3(input.positionCS.xy, 0)).x;
                #else
                float d = g_DepthTex.Load(int3(input.positionCS.xy, 0)).x;
                #endif

                // Temporary code to calculate fragment world space position.
                float4 wsPos = mul(_InvCameraViewProj, float4(clipCoord, d * 2.0 - 1.0, 1.0));
                wsPos.xyz *= 1.0 / wsPos.w;

                float3 color = 0.0.xxx;

                // TODO calculate lighting.
                float3 L = light.WsPos - wsPos.xyz;
                half att = dot(L, L) < light.Radius*light.Radius ? 1.0 : 0.0;

                color += light.Color.rgb * att * 0.1;

                return half4(color, 0.0);
            }
            ENDHLSL
        }
    }
}
