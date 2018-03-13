Shader "Hidden/HDRenderPipeline/DebugFullScreen"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal

            #pragma vertex Vert
            #pragma fragment Frag

            #include "CoreRP/ShaderLibrary/Common.hlsl"
            #include "CoreRP/ShaderLibrary/Color.hlsl"
            #include "../ShaderVariables.hlsl"
            #include "../Debug/DebugDisplay.cs.hlsl"

            TEXTURE2D(_DebugFullScreenTexture);
            float _FullScreenDebugMode;
            float _RequireToFlipInputTexture;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetNormalizedFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            // Motion vector debug utilities
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

            float2 SampleMotionVectors(float2 coords)
            {
                return SAMPLE_TEXTURE2D(_DebugFullScreenTexture, s_point_clamp_sampler, coords).xy;
            }
            // end motion vector utilties

            float4 Frag(Varyings input) : SV_Target
            {
                if (_RequireToFlipInputTexture > 0.0)
                {
                    // Texcoord are already scaled by _ScreenToTargetScale but we need to account for the flip here anyway.
                    input.texcoord.y = 1.0 * _ScreenToTargetScale.y - input.texcoord.y;
                }

                // SSAO
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_SSAO)
                {
                    return 1.0f - SAMPLE_TEXTURE2D(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord).xxxx;
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_NAN_TRACKER)
                {
                    float4 color = SAMPLE_TEXTURE2D(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    
                    if (AnyIsNan(color) || any(isinf(color)))
                    {
                        color = float4(1.0, 0.0, 0.0, 1.0);
                    }
                    else
                    {
                        color.rgb = Luminance(color.rgb).xxx;
                    }

                    return color;
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_MOTION_VECTORS)
                {
                    float2 mv = SampleMotionVectors(input.texcoord);

                    // Background color intensity - keep this low unless you want to make your eyes bleed
                    const float kIntensity = 0.15;

                    // Map motion vector direction to color wheel (hue between 0 and 360deg)
                    float phi = atan2(mv.x, mv.y);
                    float hue = (phi / PI + 1.0) * 0.5;
                    float r = abs(hue * 6.0 - 3.0) - 1.0;
                    float g = 2.0 - abs(hue * 6.0 - 2.0);
                    float b = 2.0 - abs(hue * 6.0 - 4.0);

                    float3 color = saturate(float3(r, g, b) * kIntensity);

                    // Grid subdivisions - should be dynamic
                    const float kGrid = 64.0;

                    // Arrow grid (aspect ratio is kept)
                    float rows = floor(kGrid * _ScreenParams.y / _ScreenParams.x);
                    float cols = kGrid;
                    float2 size = _ScreenParams.xy / float2(cols, rows);
                    float body = min(size.x, size.y) / sqrt(2.0);
                    float2 texcoord = input.positionCS.xy;
                    float2 center = (floor(texcoord / size) + 0.5) * size;
                    texcoord -= center;

                    // Sample the center of the cell to get the current arrow vector
                    float2 arrow_coord = center / _ScreenParams.xy;

                    if (_RequireToFlipInputTexture > 0.0)
                    {
                        arrow_coord.y = 1.0 - arrow_coord.y;
                    }
                    arrow_coord *= _ScreenToTargetScale.xy;

                    float2 mv_arrow = SampleMotionVectors(arrow_coord);

                    if (_RequireToFlipInputTexture == 0.0)
                    {
                        mv_arrow.y *= -1;
                    }

                    // Skip empty motion
                    float d = 0.0;
                    if (any(mv_arrow))
                    {
                        // Rotate the arrow according to the direction
                        mv_arrow = normalize(mv_arrow);
                        float2x2 rot = float2x2(mv_arrow.x, -mv_arrow.y, mv_arrow.y, mv_arrow.x);
                        texcoord = mul(rot, texcoord);

                        d = DrawArrow(texcoord, body, 0.25 * body, 0.5, 2.0, 1.0);
                        d = 1.0 - saturate(d);
                    }

                    return float4(color + d.xxx, 1.0);
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_DEFERRED_SHADOWS)
                {
                    float4 color = SAMPLE_TEXTURE2D(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    return float4(color.rrr, 0.0);
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_PRE_REFRACTION_COLOR_PYRAMID
                    || _FullScreenDebugMode == FULLSCREENDEBUGMODE_FINAL_COLOR_PYRAMID)
                {
                    float4 color = SAMPLE_TEXTURE2D(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    return float4(color.rgb, 1.0);
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_DEPTH_PYRAMID)
                {
                    // Reuse depth display function from DebugViewMaterial
                    float depth = SAMPLE_TEXTURE2D(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord).r;
                    PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_VP);
                    float linearDepth = frac(posInput.linearDepth * 0.1);
                    return float4(linearDepth.xxx, 1.0);
                }

                return float4(0.0, 0.0, 0.0, 0.0);
            }

            ENDHLSL
        }

    }
    Fallback Off
}
