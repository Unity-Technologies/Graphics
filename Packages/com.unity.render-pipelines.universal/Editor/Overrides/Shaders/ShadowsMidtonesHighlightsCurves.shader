Shader "Hidden/Universal Render Pipeline/Editor/Shadows Midtones Highlights Curve"
{
    CGINCLUDE

        #include "UnityCG.cginc"
        #pragma editor_sync_compilation
        #pragma target 3.5

        float4 _ShaHiLimits;    // xy: shadows min/max, zw: highlight min/max
        float4 _Variants;       // x: disabled state, y: x-scale, wz: unused

        float3 BlendScreen(float3 base, float3 blend)
        {
            return 1.0 - (1.0 - blend) * (1.0 - base);
        }

        float4 DrawCurve(v2f_img i, float3 background, float3 shadowsCurveColor, float3 midtonesCurveColor, float3 highlightsCurveColor)
        {
            float x = i.uv.x * _Variants.y;
            float y = i.uv.y;
            float aa = fwidth(i.uv.y) * 2.0;

            float shadowsY = 1.0 - smoothstep(_ShaHiLimits.x, _ShaHiLimits.y, x);
            float shadowsCurve = smoothstep(shadowsY + aa, shadowsY, y);
            float shadowsLine = smoothstep(shadowsY - aa, shadowsY, y) - smoothstep(shadowsY, shadowsY + aa, y);

            float highlightsY = smoothstep(_ShaHiLimits.z, _ShaHiLimits.w, x);
            float highlightsCurve = smoothstep(highlightsY + aa, highlightsY, y);
            float highlightsLine = smoothstep(highlightsY - aa, highlightsY, y) - smoothstep(highlightsY, highlightsY + aa, y);

            float midtonesY = 1.0 - shadowsY - highlightsY;
            float midtonesCurve = smoothstep(midtonesY + aa, midtonesY, y);
            float midtonesLine = smoothstep(midtonesY - aa, midtonesY, y) - smoothstep(midtonesY, midtonesY + aa, y);

            float grad = lerp(0.7, 1.0, y);
            float3 shadowsColor = shadowsCurveColor * shadowsCurve * grad;
            float3 midtonesColor = midtonesCurveColor * midtonesCurve * grad;
            float3 highlightsColor = highlightsCurveColor * highlightsCurve * grad;

            float3 color = BlendScreen(shadowsColor, midtonesColor);
            color = BlendScreen(color, highlightsColor);
            color = BlendScreen(background, color * _Variants.xxx);

            const float kAlpha = 0.3 * _Variants.xxx;
            color += shadowsLine * shadowsColor * kAlpha;
            color += midtonesLine * midtonesColor * kAlpha;
            color += highlightsLine * highlightsColor * kAlpha;

            return float4(color, 1.0);
        }

        float4 FragCurveDark(v2f_img i) : SV_Target
        {
            return DrawCurve(i, (pow(0.196, 2.2)).xxx, pow(float3(0.161, 0.851, 0.761), 2.2), pow(float3(0.741, 0.949, 0.443), 2.2), pow(float3(0.9, 0.9, 0.651), 2.2));
        }

        float4 FragCurveLight(v2f_img i) : SV_Target
        {
            return DrawCurve(i, (pow(0.635, 2.2)).xxx, pow(float3(0.161, 0.851, 0.761), 2.2), pow(float3(0.741, 0.949, 0.443), 2.2), pow(float3(1.0, 1.0, 0.651), 2.2));
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
