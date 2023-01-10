Shader "Hidden/GUITextureBlit2SRGB" {
    Properties
    {
        _MainTex ("Texture", any) = "" {}
        _Color("Multiplicative color", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass {
            ZTest Always Cull Off ZWrite Off

            // Shader slightly adapted from the builtin renderer
            // It can consume an exposure texture to setup the exposure in the render

            HLSLPROGRAM
            #pragma editor_sync_compilation
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/EditorShaderVariables.hlsl"

            TEXTURE2D(_MainTex);
            uniform float4 _MainTex_ST;
            uniform float4 _Color;
            uniform float _MipLevel;
            uniform float _Exposure;
            uniform bool _ManualTex2SRGB;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = mul(unity_MatrixVP, v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 colTex = SAMPLE_TEXTURE2D_LOD(_MainTex, s_linear_clamp_sampler, i.texcoord, _MipLevel);
                return colTex * _Color * exp2(_Exposure);
            }
            ENDHLSL

        }
    }
    Fallback Off
}
