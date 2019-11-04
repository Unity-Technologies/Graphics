Shader "Hidden/HD PostProcessing/Editor/Custom Tonemapper Curve"
{
    CGINCLUDE

        #include "UnityCG.cginc"
        #pragma editor_sync_compilation
        #pragma target 3.5

        float4 _CustomToneCurve;
        float4 _ToeSegmentA;
        float4 _ToeSegmentB;
        float4 _MidSegmentA;
        float4 _MidSegmentB;
        float4 _ShoSegmentA;
        float4 _ShoSegmentB;
        float4 _Variants; // x: disabled state, y: x-scale, wz: unused

        float EvalCustomSegment(float x, float4 segmentA, float2 segmentB)
        {
            const float kOffsetX = segmentA.x;
            const float kOffsetY = segmentA.y;
            const float kScaleX  = segmentA.z;
            const float kScaleY  = segmentA.w;
            const float kLnA     = segmentB.x;
            const float kB       = segmentB.y;

            float x0 = (x - kOffsetX) * kScaleX;
            float y0 = (x0 > 0.0) ? exp(kLnA + kB * log(x0)) : 0.0;
            return y0 * kScaleY + kOffsetY;
        }

        float EvalCustomCurve(float x, float3 curve, float4 toeSegmentA, float2 toeSegmentB, float4 midSegmentA, float2 midSegmentB, float4 shoSegmentA, float2 shoSegmentB)
        {
            float4 segmentA;
            float2 segmentB;

            if (x < curve.y)
            {
                segmentA = toeSegmentA;
                segmentB = toeSegmentB;
            }
            else if (x < curve.z)
            {
                segmentA = midSegmentA;
                segmentB = midSegmentB;
            }
            else
            {
                segmentA = shoSegmentA;
                segmentB = shoSegmentB;
            }

            return EvalCustomSegment(x, segmentA, segmentB);
        }

        // curve: x: inverseWhitePoint, y: x0, z: x1
        float CustomTonemap(float x, float3 curve, float4 toeSegmentA, float2 toeSegmentB, float4 midSegmentA, float2 midSegmentB, float4 shoSegmentA, float2 shoSegmentB)
        {
            float normX = x * curve.x;
            return EvalCustomCurve(normX.x, curve, toeSegmentA, toeSegmentB, midSegmentA, midSegmentB, shoSegmentA, shoSegmentB);
        }

        float4 DrawCurve(v2f_img i, float3 background, float3 curveColor)
        {
            float y = CustomTonemap(i.uv.x * _Variants.y,
                _CustomToneCurve,
                _ToeSegmentA,
                _ToeSegmentB.xy,
                _MidSegmentA,
                _MidSegmentB.xy,
                _ShoSegmentA,
                _ShoSegmentB.xy
            );

            float aa = fwidth(i.uv.y - y);
            float curve = smoothstep(y - aa, y, i.uv.y) - smoothstep(y, y + aa, i.uv.y);
            float3 color = lerp(background, curveColor, curve * _Variants.xxx);

            return float4(color, 1.0);
        }

        float4 FragCurveDark(v2f_img i) : SV_Target
        {
            return DrawCurve(i, (pow(0.196, 2.2)).xxx, (pow(0.7, 2.2)).xxx);
        }

        float4 FragCurveLight(v2f_img i) : SV_Target
        {
            return DrawCurve(i, (pow(0.635, 2.2)).xxx, (pow(0.2, 2.2)).xxx);
        }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // (0) Dark skin
        Pass
        {
            CGPROGRAM

                #pragma vertex vert_img
                #pragma fragment FragCurveDark

            ENDCG
        }

        // (1) Light skin
        Pass
        {
            CGPROGRAM

                #pragma vertex vert_img
                #pragma fragment FragCurveLight

            ENDCG
        }
    }
}
