Shader "Hidden/Universal Render Pipeline/StencilDeferred"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Deferred.hlsl"

    struct Attributes
    {
        float4 positionOS : POSITION;
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };

    #if defined(_SPOT)
    float4 _SpotLightScale;
    float4 _SpotLightBias;
    float4 _SpotLightGuard;
    #endif

    Varyings Vertex(Attributes input)
    {
        Varyings output = (Varyings)0;

        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_TRANSFER_INSTANCE_ID(input, output);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

        float3 positionOS = input.positionOS.xyz;

        #if defined(_SPOT)
        // Spot lights have an outer angle than can be up to 180 degrees, in which case the shape
        // becomes a capped hemisphere. There is no affine transforms to handle the particular cone shape,
        // so instead we will adjust the vertices positions in the vertex shader to get the tighest fit.
        [flatten] if (any(positionOS.xyz))
        {
            // The hemisphere becomes the rounded cap of the cone.
            positionOS.xyz = _SpotLightBias.xyz + _SpotLightScale.xyz * positionOS.xyz;
            positionOS.xyz = normalize(positionOS.xyz) * _SpotLightScale.w;
            // Slightly inflate the geometry to fit the analytic cone shape.
            // We want the outer rim to be expanded along xy axis only, while the rounded cap is extended along all axis.
            positionOS.xyz = (positionOS.xyz - float3(0, 0, _SpotLightGuard.w)) * _SpotLightGuard.xyz + float3(0, 0, _SpotLightGuard.w);
        }
        #endif

        #if defined(_DIRECTIONAL)
        output.positionCS = float4(positionOS.xy, UNITY_RAW_FAR_CLIP_VALUE, 1.0); // Force triangle to be on zfar
        #else
        VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS.xyz);
        output.positionCS = vertexInput.positionCS;
        #endif


        return output;
    }

    Texture2D _DepthTex;
    Texture2D _GBuffer0;
    Texture2D _GBuffer1;
    Texture2D _GBuffer2;
    float4x4 _ScreenToWorld;

    float3 _LightPosWS;
    float3 _LightColor;
    float4 _LightAttenuation; // .xy are used by DistanceAttenuation - .zw are used by AngleAttenuation *for SpotLights)
    float3 _LightDirection; // directional/spotLights support
    int _ShadowLightIndex;

    half4 DeferredShading(Varyings input) : SV_Target
    {
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        half3 color = 0.0.xxx;

        float d = _DepthTex.Load(int3(input.positionCS.xy, 0)).x; // raw depth value has UNITY_REVERSED_Z applied on most platforms.
        half4 gbuffer0 = _GBuffer0.Load(int3(input.positionCS.xy, 0));
        half4 gbuffer1 = _GBuffer1.Load(int3(input.positionCS.xy, 0));
        half4 gbuffer2 = _GBuffer2.Load(int3(input.positionCS.xy, 0));

        float4 posWS = mul(_ScreenToWorld, float4(input.positionCS.xy, d, 1.0));
        posWS.xyz *= 1.0 / posWS.w;

        SurfaceData surfaceData = SurfaceDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);
        InputData inputData = InputDataFromGbufferAndWorldPosition(gbuffer2, posWS.xyz);
        BRDFData brdfData;
        InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

        Light unityLight;

        #if _DIRECTIONAL
            unityLight.direction = _LightDirection;
            unityLight.color = _LightColor.rgb;
            unityLight.distanceAttenuation = 1.0;
            unityLight.shadowAttenuation = 1.0;
        #else
            PunctualLightData light;
            light.posWS = _LightPosWS;
            light.radius2 = 0.0; //  only used by tile-lights.
            light.color = float4(_LightColor, 0.0);
            light.attenuation = _LightAttenuation;
            light.spotDirection = _LightDirection;
            light.shadowLightIndex = _ShadowLightIndex;
            unityLight = UnityLightFromPunctualLightDataAndWorldSpacePosition(light, posWS.xyz);
        #endif

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

            #pragma multi_compile _ _SPOT
            #pragma multi_compile_fragment _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma vertex Vertex
            #pragma fragment Frag
            //#pragma enable_d3d11_debug_symbols

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(1.0, 1.0, 1.0, 1.0);
            }
            ENDHLSL
        }

        // 1 - Deferred Punctual Light
        Pass
        {
            Name "Deferred Punctual Light"

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

            #pragma multi_compile _POINT _SPOT
            #pragma multi_compile_fragment _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma vertex Vertex
            #pragma fragment DeferredShading
            //#pragma enable_d3d11_debug_symbols

            ENDHLSL
        }

        // 2 - Directional Light
        Pass
        {
            Name "Deferred Directional Light"

            ZTest Less
            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend One One, Zero One
            BlendOp Add, Add

            HLSLPROGRAM

            #pragma multi_compile _DIRECTIONAL
            #pragma multi_compile_fragment _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma vertex Vertex
            #pragma fragment DeferredShading
            //#pragma enable_d3d11_debug_symbols

            ENDHLSL
        }
    }
}
