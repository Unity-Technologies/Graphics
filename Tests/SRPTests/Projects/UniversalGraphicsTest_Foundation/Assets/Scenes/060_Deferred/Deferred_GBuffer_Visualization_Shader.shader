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

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            #pragma multi_compile_fragment _ VIS_ALPHA

            TEXTURE2D_X(_GBuffer0);
            TEXTURE2D_X(_GBuffer1);
            TEXTURE2D_X(_GBuffer2);
            TEXTURE2D_X(_GBuffer3);

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

                outColor = float4(1,1,0,1);

                float2 uv = input.texcoord;
                #ifndef UNITY_UV_STARTS_AT_TOP
                    uv.y = 1.0 - uv.y;
                #endif
                // Bottom row.
                if(all(uv < float2(0.5, 0.5))) // GBuffer2 == normal.rgb, smoothness
                {
                    float2 uvX = saturate(uv * 2);
                    outColor = SAMPLE_TEXTURE2D_X_LOD(_GBuffer2, sampler_PointClamp, uvX, 0);
                }
                else if(uv.x >= 0.5 && uv.y < 0.5) // GBuffer3 == GI.rgb
                {
                    float2 uvX = saturate((uv - float2(0.5, 0.0)) * 2);
                    outColor = SAMPLE_TEXTURE2D_X_LOD(_GBuffer3, sampler_PointClamp, uvX, 0);
                }
                // Top row.
                else if(uv.x < 0.5 && uv.y >= 0.5) // GBuffer0 == Color.rgb, material flags
                {
                    float2 uvX = saturate((uv - float2(0.0, 0.5)) * 2);
                    outColor = SAMPLE_TEXTURE2D_X_LOD(_GBuffer0, sampler_PointClamp, uvX, 0);
                }
                else if(all(uv >= float2(0.5, 0.5))) // Gbuffer1 == Metallic.r/Specular.rgb, ambient occlusion
                {
                    float2 uvX = saturate((uv - float2(0.5, 0.5)) * 2);
                    outColor = SAMPLE_TEXTURE2D_X_LOD(_GBuffer1, sampler_PointClamp, uvX, 0);
                }

                // Visualize the alpha channel in rgb.
                #ifdef VIS_ALPHA
                outColor.rgb = outColor.aaa;
                #endif

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
