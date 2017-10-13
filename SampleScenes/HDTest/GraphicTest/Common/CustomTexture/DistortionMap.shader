Shader "Hidden/HDRenderPipeline/Test/DistortionMap"{
    Properties
    {
        _Size("Size", Float) = 1
        _DistortionAmplitude("Distortion Amplitude", Float) = 1
        _NoiseAmplitude("Noise Amplitude", Range(0, 1)) = 0.5
        _NoiseLacunarity("Noise Lacunarity", Range(0, 10)) = 2
    }

    CGINCLUDE

    const float Pi = 3.14159265359;

    float _Size;
    float _DistortionAmplitude;
    float _NoiseAmplitude;
    float _NoiseLacunarity;

    float3 permute(float3 x) { return ((x*34.0) + 1.0)*x % 289.0; }

    float2 rand2(float2 p)
    {
        return frac(sin(float2(dot(p, float2(64321.9843, 8143.18321)), dot(p, float2(8312.153, 3218.1984)))) * 13218.2165);
    }

    #define SDF_VORONOI(pointTransform, name) float name(float2 value)\
    {\
        float2 o = floor(value);\
        float2 f = frac(value);\
        float r = 1;\
        for (int y = -1; y <= 1; ++y)\
        {\
            for (int x = -1; x <= 1; ++x)\
            {\
                float2 cell = float2(float(x), float(y));\
                float2 pCS = rand2(cell + o);\
                pCS = pointTransform(pCS);\
                float2 diff = cell + pCS - f;\
                float dist = length(diff);\
                    \
                r = min(r, dist);\
            }\
        }\
        \
        return r;\
    }

    #define SDF_GRADIENT(sdf, name) float2 name(float2 value)\
    {\
        const float2 d = float2(0.0, 0.0001);\
        float f00 = sdf(value + d.xx);\
        float f01 = sdf(value + d.xy);\
        float f10 = sdf(value + d.yx);\
        float f11 = sdf(value + d.yy);\
        return float2((f10 - f00 + f11 - f10) * 0.5, (f01 - f00 + f11 - f10) * 0.5);\
    }

    float snoise(float2 v) {
        const float4 C = float4(0.211324865405187, 0.366025403784439,
            -0.577350269189626, 0.024390243902439);
        float2 i = floor(v + dot(v, C.yy));
        float2 x0 = v - i + dot(i, C.xx);
        float2 i1;
        i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
        float4 x12 = x0.xyxy + C.xxzz;
        x12.xy -= i1;
        i = i % 289.0;
        float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0))
            + i.x + float3(0.0, i1.x, 1.0));
        float3 m = max(0.5 - float3(dot(x0, x0), dot(x12.xy, x12.xy),
            dot(x12.zw, x12.zw)), 0.0);
        m = m*m;
        m = m*m;
        float3 x = 2.0 * frac(p * C.www) - 1.0;
        float3 h = abs(x) - 0.5;
        float3 ox = floor(x + 0.5);
        float3 a0 = x - ox;
        m *= 1.79284291400159 - 0.85373472095314 * (a0*a0 + h*h);
        float3 g;
        g.x = a0.x  * x0.x + h.x  * x0.y;
        g.yz = a0.yz * x12.xz + h.yz * x12.yw;
        return 130.0 * dot(m, g);
    }

    float4 permute(float4 x) { return ((x*34.0) + 1.0)*x % 289.0; }
    float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - 0.85373472095314 * r; }

    float snoise(float3 v) {
        const float2  C = float2(1.0 / 6.0, 1.0 / 3.0);
        const float4  D = float4(0.0, 0.5, 1.0, 2.0);

        // First corner
        float3 i = floor(v + dot(v, C.yyy));
        float3 x0 = v - i + dot(i, C.xxx);

        // Other corners
        float3 g = step(x0.yzx, x0.xyz);
        float3 l = 1.0 - g;
        float3 i1 = min(g.xyz, l.zxy);
        float3 i2 = max(g.xyz, l.zxy);

        //  x0 = x0 - 0. + 0.0 * C 
        float3 x1 = x0 - i1 + 1.0 * C.xxx;
        float3 x2 = x0 - i2 + 2.0 * C.xxx;
        float3 x3 = x0 - 1. + 3.0 * C.xxx;

        // Permutations
        i = i % 289.0;
        float4 p = permute(permute(permute(
            i.z + float4(0.0, i1.z, i2.z, 1.0))
            + i.y + float4(0.0, i1.y, i2.y, 1.0))
            + i.x + float4(0.0, i1.x, i2.x, 1.0));

        // Gradients
        // ( N*N points uniformly over a square, mapped onto an octahedron.)
        float n_ = 1.0 / 7.0; // N=7
        float3  ns = n_ * D.wyz - D.xzx;

        float4 j = p - 49.0 * floor(p * ns.z *ns.z);  //  mod(p,N*N)

        float4 x_ = floor(j * ns.z);
        float4 y_ = floor(j - 7.0 * x_);    // mod(j,N)

        float4 x = x_ *ns.x + ns.yyyy;
        float4 y = y_ *ns.x + ns.yyyy;
        float4 h = 1.0 - abs(x) - abs(y);

        float4 b0 = float4(x.xy, y.xy);
        float4 b1 = float4(x.zw, y.zw);

        float4 s0 = floor(b0)*2.0 + 1.0;
        float4 s1 = floor(b1)*2.0 + 1.0;
        float4 sh = -step(h, float4(0.0, 0.0, 0.0, 0.0));

        float4 a0 = b0.xzyw + s0.xzyw*sh.xxyy;
        float4 a1 = b1.xzyw + s1.xzyw*sh.zzww;

        float3 p0 = float3(a0.xy, h.x);
        float3 p1 = float3(a0.zw, h.y);
        float3 p2 = float3(a1.xy, h.z);
        float3 p3 = float3(a1.zw, h.w);

        //Normalise gradients
        float4 norm = taylorInvSqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
        p0 *= norm.x;
        p1 *= norm.y;
        p2 *= norm.z;
        p3 *= norm.w;

        // Mix final noise value
        float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
        m = m * m;
        return 42.0 * dot(m*m, float4(dot(p0, x0), dot(p1, x1),
            dot(p2, x2), dot(p3, x3)));
    }

    float3 fbm2x3(float2 v, float a, float f)
    {
        float3 r = float3(0.0, 0.0, 0.0);

        for (int i = 0; i < 6; ++i)
        {
            r += a * float3(snoise(v), snoise(v * 1.18142), snoise(v * 0.974236));
            v *= f;
            a *= a;
        }

        return r;
    }

    float fbm3(float3 v, float a, float f)
    {
        float r = float(0.0);

        for (int i = 0; i < 6; ++i)
        {
            r += a * snoise(v);
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
                float3 s = fbm2x3(p, _NoiseAmplitude, _NoiseLacunarity * (_SinTime.w * 0.1 + 1.0));

                float2 distortion = s.xy * _DistortionAmplitude;
                float blur = s.z;

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
                float s = _Size * (cos(_Time.w * 0.5) * 0.1 + 1);

                float3 n3 = fbm2x3(IN.globalTexcoord.xy * 5, _NoiseAmplitude, _NoiseLacunarity);
                float n = fbm3(n3 + IN.globalTexcoord.xyx + _Time.w * 0.1, _NoiseAmplitude, _NoiseLacunarity);
                float t = sin(n * 5 + _Time.w * 0.5) * 0.5 + 0.5;

                float2 distortion = sin(n3.xy * s) * _DistortionAmplitude;
                
                float blur = (min(t, 1-t) * 2);

                return float4(distortion, blur, 1.0);
            }
            ENDCG
        }

        Pass
        {
            Name "Voronoi"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            float2 circlePoint(float2 p)
            {
                p = 0.5 + 0.5 * sin(_Time.w * 0.2 + 6.2831 * p);
                return p;
            }

            SDF_VORONOI(circlePoint, sdfVoronoi)
            SDF_GRADIENT(sdfVoronoi, sdfVoronoiGrad)

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float2 uv = IN.globalTexcoord.xy * _Size;
                float v = sdfVoronoi(uv);
                v = v * v * v;

                float2 distortion = sdfVoronoiGrad(uv);

                float blur = v;

                return float4(distortion, blur, 1.0);
            }
            ENDCG
        }
    }
}