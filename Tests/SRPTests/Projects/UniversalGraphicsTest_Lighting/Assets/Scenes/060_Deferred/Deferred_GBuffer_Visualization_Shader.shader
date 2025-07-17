Shader "Universal Render Pipeline/Deferred_GBuffer_Visualization_Shader"
{
    Properties
    {
        // BlendMode
        [HideInInspector]_Surface("__surface", Float) = 0.0
        [HideInInspector]_Blend("__mode", Float) = 0.0
        [HideInInspector]_Cull("__cull", Float) = 2.0
        [HideInInspector][ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _BlendOp("__blendop", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "IgnoreProjector" = "True"
            "UniversalMaterialType" = "Unlit"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        // -------------------------------------
        // Render State Commands
        Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
        ZWrite [_ZWrite]
        Cull [_Cull]

        Pass
        {
            Name "Deferred_GBuffer_Visualization"

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex GBufferVisPassVertex
            #pragma fragment GBufferVisPassFragment

            #pragma multi_compile_local_fragment _ GBUFFER_0 GBUFFER_1 GBUFFER_2 GBUFFER_ALPHA_A GBUFFER_ALPHA_B GBUFFER_RL GBUFFER_SM
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ SHADOWS_SHADOWMASK _DEFERRED_MIXED_LIGHTING
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferInput.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings GBufferVisPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);

                output.positionCS = pos;
                output.texcoord   = uv;

                return output;
            }

            void GBufferVisPassFragment(Varyings input, out half4 outColor : SV_Target0)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                outColor = float4(0, 0, 0, 1);

                half4 gBuffer0;
                half4 gBuffer1;
                half4 gBuffer2;
                float gBufferDepth;
                uint gBufferRenderingLayers;
                half4 gBufferShadowMask;

                uint2 positionSS = uint2(input.texcoord * (_ScreenSize.xy - uint2(1, 1)) + float2(0.5, 0.5));

                LoadGBuffers(positionSS, gBuffer0, gBuffer1, gBuffer2, gBufferDepth, gBufferRenderingLayers, gBufferShadowMask);

                #if defined(GBUFFER_0)
                    const uint mode = 0;
                #elif defined(GBUFFER_1)
                    const uint mode = 1;
                #elif defined(GBUFFER_2)
                    const uint mode = 2;
                #elif defined(GBUFFER_ALPHA_A)
                    const uint mode = 3;
                #elif defined(GBUFFER_ALPHA_B)
                    const uint mode = 4;
                #elif defined(GBUFFER_RL) && defined(GBUFFER_FEATURE_RENDERING_LAYERS)
                    const uint mode = 5;
                #elif defined(GBUFFER_SM) && defined(GBUFFER_FEATURE_SHADOWMASK)
                    const uint mode = 6;
                #else
                    const uint mode = input.positionCS.x / 8 % 7;
                #endif

                switch (mode)
                {
                default: // 0
                    outColor = gBuffer0;
                    break;
                case 1:
                    outColor = gBuffer1;
                    break;
                case 2:
                    outColor.rgb = UnpackGBufferNormal(gBuffer2.rgb) * 0.5 + 0.5; // Unpack normal & normalize 0..1
                    break;
                case 3:
                    outColor.r = gBuffer0.a;
                    outColor.g = 1.0 - gBuffer1.a; // Occlusion, invert for sanity
                    outColor.b = gBuffer2.a;
                    break;
                case 4:
                    outColor.r = Linear01DepthFromNear(gBufferDepth, _ZBufferParams); // Linear depth
                    #if defined(GBUFFER_FEATURE_SHADOWMASK)
                        outColor.g = gBufferShadowMask.a; // Shadow mask alpha channel
                    #endif
                    #if defined(GBUFFER_FEATURE_RENDERING_LAYERS)
                        outColor.b = half((gBufferRenderingLayers >> 24) & uint(255)) / (half)255.0; // Rendering Layers upper bits (only matters for 32bit RL)
                    #endif
                    break;
                case 5:
                    #if defined(GBUFFER_FEATURE_RENDERING_LAYERS)
                        outColor.r = half((gBufferRenderingLayers >> 0)  & uint(255)) / (half)255.0; // Unpack uint32 -> unorm8888
                        outColor.g = half((gBufferRenderingLayers >> 8)  & uint(255)) / (half)255.0;
                        outColor.b = half((gBufferRenderingLayers >> 16) & uint(255)) / (half)255.0;
                    #endif
                    break;
                case 6:
                    #if defined(GBUFFER_FEATURE_SHADOWMASK)
                        outColor = gBufferShadowMask;
                    #endif
                    break;
                }

                // The testframework / refimages don't handle non-transparent alpha well,
                // force alpha to 1 for visual test purposes.
                // Comment out to check GBuffer alpha channels.
                outColor.a = 1;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
