Shader "MotionVecDebug"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off
        Pass
        {
            Name "MotionVectorDebugPass"

            HLSLPROGRAM
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
            #pragma vertex FullscreenVert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"

            TEXTURE2D(_MotionVectorTexture);
            SAMPLER(sampler_MotionVectorTexture);

            float4 _SourceTex_TexelSize;
            float _Intensity;

            float2 SampleMotionVectors(float2 coord)
            {
                return SAMPLE_TEXTURE2D_X(_MotionVectorTexture, sampler_MotionVectorTexture, coord);
            }

            float DistanceToLine(float2 p, float2 p1, float2 p2)
            {
                float2 center = (p1 + p2) * 0.5;
                float len = length(p2 - p1);
                float2 dir = (p2 - p1) / len;
                float2 rel_p = p - center;
                return dot(rel_p, float2(dir.y, -dir.x));
            }

            float DistanceToSegment(float2 p, float2 p1, float2 p2)
            {
                float2 center = (p1 + p2) * 0.5;
                float len = length(p2 - p1);
                float2 dir = (p2 - p1) / len;
                float2 rel_p = p - center;
                float dist1 = abs(dot(rel_p, float2(dir.y, -dir.x)));
                float dist2 = abs(dot(rel_p, dir)) - 0.5 * len;
                return max(dist1, dist2);
            }

            float DrawArrow(float2 texcoord, float body, float head, float height, float linewidth, float antialias)
            {
                float w = linewidth / 2.0 + antialias;
                float2 start = -float2(body / 2.0, 0.0);
                float2 end = float2(body / 2.0, 0.0);

                // Head: 3 lines
                float d1 = DistanceToLine(texcoord, end, end - head * float2(1.0, -height));
                float d2 = DistanceToLine(texcoord, end - head * float2(1.0, height), end);
                float d3 = texcoord.x - end.x + head;

                // Body: 1 segment
                float d4 = DistanceToSegment(texcoord, start, end - float2(linewidth, 0.0));

                float d = min(max(max(d1, d2), -d3), d4);
                return d;
            }

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 mv = SampleMotionVectors(input.uv);
                if (length(mv * _ScreenSize.xy) < 0.001f)
                {
                    return float4(0, 0, 0, 1);
                }
                // Background color intensity - keep this low unless you want to make your eyes bleed
                const float kMinIntensity = 0.03f;
                const float kMaxIntensity = 0.50f;

                // Map motion vector direction to color wheel (hue between 0 and 360deg)
                float phi = atan2(mv.x, mv.y);
                float hue = (phi / PI + 1.0) * 0.5;
                float r = abs(hue * 6.0 - 3.0) - 1.0;
                float g = 2.0 - abs(hue * 6.0 - 2.0);
                float b = 2.0 - abs(hue * 6.0 - 4.0);

                float maxSpeed = 60.0f / 0.15f; // Admit that 15% of a move the viewport by second at 60 fps is really fast
                float absoluteLength = saturate(length(mv.xy) * maxSpeed);
                float3 color = float3(r, g, b) * lerp(kMinIntensity, kMaxIntensity, absoluteLength);
                color = saturate(color);

                // Grid subdivisions - should be dynamic
                const float kGrid = 64.0;

                // Arrow grid (aspect ratio is kept)
                float aspect = _ScreenSize.y * _ScreenSize.z;
                float rows = floor(kGrid * aspect);
                float cols = kGrid;
                float2 size = _ScreenSize.xy / float2(cols, rows);
                float body = min(size.x, size.y) / sqrt(2.0);
                float2 positionSS = input.uv.xy;
                positionSS *= _ScreenSize.xy;
                float2 center = (floor(positionSS / size) + 0.5) * size;
                positionSS -= center;

                // Sample the center of the cell to get the current arrow vector
                float2 mv_arrow = 0.0f;
                mv_arrow = SampleMotionVectors(center * _ScreenSize.zw);
                mv_arrow.y *= -1;

                // Skip empty motion
                float d = 0.0;
                if (any(mv_arrow))
                {
                    // Rotate the arrow according to the direction
                    mv_arrow = normalize(mv_arrow);
                    float2x2 rot = float2x2(mv_arrow.x, -mv_arrow.y, mv_arrow.y, mv_arrow.x);
                    positionSS = mul(rot, positionSS);

                    d = DrawArrow(positionSS, body, 0.25 * body, 0.5, 2.0, 1.0);
                    d = 1.0 - saturate(d);
                }

                // Explicitly handling the case where mv == float2(0, 0) as atan2(mv.x, mv.y) above would be atan2(0,0) which
                // is undefined and in practice can be incosistent between compilers (e.g. NaN on FXC and ~pi/2 on DXC)
                if (!any(mv))
                    color = float3(0, 0, 0);

                return float4(color + d.xxx, 1.0);
            }
            ENDHLSL
        }
    }
}
