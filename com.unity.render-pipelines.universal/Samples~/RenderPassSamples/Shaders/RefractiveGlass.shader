Shader "Custom/RefractiveGlass"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "white" {}
        _NormalStrength ("Normal Strength", Range(0,1000)) = 0.0
        _Alpha ("Alpha", Range(0,1)) = 0.0
        [Toggle] _UseRenderPass ("Use Render Pass", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline" }
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float4 uvgrab   : TEXCOORD0;
                float2 uvBase   : TEXCOORD1;
                float2 uvNormal : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SamplerState sampler_BaseMap;
            TEXTURE2D(_NormalMap);
            SamplerState sampler_NormalMap;

            TEXTURE2D_X(_GrabBlurTexture);
            SamplerState sampler_GrabBlurTexture;
            float4 _GrabBlurTexture_TexelSize;

            float4 _BaseMap_ST;
            float4 _NormalMap_ST;

            float _NormalStrength;
            float _Alpha;
            int _UseRenderPass;

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
#if UNITY_UV_STARTS_AT_TOP
                float scale = -1.0;
#else
                float scale = 1.0;
#endif
                o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
                o.uvgrab.zw = o.vertex.zw;

                o.uvBase   = TRANSFORM_TEX(v.uv, _BaseMap);
                o.uvNormal = TRANSFORM_TEX(v.uv, _NormalMap);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // calculate perturbed coordinates
                // we could optimize this by just reading the x & y without reconstructing the Z
                half2 n = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap,
                                                        input.uvNormal)).rg;
                float2 offset = n * _NormalStrength * _GrabBlurTexture_TexelSize.xy;
                input.uvgrab.xy = offset * input.uvgrab.z + input.uvgrab.xy;

                half4 grabCol = SAMPLE_TEXTURE2D_X(_GrabBlurTexture, sampler_GrabBlurTexture,
                                               UnityStereoTransformScreenSpaceTex(input.uvgrab.xy/input.uvgrab.w));
                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap,
                                                        input.uvBase);
                if (_UseRenderPass)
                {
                    baseCol = lerp(grabCol, baseCol, _Alpha);
                    baseCol.w = 1;
                }
                else
                {
                    baseCol.w = _Alpha;
                }
                return baseCol;
            }

            ENDHLSL
        }
    }
}
