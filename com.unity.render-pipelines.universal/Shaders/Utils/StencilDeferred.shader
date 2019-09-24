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
                float4 csPos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.vertex));

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
                float4 csPos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.vertex));
                

                Varyings output;
                output.positionCS = csPos;
                return output;
            }

            Texture2D _DepthTex;
            Texture2D _GBuffer0;
            Texture2D _GBuffer1;
            Texture2D _GBuffer2;
            Texture2D _GBuffer3;
            Texture2D _GBuffer4;

            float3 _LightWsPos;
            float _LightRadius;
            float3 _LightColor;

            half4 PointLightShading(Varyings input) : SV_Target
            {
                PointLightData light;
                light.wsPos = _LightWsPos;
                light.radius = _LightRadius;
                light.color = float4(_LightColor, 0.0);

                float2 clipCoord = input.positionCS.xy * _ScreenSize.zw * 2.0 - 1.0;

                #if UNITY_REVERSED_Z // TODO: can fold reversed_z into g_unproject parameters.
                float d = 1.0 - _DepthTex.Load(int3(input.positionCS.xy, 0)).x;
                #else
                float d = _DepthTex.Load(int3(input.positionCS.xy, 0)).x;
                #endif

                // Temporary code to calculate fragment world space position.
                float4 wsPos = mul(_InvCameraViewProj, float4(clipCoord, d * 2.0 - 1.0, 1.0));
                wsPos.xyz *= 1.0 / wsPos.w;

                float3 color = 0.0.xxx;

#if TEST_WIP_DEFERRED_POINT_LIGHTING
                half4 gbuffer0 = _GBuffer0.Load(int3(input.positionCS.xy, 0));
                half4 gbuffer1 = _GBuffer1.Load(int3(input.positionCS.xy, 0));
                half4 gbuffer2 = _GBuffer2.Load(int3(input.positionCS.xy, 0));
                half4 gbuffer3 = _GBuffer3.Load(int3(input.positionCS.xy, 0));
                half4 gbuffer4 = _GBuffer4.Load(int3(input.positionCS.xy, 0));

                SurfaceData surfaceData = SurfaceDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2, gbuffer3);
                InputData inputData = InputDataFromGbufferAndWorldPosition(gbuffer2, gbuffer4, wsPos.xyz);
                BRDFData brdfData;
                InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

                // TODO re-use _GBuffer4 as base RT instead?
                // TODO Do this in a separate pass?
                color = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.normalWS, inputData.viewDirectionWS);

                Light unityLight = UnityLightFromPointLightDataAndWorldSpacePosition(light, wsPos.xyz);
                color += LightingPhysicallyBased(brdfData, unityLight, inputData.normalWS, inputData.viewDirectionWS);

            #if 0 // Temporary debug output
                // TO CHECK (does Forward support works??):
                //color.rgb = surfaceData.emission;

                // TO REVIEW (needed for cutouts?):
                //color.rgb = half3(surfaceData.alpha, surfaceData.alpha, surfaceData.alpha);

                // TODO (approach for shadows?)
                //color.rgb = inputData.shadowCoord.xyz;
                // TO REVIEW (support those? fog passed with VertexLight in forward)
                //color.rgb = half3(inputData.fogCoord, inputData.fogCoord, inputData.fogCoord);
                //color.rgb = inputData.vertexLighting;
            #endif

#else
                float4 albedoOcc = _GBuffer0.Load(int3(input.positionCS.xy, 0));
                float4 normalRoughness = _GBuffer1.Load(int3(input.positionCS.xy, 0));
                float4 spec = _GBuffer2.Load(int3(input.positionCS.xy, 0));

                // TODO calculate lighting.
                float3 L = light.wsPos - wsPos.xyz;
                half att = dot(L, L) < light.radius*light.radius ? 1.0 : 0.0;

                color += light.color.rgb * att * 0.1; // + (albedoOcc.rgb + normalRoughness.rgb + spec.rgb) * 0.001 + half3(albedoOcc.a, normalRoughness.a, spec.a) * 0.01;
#endif
                return half4(color, 0.0);
            }
            ENDHLSL
        }
    }
}
