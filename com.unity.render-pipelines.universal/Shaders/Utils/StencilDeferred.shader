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
            //#pragma enable_d3d11_debug_symbols

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
            //#pragma enable_d3d11_debug_symbols

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;

                return output;
            }

            Texture2D _DepthTex;
            Texture2D _GBuffer0;
            Texture2D _GBuffer1;
            Texture2D _GBuffer2;
            float4x4 _ScreenToWorld;

            float3 _LightWsPos;
            float _LightRadius2;
            float3 _LightColor;
            float4 _LightAttenuation; // .xy are used by DistanceAttenuation - .zw are used by AngleAttenuation *for SpotLights)
            float3 _LightSpotDirection; // spotLights support

            half4 PointLightShading(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                PointLightData light;
                light.wsPos = _LightWsPos;
                light.radius2 = _LightRadius2;
                light.color = float4(_LightColor, 0.0);
                light.attenuation = _LightAttenuation;
                light.spotDirection = _LightSpotDirection;

                float3 color = 0.0.xxx;

                float d = _DepthTex.Load(int3(input.positionCS.xy, 0)).x; // raw depth value has UNITY_REVERSED_Z applied on most platforms.
                half4 gbuffer0 = _GBuffer0.Load(int3(input.positionCS.xy, 0));
                half4 gbuffer1 = _GBuffer1.Load(int3(input.positionCS.xy, 0));
                half4 gbuffer2 = _GBuffer2.Load(int3(input.positionCS.xy, 0));

                // Temporary(?) code to calculate fragment world space position.
                float4 wsPos = mul(_ScreenToWorld, float4(input.positionCS.xy, d, 1.0));
                wsPos.xyz *= 1.0 / wsPos.w;

                SurfaceData surfaceData = SurfaceDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);
                InputData inputData = InputDataFromGbufferAndWorldPosition(gbuffer2, wsPos.xyz);
                BRDFData brdfData;
                InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

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
                return half4(color, 0.0);
            }
            ENDHLSL
        }
    }
}
