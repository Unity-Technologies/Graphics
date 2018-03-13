Shader "Hidden/LightweightPipeline/Sampling"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "../ShaderLibrary/Core.hlsl"

    struct VertexInput
    {
        float4 vertex   : POSITION;
        float2 texcoord : TEXCOORD0;
    };

    struct Interpolators
    {
        half4  pos      : SV_POSITION;
        half4  texcoord : TEXCOORD0;
    };

    Interpolators Vertex(VertexInput i)
    {
        Interpolators o;
        UNITY_SETUP_INSTANCE_ID(i);
        UNITY_TRANSFER_INSTANCE_ID(i, o);

        o.pos = TransformObjectToHClip(i.vertex.xyz);

        float4 projPos = o.pos * 0.5;
        projPos.xy = projPos.xy + projPos.w;

        o.texcoord.xy = i.texcoord;
        o.texcoord.zw = projPos.xy;

        return o;
    }

    half4 DownsampleBox4Tap(Texture2D tex, SamplerState samplerTex, float2 uv, float2 texelSize, float amount)
    {
        float4 d = texelSize.xyxy * float4(-amount, -amount, amount, amount);

        half4 s;
        s =  (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xy));
        s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zy));
        s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.xw));
        s += (SAMPLE_TEXTURE2D(tex, samplerTex, uv + d.zw));

        return s * (1.0 / 4.0);
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}
        LOD 100

        // 0 - Downsample - Box filtering
        Pass
        {
            Tags { "LightMode" = "LightweightForward"}

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

            half4 FragBoxDownsample(Interpolators i) : SV_Target
            {
                half4 col = DownsampleBox4Tap(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy, 1.0);
                return half4(col.rgb, 1);
            }
            ENDHLSL
        }

        // 1 - 2x Downsample - Box filtering
        Pass
        {
            Tags { "LightMode" = "LightweightForward"}

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

            half4 FragBoxDownsample(Interpolators i) : SV_Target
            {
                half4 col = DownsampleBox4Tap(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, _MainTex_TexelSize.xy, 2.0);
                return half4(col.rgb, 1);
            }
            ENDHLSL
        }
    }
}
