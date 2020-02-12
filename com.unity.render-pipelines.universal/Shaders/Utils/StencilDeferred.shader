Shader "Hidden/Universal Render Pipeline/StencilDeferred"
{
    HLSLINCLUDE

    // _ADDITIONAL_LIGHT_SHADOWS is shader keyword globally enabled for a range of render-passes.
    // When rendering deferred lights, we need to set/unset this flag dynamically for each deferred
    // light, however there is no way to restore the value of the keyword, whch is needed by the
    // forward transparent pass. The workaround is to use a new shader keyword
    // _DEFERRED_ADDITIONAL_LIGHT_SHADOWS to set _ADDITIONAL_LIGHT_SHADOWS as a #define, so that
    // the "state" of the keyword itself is unchanged.
    #ifdef _DEFERRED_ADDITIONAL_LIGHT_SHADOWS
    #define _ADDITIONAL_LIGHT_SHADOWS 1
    #endif

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
        float4 posCS : TEXCOORD0;
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

        #if defined(_DIRECTIONAL) || defined(_FOG)
        output.positionCS = float4(positionOS.xy, UNITY_RAW_FAR_CLIP_VALUE, 1.0); // Force triangle to be on zfar
        #else
        VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS.xyz);
        output.positionCS = vertexInput.positionCS;
        #endif

        output.posCS = output.positionCS;

        return output;
    }

    TEXTURE2D_X(_DepthTex);
    //TEXTURE2D_X_HALF(_GBuffer0);
    UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF(0);
    //TEXTURE2D_X_HALF(_GBuffer1);
    UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF(1);

    //TEXTURE2D_X_HALF(_GBuffer2);
    UNITY_DECLARE_FRAMEBUFFER_INPUT_HALF(2);

    UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(3);

    float4x4 _ScreenToWorld;

    float3 _LightPosWS;
    float3 _LightColor;
    float4 _LightAttenuation; // .xy are used by DistanceAttenuation - .zw are used by AngleAttenuation *for SpotLights)
    float3 _LightDirection; // directional/spotLights support
    int _ShadowLightIndex;

    half4 FragWhite(Varyings input) : SV_Target
    {
        return half4(1.0, 1.0, 1.0, 1.0);
    }

    half4 DeferredShading(Varyings input) : SV_Target
    {
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float d        = UNITY_READ_FRAMEBUFFER_INPUT(3, input.positionCS.xy).x;//LOAD_TEXTURE2D_X(_DepthTex, input.positionCS.xy).x; // raw depth value has UNITY_REVERSED_Z applied on most platforms.
        half4 gbuffer0 = UNITY_READ_FRAMEBUFFER_INPUT(0, input.positionCS.xy);//LOAD_TEXTURE2D_X(_GBuffer0, input.positionCS.xy);
        half4 gbuffer1 = UNITY_READ_FRAMEBUFFER_INPUT(1, input.positionCS.xy);//LOAD_TEXTURE2D_X(_GBuffer1, input.positionCS.xy);
        half4 gbuffer2 = UNITY_READ_FRAMEBUFFER_INPUT(2, input.positionCS.xy);//LOAD_TEXTURE2D_X(_GBuffer2, input.positionCS.xy);

        #if !defined(USING_STEREO_MATRICES)
        // We can fold all this into 1 neat matrix transform, unless in XR Single Pass mode at the moment.
        float4 posWS = mul(_ScreenToWorld, float4(input.positionCS.xy, d, 1.0));
            posWS.xyz *= rcp(posWS.w);
        #else
            #if UNITY_REVERSED_Z
            d = 1.0 - d;
            #endif
            d = d * 2.0 - 1.0;
            float4 posCS = float4(input.posCS.xy, d * input.posCS.w, input.posCS.w);
            #if UNITY_UV_STARTS_AT_TOP
            posCS.y = -posCS.y;
            #endif
            float3 posWS = ComputeWorldSpacePosition(posCS, UNITY_MATRIX_I_VP);
        #endif

        InputData inputData = InputDataFromGbufferAndWorldPosition(gbuffer2, posWS.xyz);
        uint materialFlags = UnpackMaterialFlags(gbuffer0.a);
        bool materialReceiveShadowsOff = (materialFlags & kMaterialFlagReceiveShadowsOff) != 0;
        #if SHADER_API_SWITCH
        // Specular highlights are still silenced by setting specular to 0.0 during gbuffer pass and GPU timing is still reduced.
        bool materialSpecularHighlightsOff = false;
        #else
        bool materialSpecularHighlightsOff = (materialFlags & kMaterialFlagSpecularHighlightsOff);
        #endif

        Light unityLight;

        #if defined(_DIRECTIONAL)
            unityLight.direction = _LightDirection;
            unityLight.color = _LightColor.rgb;
            unityLight.distanceAttenuation = 1.0;
            unityLight.shadowAttenuation = 1.0; // TODO materialFlagReceiveShadows
        #else
            PunctualLightData light;
            light.posWS = _LightPosWS;
            light.radius2 = 0.0; //  only used by tile-lights.
            light.color = float4(_LightColor, 0.0);
            light.attenuation = _LightAttenuation;
            light.spotDirection = _LightDirection;
            light.shadowLightIndex = _ShadowLightIndex;
            unityLight = UnityLightFromPunctualLightDataAndWorldSpacePosition(light, posWS.xyz, materialReceiveShadowsOff);
        #endif

        half3 color = 0.0.xxx;

        #if defined(_LIT)
            BRDFData brdfData = BRDFDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);
            color = LightingPhysicallyBased(brdfData, unityLight, inputData.normalWS, inputData.viewDirectionWS, materialSpecularHighlightsOff);
        #elif defined(_SIMPLELIT)
            SurfaceData surfaceData = SurfaceDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2, kLightingSimpleLit);
            half3 attenuatedLightColor = unityLight.color * (unityLight.distanceAttenuation * unityLight.shadowAttenuation);
            half3 diffuseColor = LightingLambert(attenuatedLightColor, unityLight.direction, inputData.normalWS);
            half3 specularColor = LightingSpecular(attenuatedLightColor, unityLight.direction, inputData.normalWS, inputData.viewDirectionWS, half4(surfaceData.specular, surfaceData.smoothness), surfaceData.smoothness);
            // TODO: if !defined(_SPECGLOSSMAP) && !defined(_SPECULAR_COLOR), force specularColor to 0 in gbuffer code
            color = diffuseColor * surfaceData.albedo + specularColor;
        #endif

        return half4(color, 0.0);
    }

    half4 FragFog(Varyings input) : SV_Target
    {
       // float d = LOAD_TEXTURE2D_X(_DepthTex, input.positionCS.xy).x;
       //TODO: not working and gives 0
        float d = 1;//UNITY_READ_FRAMEBUFFER_INPUT(3, input.positionCS.xy).x;//LOAD_TEXTURE2D_X(_DepthTex, input.positionCS.xy).x;
        float z = LinearEyeDepth(d, _ZBufferParams);
        half fogFactor = ComputeFogFactorFromLinearEyeDepth(z);
        half fogIntensity = ComputeFogIntensity(fogFactor);
        return half4(unity_FogColor.rgb, fogIntensity);
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

            // Bit 4 is used for the stencil volume.
            Stencil {
                WriteMask 16
                ReadMask 16
                CompFront Always
                PassFront Keep
                ZFailFront Invert
                CompBack Always
                PassBack Keep
                ZFailBack Invert
            }

            HLSLPROGRAM

            #pragma multi_compile _ _SPOT
            #pragma multi_compile_fragment _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _DEFERRED_ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma vertex Vertex
            #pragma fragment FragWhite
            //#pragma enable_d3d11_debug_symbols

            ENDHLSL
        }

        // 1 - Deferred Punctual Light (Lit)
        Pass
        {
            Name "Deferred Punctual Light (Lit)"

            ZTest GEqual
            ZWrite Off
            Cull Front
            Blend One One, Zero One
            BlendOp Add, Add

            // [Stencil] Bit 4 is used for the stencil volume.
            // [Stencil] Bit 5-6 material type. 00 = unlit/bakedList, 01 = Lit, 10 = SimpleLit
            Stencil {
                Ref 48       // 0b00110000
                WriteMask 16 // 0b00010000
                ReadMask 112 // 0b01110000
                CompBack Equal
                PassBack Zero
                FailBack Keep
                ZFailBack Keep
            }

            HLSLPROGRAM

            #pragma multi_compile _POINT _SPOT
            #pragma multi_compile_fragment _LIT
            #pragma multi_compile_fragment _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _DEFERRED_ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma vertex Vertex
            #pragma fragment DeferredShading
            //#pragma enable_d3d11_debug_symbols

            ENDHLSL
        }

        // 2 - Deferred Punctual Light (SimpleLit)
        Pass
        {
            Name "Deferred Punctual Light (SimpleLit)"

            ZTest GEqual
            ZWrite Off
            Cull Front
            Blend One One, Zero One
            BlendOp Add, Add

            // [Stencil] Bit 4 is used for the stencil volume.
            // [Stencil] Bit 5-6 material type. 00 = unlit/bakedList, 01 = Lit, 10 = SimpleLit
            Stencil {
                Ref 80       // 0b01010000
                WriteMask 16 // 0b00010000
                ReadMask 112 // 0b01110000
                CompBack Equal
                PassBack Zero
                FailBack Keep
                ZFailBack Keep
            }

            HLSLPROGRAM

            #pragma multi_compile _POINT _SPOT
            #pragma multi_compile_fragment _SIMPLELIT
            #pragma multi_compile_fragment _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _DEFERRED_ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma vertex Vertex
            #pragma fragment DeferredShading
            //#pragma enable_d3d11_debug_symbols

            ENDHLSL
        }

        // 3 - Directional Light (Lit)
        Pass
        {
            Name "Deferred Directional Light (Lit)"

            ZTest Less
            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend One One, Zero One
            BlendOp Add, Add

            // [Stencil] Bit 4 is used for the stencil volume.
            // [Stencil] Bit 5-6 material type. 00 = unlit/bakedList, 01 = Lit, 10 = SimpleLit
            Stencil {
                Ref 32      // 0b00100000
                WriteMask 0 // 0b00000000
                ReadMask 96 // 0b01100000
                Comp Equal
                Pass Keep
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM

            #pragma multi_compile _DIRECTIONAL
            #pragma multi_compile_fragment _LIT
            #pragma multi_compile_fragment _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _DEFERRED_ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma vertex Vertex
            #pragma fragment DeferredShading
            //#pragma enable_d3d11_debug_symbols

            ENDHLSL
        }

        // 4 - Directional Light (SimpleLit)
        Pass
        {
            Name "Deferred Directional Light (SimpleLit)"

            ZTest Less
            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend One One, Zero One
            BlendOp Add, Add

            // [Stencil] Bit 4 is used for the stencil volume.
            // [Stencil] Bit 5-6 material type. 00 = unlit/bakedList, 01 = Lit, 10 = SimpleLit
            Stencil {
                Ref 64      // 0b01000000
                WriteMask 0 // 0b00000000
                ReadMask 96 // 0b01100000
                Comp Equal
                Pass Keep
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM

            #pragma multi_compile _DIRECTIONAL
            #pragma multi_compile_fragment _SIMPLELIT
            #pragma multi_compile_fragment _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _DEFERRED_ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma vertex Vertex
            #pragma fragment DeferredShading
            //#pragma enable_d3d11_debug_symbols

            ENDHLSL
        }

        // 5 - Legacy fog
        Pass
        {
            Name "Fog"

            ZTest Less
            ZTest NotEqual
            ZWrite Off
            Cull Off
            Blend OneMinusSrcAlpha SrcAlpha, Zero One
            BlendOp Add, Add

            HLSLPROGRAM

            #pragma multi_compile _FOG
            #pragma multi_compile FOG_LINEAR FOG_EXP FOG_EXP2

            #pragma vertex Vertex
            #pragma fragment FragFog
            //#pragma enable_d3d11_debug_symbols

            ENDHLSL
        }
    }
}
