Shader "Hidden/HDRenderPipeline/Test/DistortionMap"{
    Properties
    {
        _Size("Size", Float) = 1
        _DistortionAmplitude("Distortion Amplitude", Float) = 1
        _BlurMinAmplitude("Blur Min Amplitude", Range(0, 1)) = 0
        _BlurMaxAmplitude("Blur Max Amplitude", Range(0, 1)) = 1
        _NoiseAmplitude("Noise Amplitude", Range(0, 1)) = 0.5
        _NoiseLacunarity("Noise Lacunarity", Range(0, 10)) = 2
    }

    CGINCLUDE

    const float Pi = 3.14159265359;

    float _Size;
    float _DistortionAmplitude;
    float _BlurMinAmplitude;
    float _BlurMaxAmplitude;
    float _NoiseAmplitude;
    float _NoiseLacunarity;

    float rand(float v)
    {
        return frac(sin(v) * 1634242.2341);
    }

    float rand(float2 v, float2 o)
    {
        return rand(dot(v, o));
    }

    float noise(float2 v, float2 o)
    {
        float2 p = v;
        float2 i = floor(p);
        float2 f = frac(v);
        float2 u = f*f*(3.0 - 2.0*f);

        float f00 = rand(i, o);
        float f01 = rand(i + float2(0.0, 1.0), o);
        float f11 = rand(i + float2(1.0, 1.0), o);
        float f10 = rand(i + float2(1.0, 0.0), o);

        return lerp(f00, f10, u.x) +
            (f01 - f00)* u.y * (1.0 - u.x) +
            (f11 - f10) * u.x * u.y;
    }

    float3 noise3(float2 v)
    {
        const float2 _O1 = float2(0.734523, 0.235214);
        const float2 _O2 = float2(0.142378, 0.781423);
        const float2 _O3 = float2(0.891253, 0.534342);

        return float3(noise(v, _O1), noise(v, _O2), noise(v, _O3));
    }

    float3 fbm(float2 v, float a, float f)
    {
        float3 r = float3(0.0, 0.0, 0.0);

        for (int i = 0; i < 6 ; ++i)
        {
            r += a * noise3(v);
            v *= f;
            a *= a;
        }

        return r;
    }

    float3 turb(float2 v, float a, float f)
    {
        float3 r = float3(0.0, 0.0, 0.0);

        for (int i = 0; i < 6; ++i)
        {
            r += a * abs(noise3(v));
            v *= f;
            a *= a;
        }

        return r;
    }

    float3 ridge(float2 v, float a, float f, float3 n)
    {
        float3 r = float3(0.0, 0.0, 0.0);

        for (int i = 0; i < 6; ++i)
        {
            float3 t = n - abs(a * noise3(v));
            t *= t;
            r += t;

            v *= f;
            a *= a;
        }

        return r;
    }
    ENDCG

    SubShader
    {
        Lighting Off
        Blend One Zero

        Pass
        {
            Name "2DNoise"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float2 p = _Size * IN.globalTexcoord.xy;
                float3 s = fbm(p, _NoiseAmplitude, _NoiseLacunarity * (_SinTime.w * 0.1 + 1.0));

                float2 distortion = s.xy * _DistortionAmplitude;
                float blur = s.z * (_BlurMaxAmplitude - _BlurMinAmplitude) + _BlurMinAmplitude;

                return float4(distortion, blur, 1.0);
            }
            ENDCG
        }

        

        Pass
        {
            Name "Ripple"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float s = _Size * (cos(_Time.w) * 0.1 + 1);

                float2 distortion = float2(sin(IN.globalTexcoord.y * s) * _DistortionAmplitude, 0);

                float blur = 0;

                return float4(distortion, blur, 1.0);
            }
            ENDCG
        }
    }
}