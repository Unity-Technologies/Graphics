Shader "Hidden/Lightweight Render Pipeline/Sampling"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"

    struct Attributes
    {
        float4 positionOS   : POSITION;
        float2 texcoord     : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4  positionCS  : SV_POSITION;
        float2  uv          : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    Varyings Vertex(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_TRANSFER_INSTANCE_ID(input, output);

        output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
        output.uv = input.texcoord;
        return output;
    }

    half4 DownsampleBox4Tap(TEXTURE2D_PARAM(tex, samplerTex), float2 uv, float2 texelSize, float amount)
    {
        float4 d = texelSize.xyxy * float4(-amount, -amount, amount, amount);

        half4 s;
        s =  (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xy));
        s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zy));
        s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xw));
        s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zw));

        return s * 0.25h;
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}
        LOD 100

        // 0 - Downsample - Box filtering
        Pass
        {
            Name "Default"
            ZTest Always
            ZWrite Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma vertex Vertex
            #pragma fragment FragBoxDownsample

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            float _SampleOffset;

            half4 FragBoxDownsample(Varyings input) : SV_Target
            {
                half4 col = DownsampleBox4Tap(TEXTURE2D_ARGS(_MainTex, sampler_MainTex), input.uv, _MainTex_TexelSize.xy, _SampleOffset);
                return half4(col.rgb, 1);
            }
            ENDHLSL
        }
    }
}
