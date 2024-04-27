Shader "Hidden/HDRP/DebugCapsuleTiles"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off
            Cull Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            #define DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"

            TEXTURE2D_X_UINT(_CapsuleShadowTileDebug);
            float4 _CapsuleShadowTileDebugParams;
            float4 _CapsuleShadowTileDebugParams2;

            #define _CapsuleShadowTileDebugUvScale  _CapsuleShadowTileDebugParams.xy
            #define _CapsuleShadowTileDebugTileSize uint2(_CapsuleShadowTileDebugParams2.xy)
            #define _CapsuleShadowTileDebugLimit    uint(_CapsuleShadowTileDebugParams2.z)

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID) * _CapsuleShadowTileDebugUvScale;
                return output;
            }

            float4 OverlayHeatMapNonPowerOf2(uint2 pixCoord, uint2 tileSize, uint n, uint maxN, float opacity)
            {
                int2 coord = (pixCoord % tileSize) - int2(tileSize.x/2-12, tileSize.y/2-7);
                float4 color = OverlayHeatMapColor(n, maxN, opacity);

                if (SampleDebugFontNumberAllDigits(coord, n))        // Shadow
                    color = float4(0, 0, 0, 1);
                if (SampleDebugFontNumberAllDigits(coord + 1, n))    // Text
                    color = float4(1, 1, 1, 1);

                return color;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                uint n = LOAD_TEXTURE2D_X(_CapsuleShadowTileDebug, uint2(input.uv)).x;

                float4 result = float4(0.f, 0.f, 0.f, 0.f);
                if (n > 0)
                    result = OverlayHeatMapNonPowerOf2(input.positionCS.xy, _CapsuleShadowTileDebugTileSize, n, _CapsuleShadowTileDebugLimit, 0.3f);
                return result;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
