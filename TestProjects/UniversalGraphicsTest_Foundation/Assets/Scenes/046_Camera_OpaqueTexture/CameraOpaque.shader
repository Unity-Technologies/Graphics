Shader "Unlit/CameraOpaque"
{
    Properties
    {

    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenUV : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs vertexInputs = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexInputs.positionCS;
                o.uv = v.uv;
                o.screenUV = vertexInputs.positionNDC;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // sample the texture
                half2 screenUV = i.screenUV.xy / i.screenUV.w;
                half v = 0.05;
                screenUV += (frac(screenUV * 80) * v) - v * 0.5;
                half4 col = half4(-0.05, 0, -0.05, 1);
                col.r += SampleSceneColor(((screenUV - 0.5) * 1.1) + 0.5).r;
                col.g += SampleSceneColor(screenUV).g;
                col.b += SampleSceneColor(((screenUV - 0.5) * 0.9) + 0.5).b;
                return col;
            }
            ENDHLSL
        }
    }
}
