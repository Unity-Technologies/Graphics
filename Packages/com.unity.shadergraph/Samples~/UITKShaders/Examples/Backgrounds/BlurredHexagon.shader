Shader "Unlit/UITKShaders/BlurredHexagon"
{
    // A UI Toolkit custom backdrop-filter port of the UGUI sample's "Blurred Hexagon": the
    // captured backdrop (_MainTex) is spiral-blurred INSIDE each hexagon cell (frosted-glass
    // tiles), graded by saturation and tint, with soft white borders between cells that can be
    // dissolved per cell. Mirrors the UGUI graph's building blocks — the SpiralBlur custom
    // function of the "Scene Color Blurred" helper and the "Hexagon Tiles" border model
    // (border distance / thickness / softness) — as a post-process shader (NOT UI Shader
    // Graph) per UITK's filter model.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HexTiles ("Hex Tiles", Float) = 10
        _BlurTexels ("Blur", Float) = 3.5
        _Dissolve ("Dissolve", Float) = 0.98
        _Saturation ("Saturation", Float) = 1
        _BorderDistance ("Border Distance", Float) = 0.42
        _BorderThickness ("Border Thickness", Float) = 0.01
        _BorderSoftness ("Border Softness", Float) = 0.05
        _BorderOpacity ("Border Opacity", Float) = 1
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Blend One Zero
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _UIE_OUTPUT_LINEAR

            #include "UnityCG.cginc"
            #include "UnityUIEFilter.cginc"

            // Spiral pattern matching the UGUI demo's settings (4 cycles x 3 samples): the low
            // tap count is deliberate — with the per-pixel jitter it produces the original's
            // coarse frosted-glass grain rather than a smooth gaussian.
            #define BLUR_CYCLES 4
            #define BLUR_SAMPLES_PER_CYCLE 3

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                uint rectIndex : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            float _HexTiles;
            float _BlurTexels;
            float _Dissolve;
            float _Saturation;
            float _BorderDistance;
            float _BorderThickness;
            float _BorderSoftness;
            float _BorderOpacity;
            half4 _Tint;

            v2f vert (FilterVertexInput v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.rectIndex = GetFilterRectIndex(v);
                return o;
            }

            float2 NormalizeUVs(float2 uv, float4 uvRect)
            {
                return float2((uv.x - uvRect.x) / uvRect.z, (uv.y - uvRect.y) / uvRect.w);
            }

            // Distance to a pointy-top hex boundary from a point local to the hex centre.
            float HexDist(float2 p)
            {
                p = abs(p);
                return max(dot(p, normalize(float2(1.0, 1.7320508))), p.x);
            }

            // Deterministic per-cell random for the dissolve threshold.
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Spiral blur of the backdrop, ported from the UGUI sample's SpiralBlurCustomFunction:
            // per cycle the sample angle sweeps a turn while the radius steps outward, giving a
            // soft even frost. The radius is a PERCENTAGE of the element size (like the UGUI
            // helper's UV-fraction Blurriness), so the frost reads the same at any element size.
            // Samples are clamped inside the element's atlas sub-rect.
            half4 SpiralBlur(float2 uv, float4 uvRect)
            {
                float2 lo = float2(uvRect.x, uvRect.y);
                float2 hi = float2(uvRect.x + uvRect.z, uvRect.y + uvRect.w);
                float radius = max(0.0, _BlurTexels) * 0.01;
                float2 stepSize = float2(uvRect.z, uvRect.w) * (radius / BLUR_CYCLES);

                half4 sum = tex2D(_MainTex, uv);
                float count = 1.0;

                // Per-pixel jitter, like the original's noise-driven sampling: rotating and
                // scaling each pixel's tap pattern randomly turns the fixed-pattern banding into
                // the characteristic frosted-glass grain (the visible "dots" in the frost).
                float jitter = Hash21(uv * _MainTex_TexelSize.zw) - 0.5;

                [unroll] for (int c = 1; c <= BLUR_CYCLES; c++)
                {
                    [unroll] for (int s = 0; s < BLUR_SAMPLES_PER_CYCLE; s++)
                    {
                        // Offset alternate cycles by half a step so taps interleave.
                        float angle = (s + 0.5 * (c & 1) + jitter) * (6.2831853 / BLUR_SAMPLES_PER_CYCLE);
                        float2 offset = float2(cos(angle), sin(angle)) * stepSize * (c + jitter * 0.75);
                        sum += tex2D(_MainTex, clamp(uv + offset, lo, hi));
                        count += 1.0;
                    }
                }
                return sum / count;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 uvRect = GetFilterUVRect(i.rectIndex);

                // Hexagon lattice (aspect-corrected so the cells stay regular). Positive-domain
                // fmod wrap: huv - h can be negative in the first half-cell band and HLSL fmod
                // keeps the dividend's sign, which would pick the wrong nearest cell there.
                float2 uv01 = NormalizeUVs(i.uv, uvRect);
                float aspect = (uvRect.z * _MainTex_TexelSize.z) / max(1.0, uvRect.w * _MainTex_TexelSize.w);
                float2 huv = float2(uv01.x * aspect, uv01.y) * max(1.0, _HexTiles);

                float2 r = float2(1.0, 1.7320508);
                float2 h = r * 0.5;
                float2 a = fmod(huv, r) - h;
                float2 b = fmod(huv - h + r, r) - h;
                float2 gv = dot(a, a) < dot(b, b) ? a : b;

                // Frosted interior: spiral-blurred backdrop, then saturation + tint grading.
                half4 col = SpiralBlur(i.uv, uvRect);
                half luma = dot(col.rgb, half3(0.2126, 0.7152, 0.0722));
                col.rgb = lerp(luma.xxx, col.rgb, saturate(_Saturation));
                col.rgb *= _Tint.rgb;

                // Soft border band CENTRED on the cell edge. |d - 0.5| is continuous across the
                // boundary (distances to the two cells are complementary there), so both sides
                // draw the same full-width band — no per-cell half-borders that could dither
                // into dashes at small cell sizes.
                float d = HexDist(gv);
                float halfWidth = max(0.0, 0.5 - (_BorderDistance - _BorderThickness));
                float border = 1.0 - smoothstep(halfWidth - _BorderSoftness, halfWidth, abs(d - 0.5));

                // Dissolve as smooth spatial noise (bilinear value noise over the lattice space):
                // borders vanish in coherent patches, like the UGUI demo, rather than as lone
                // cell outlines whose shared edges would be left half-drawn.
                float2 np = huv * 0.5;
                float2 ni = floor(np);
                float2 nf = np - ni;
                nf = nf * nf * (3.0 - 2.0 * nf);
                float noise = lerp(
                    lerp(Hash21(ni), Hash21(ni + float2(1, 0)), nf.x),
                    lerp(Hash21(ni + float2(0, 1)), Hash21(ni + float2(1, 1)), nf.x), nf.y);
                border *= 1.0 - smoothstep(saturate(_Dissolve) - 0.03, saturate(_Dissolve) + 0.03, noise);

                // The gaps between the frosted tiles show the backdrop directly — sharp and
                // unmodified. This mirrors the UGUI original exactly: its graph routes the
                // hexagon pattern into SurfaceDescription.Alpha, so its borders are transparent
                // holes with the real scene showing through (they only read as "white" where the
                // scene behind is bright). _BorderOpacity dials how fully the sharp backdrop
                // replaces the frost inside the gap.
                half4 sharp = tex2D(_MainTex, i.uv);
                col = lerp(col, sharp, border * saturate(_BorderOpacity));

                #if _UIE_OUTPUT_LINEAR
                col.rgb = GammaToLinearSpace(col.rgb);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
