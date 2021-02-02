Shader "Custom/RenderFeature/KawaseBlur"
{
    //Properties
    //{
    //    _BaseMap("Texture", 2D) = "white" {}
    //}

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag


            struct Attributes
            {
                float4 positionCS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D_X(_BaseMap);
            SamplerState sampler_BaseMap;
            float4 _BaseMap_TexelSize;
            float4 _BaseMap_sampler_ST;

            float _offset;

            #define SAMPLE_BASEMAP(uv)   SAMPLE_TEXTURE2D_X(_BaseMap, sampler_BaseMap, UnityStereoTransformScreenSpaceTex(uv)).rgb;

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = float4(input.positionCS.xyz, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                output.positionCS.y *= -1;
                #endif
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 res = _BaseMap_TexelSize.xy;
                float i = _offset;

                half4 col;
                col.w = 1.0f;

                col.rgb  = SAMPLE_BASEMAP(input.uv);
                col.rgb += SAMPLE_BASEMAP(input.uv + float2( i,  i) * res);
                col.rgb += SAMPLE_BASEMAP(input.uv + float2( i, -i) * res);
                col.rgb += SAMPLE_BASEMAP(input.uv + float2(-i,  i) * res);
                col.rgb += SAMPLE_BASEMAP(input.uv + float2(-i, -i) * res);
                col.rgb /= 5.0f;

                return col;
            }
            ENDHLSL
        }
    }
}
