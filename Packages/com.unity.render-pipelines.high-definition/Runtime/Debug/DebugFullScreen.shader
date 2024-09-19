Shader "Hidden/HDRP/DebugFullScreen"
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
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #define DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/FullScreenDebug.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"

            CBUFFER_START (UnityDebug)
            float _FullScreenDebugMode;
            float _ApplyExposure;
            float4 _FullScreenDebugDepthRemap;
            float _TransparencyOverdrawMaxPixelCost;
            float _QuadOverdrawMaxQuadCost;
            float _VertexDensityMaxPixelCost;
            uint _DebugContactShadowLightIndex;
            float4 _DebugDepthPyramidParams; // (mip index, offset_x, offset_y, unused)
            float _MinMotionVector;
            float4 _MotionVecIntensityParams;
            float _FogVolumeOverdrawMaxValue;
            int _ComputeThicknessLayerIndex;
            int _ComputeThicknessShowOverlapCount;
            float _ComputeThicknessScale;
            int _VolumetricCloudsDebugMode;
            CBUFFER_END

            #define _MotionVecIntensityScale _MotionVecIntensityParams.x
            #define _MotionVecHeatMode _MotionVecIntensityParams.y

            TEXTURE2D_X(_DebugFullScreenTexture);

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);

                //Texcoord holds the coordinates of the original rendering before post processing.
                output.texcoord = GetNormalizedFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            static float4 VTDebugColors[] = {
                float4(1.0f, 1.0f, 1.0f, 1.0f),
                float4(1.0f, 1.0f, 0.0f, 1.0f),
                float4(0.0f, 1.0f, 1.0f, 1.0f),
                float4(0.0f, 1.0f, 0.0f, 1.0f),
                float4(1.0f, 0.0f, 1.0f, 1.0f),
                float4(1.0f, 0.0f, 0.0f, 1.0f),
                float4(0.0f, 0.0f, 1.0f, 1.0f),
                float4(0.5f, 0.5f, 0.5f, 1.0f),
                float4(0.5f, 0.5f, 0.0f, 1.0f),
                float4(0.0f, 0.5f, 0.5f, 1.0f),
                float4(0.0f, 0.5f, 0.0f, 1.0f),
                float4(0.5f, 0.0f, 0.5f, 1.0f),
                float4(0.5f, 0.0f, 0.0f, 1.0f),
                float4(0.0f, 0.0f, 0.5f, 1.0f)
            };

            // Compute polynomial order-3: c[0] + c[1]*x + c[2]*x^2 + c[3]*x^3
            float HornerForm3(float x, float4 c)
            {
                return mad(x, mad(x, mad(x, c[3], c[2]), c[1]), c[0]);
            }

            // Input x [0.0f; 1.0f]
            // Colorscale 'Viridis' perceptualy linear and compliant with all colorblind type.
            float3 Viridis(float x)
            {
                return float3(
                        HornerForm3(x, float4(0.336877f, -0.310721f, -1.62633f, 2.34395f)) / HornerForm3(x, float4(1.21945f, -1.70046f, 0.233104f, 1.0f)),
                        HornerForm3(x, float4(0.00259495f, 1.21637f, -2.61721f, 1.72375f)) / HornerForm3(x, float4(0.800986f, -1.04614f, -0.395385f, 1.0f)),
                        HornerForm3(x, float4(0.0754636f, 0.685555f, -1.65847f, 0.902681f)) / HornerForm3(x, float4(0.234944f, 0.606111f, -1.80461f, 1.0f))
                    );
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

            void ColorWidget(
                int2 positionSS,
                float4 rect,
                float3 borderColor,
                float3 innerColor,
                inout float4 debugColor,
                inout float4 backgroundColor
            )
            {
                const float4 distToRects = float4(rect.zw - positionSS,  positionSS - rect.xy);
                if (all(distToRects > 0))
                {
                    const float distToRect = min(min(distToRects.x, distToRects.y), min(distToRects.z, distToRects.w));
                    const float sdf = clamp(distToRect * 0.5, 0, 1);
                    debugColor = float4(
                        lerp(borderColor, innerColor, sdf),
                        1.0
                    );
                    backgroundColor.a = 0;
                }
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

            // return motion vector in NDC space [0..1]
            float2 SampleMotionVectors(float2 coords)
            {
                float2 motionVectorNDC;
                DecodeMotionVector(SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, coords), motionVectorNDC);
                return motionVectorNDC;
            }
            // end motion vector utilties

            float3 ToHeat(float value)
            {
                float3 r = value * 2.1f - float3(1.8f, 1.14f, 0.3f);
                return 1.0f - r * r;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Note: If mipmap debug mode is enabled, we don't render other full screen debug modes
                // and the value of _FullScreenDebugMode is forced to 0
                if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
                {
                    // just passing through
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);

                    // draw legend
                    switch(_DebugMipMapMode)
                    {
                        case DEBUGMIPMAPMODE_MIP_COUNT:
                            DrawMipCountLegend(input.texcoord / _RTHandleScale.xy, _ScreenSize, color.rgb);
                            break;
                        case DEBUGMIPMAPMODE_MIP_RATIO:
                            DrawMipRatioLegend(input.texcoord / _RTHandleScale.xy, _ScreenSize, color.rgb);
                            break;
                        case DEBUGMIPMAPMODE_MIP_STREAMING_STATUS:
                            if (_DebugMipMapStatusMode == DEBUGMIPMAPSTATUSMODE_TEXTURE)
                                DrawMipStreamingStatusLegend(input.texcoord / _RTHandleScale.xy, _ScreenSize, _DebugMipMapShowStatusCode, color.rgb);
                            else
                                DrawMipStreamingStatusPerMaterialLegend(input.texcoord / _RTHandleScale.xy, _ScreenSize, color.rgb);
                            break;
                        case DEBUGMIPMAPMODE_MIP_STREAMING_PERFORMANCE:
                            DrawTextureStreamingPerformanceLegend(input.texcoord / _RTHandleScale.xy, _ScreenSize, color.rgb);
                            break;
                        case DEBUGMIPMAPMODE_MIP_STREAMING_PRIORITY:
                            DrawMipPriorityLegend(input.texcoord / _RTHandleScale.xy, _ScreenSize, color.rgb);
                            break;
                        case DEBUGMIPMAPMODE_MIP_STREAMING_ACTIVITY:
                            DrawMipRecentlyUpdatedLegend(input.texcoord / _RTHandleScale.xy, _ScreenSize, _DebugMipMapStatusMode == DEBUGMIPMAPSTATUSMODE_MATERIAL, color.rgb);
                            break;
                    }

                    return color;
                }

                // Note: If the single shadow debug mode is enabled, we don't render other full screen debug modes
                // and the value of _FullScreenDebugMode is forced to 0
                if (_DebugShadowMapMode == SHADOWMAPDEBUGMODE_SINGLE_SHADOW || _FullScreenDebugMode == FULLSCREENDEBUGMODE_NONE)
                {
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    color *= _ApplyExposure > 0.0 ? GetCurrentExposureMultiplier() : 1.0;
                    return color;
                }

                // SSAO
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_SCREEN_SPACE_AMBIENT_OCCLUSION)
                {
                    return 1.0f - SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord).xxxx;
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_NAN_TRACKER)
                {
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);

                    if (AnyIsNaN(color) || AnyIsInf(color))
                    {
                        color = float4(1.0, 0.0, 0.0, 1.0);
                    }
                    else
                    {
                        color.rgb = Luminance(color.rgb).xxx;
                    }

                    return color;
                }
                if( _FullScreenDebugMode == FULLSCREENDEBUGMODE_LIGHT_CLUSTER)
                {
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    return color;
                }
                if( _FullScreenDebugMode == FULLSCREENDEBUGMODE_SCREEN_SPACE_GLOBAL_ILLUMINATION)
                {
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    return color.w * color;
                }
                if( _FullScreenDebugMode == FULLSCREENDEBUGMODE_RECURSIVE_RAY_TRACING)
                {
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    return color;
                }
                if ( _FullScreenDebugMode == FULLSCREENDEBUGMODE_RAY_TRACED_SUB_SURFACE)
                {
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    return color;
                }
                if ( _FullScreenDebugMode == FULLSCREENDEBUGMODE_VOLUMETRIC_CLOUDS)
                {
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    return (_VolumetricCloudsDebugMode == 0) ? float4(color.xyz, 1.0) : color.x;
                }
                if ( _FullScreenDebugMode == FULLSCREENDEBUGMODE_VOLUMETRIC_CLOUDS_SHADOW)
                {
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    return float4(color.yyy, 1.0);
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_VOLUMETRIC_FOG)
                {
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    return float4(color.rgb, 1.0);
                }
                if ( _FullScreenDebugMode == FULLSCREENDEBUGMODE_SCREEN_SPACE_SHADOWS)
                {
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    return color;
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_RAY_TRACING_ACCELERATION_STRUCTURE)
                {
                    return SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_MOTION_VECTORS_INTENSITY)
                {
                    float2 mv = SampleMotionVectors(input.texcoord);
                    float mvLen = length(mv) * _MotionVecIntensityScale;

                    if (_MotionVecHeatMode)
                        return float4(ToHeat(saturate(mvLen)), 1);

                    return float4(mvLen.xxx, 1);

                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_MOTION_VECTORS)
                {
                    float2 mv = SampleMotionVectors(input.texcoord);
                    if (length(mv * _ScreenSize.xy) < _MinMotionVector)
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
                    float2 positionSS = input.texcoord.xy / _RTHandleScale.xy;
                    positionSS *= _ScreenSize.xy;
                    float2 center = (floor(positionSS / size) + 0.5) * size;
                    positionSS -= center;

                    // Sample the center of the cell to get the current arrow vector
                    float2 mv_arrow = 0.0f;
#if DONT_USE_NINE_TAP_FILTER
                    mv_arrow = SampleMotionVectors(center * _ScreenSize.zw * _RTHandleScale.xy);
#else
                    UNITY_UNROLL
                    for (int i = -1; i <= 1; ++i)
                        UNITY_UNROLL
                        for (int j = -1; j <= 1; ++j)
                            mv_arrow += SampleMotionVectors((center + float2(i, j)) * _RTHandleScale.xy * _ScreenSize.zw);
                    mv_arrow /= 9.0f;
#endif
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
                    if(!any(mv))
                        color = float3(0, 0, 0);

                    return float4(color + d.xxx, 1.0);
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_COLOR_LOG)
                {
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    return color;
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_DEPTH_OF_FIELD_COC)
                {
                    float coc = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord).x;

                    float3 color = lerp(float3(1.0, 0.0, 0.0), float3(1.0, 1.0, 1.0), saturate(-coc));
                    color = lerp(color, float3(1.0, 1.0, 1.0), saturate(coc));

                    const float kPeakingThreshold = 0.01;
                    if (abs(coc) <= kPeakingThreshold)
                        color = lerp(float3(0.0, 0.0, 1.0), color, PositivePow(abs(coc) / kPeakingThreshold, 2.0));

                    return float4(color, 1.0);
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_DEPTH_OF_FIELD_TILE_CLASSIFICATION)
                {
                    float3 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord).rgb;
                    return float4(color, 1.0);
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_CONTACT_SHADOWS)
                {
                    uint2 samplePosition = (uint2)((input.texcoord.xy / _RTHandleScale.xy) * _DebugViewportSize.xy);
                    uint contactShadowData = LOAD_TEXTURE2D_X(_ContactShadowTexture, samplePosition).r;

                    // when the index is -1 we display all contact shadows
                    uint mask = (_DebugContactShadowLightIndex == -1) ? -1 : 1 << _DebugContactShadowLightIndex;
                    float lightContactShadow = (contactShadowData & mask) != 0;

                    return float4(1.0 - lightContactShadow.xxx, 0.0);
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_CONTACT_SHADOWS_FADE)
                {
                    uint2 samplePosition = (uint2)((input.texcoord.xy / _RTHandleScale.xy) * _DebugViewportSize.xy);
                    uint contactShadowData = LOAD_TEXTURE2D_X(_ContactShadowTexture, samplePosition).r;
                    float fade = float((contactShadowData >> 24)) / 255.0;

                    return float4(fade.xxx, 0.0);
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_SCREEN_SPACE_REFLECTIONS ||
                    _FullScreenDebugMode == FULLSCREENDEBUGMODE_SCREEN_SPACE_REFLECTIONS_PREV ||
                    _FullScreenDebugMode == FULLSCREENDEBUGMODE_SCREEN_SPACE_REFLECTIONS_ACCUM ||
                    _FullScreenDebugMode == FULLSCREENDEBUGMODE_TRANSPARENT_SCREEN_SPACE_REFLECTIONS ||
                    _FullScreenDebugMode == FULLSCREENDEBUGMODE_SCREEN_SPACE_REFLECTION_SPEED_REJECTION)
                {
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);

                    // Raytrace reflection use the current frame exposure - TODO: currently the buffer don't use pre-exposure.
                    // See InversePreExposeSsrLighting() in LightEvaluation.hlsl
                    float exposureMultiplier = _EnableRayTracedReflections ? GetCurrentExposureMultiplier() : 1.0;
                    color *=  exposureMultiplier;

                    return float4(color.rgb, 1.0f);
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_PRE_REFRACTION_COLOR_PYRAMID
                    || _FullScreenDebugMode == FULLSCREENDEBUGMODE_FINAL_COLOR_PYRAMID)
                {
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    return float4(color.rgb, 1.0);
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_DEPTH_PYRAMID)
                {
                    int debugDepthPyramidMip = _DebugDepthPyramidParams.x;
                    int2 debugDepthPyramidOffset = int2(_DebugDepthPyramidParams.yz);

                    // Reuse depth display function from DebugViewMaterial
                    uint2 samplePosition = (uint2)((input.texcoord.xy / _RTHandleScale.xy) * _DebugViewportSize.xy);
                    uint2 pixCoord = (uint2)samplePosition >> debugDepthPyramidMip;
                    float depth = LOAD_TEXTURE2D_X(_CameraDepthTexture, pixCoord + debugDepthPyramidOffset).r;
                    PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

                    // We square the factors to have more precision near zero which is where people usually want to visualize depth.
                    float remappedFar = min(_FullScreenDebugDepthRemap.w, _FullScreenDebugDepthRemap.y * _FullScreenDebugDepthRemap.y * _FullScreenDebugDepthRemap.w);
                    float remappedNear = max(_FullScreenDebugDepthRemap.z, _FullScreenDebugDepthRemap.x * _FullScreenDebugDepthRemap.x * _FullScreenDebugDepthRemap.w);
                    float linearDepth = lerp(0.0, 1.0, (posInput.linearDepth - remappedNear) / (remappedFar - remappedNear));
                    return float4(linearDepth.xxx, 1.0);
                }

                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_WORLD_SPACE_POSITION)
                {
                    uint2 samplePosition = (uint2)((input.texcoord / _RTHandleScale.xy) * _DebugViewportSize.xy);
                    float depth = LoadCameraDepth(samplePosition);
                    PositionInputs posInput = GetPositionInput(samplePosition, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
                    float3 positionWS = GetAbsolutePositionWS(posInput.positionWS);

                    if (depth != 0)
                        return float4(positionWS.xyz, 1.0);
                    return float4(0.0, 0.0, 0.0, 0.0);
                }

                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_TRANSPARENCY_OVERDRAW)
                {
                    float4 color = (float4)0;

                    float pixelCost = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord).r;
                    if ((pixelCost > 0.001))
                    {
                        color.rgb = GetOverdrawColor(pixelCost, _TransparencyOverdrawMaxPixelCost);
                    }

                    DrawOverdrawLegend(input.texcoord / _RTHandleScale.xy, _TransparencyOverdrawMaxPixelCost, _ScreenSize, color.rgb);

                    return color;
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_QUAD_OVERDRAW)
                {
                    uint2 samplePosition = (uint2)((input.texcoord / _RTHandleScale.xy) * _DebugViewportSize.xy);
                    uint2 quad = (uint2)samplePosition.xy & ~1;
                    uint2 renderScreenSize = (uint2)_DebugViewportSize.xy;
                    uint quad0_idx = renderScreenSize.x * (renderScreenSize.y * SLICE_ARRAY_INDEX + quad.y) + quad.x;
                    float4 color = (float4)0;

                    float quadCost = (float)_FullScreenDebugBuffer[quad0_idx];
                    if ((quadCost > 0.001))
                        color.rgb = HsvToRgb(float3(0.66 * saturate(1.0 - (1.0 / _QuadOverdrawMaxQuadCost) * quadCost), 1.0, 1.0));

                    return color;
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_LOCAL_VOLUMETRIC_FOG_OVERDRAW)
                {
                    float4 color = (float4)0;

                    float pixelCost = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord).r;
                    if ((pixelCost > 0.001))
                    {
                        color.rgb = GetOverdrawColor(pixelCost, _FogVolumeOverdrawMaxValue);
                    }

                    DrawOverdrawLegend(input.texcoord / _RTHandleScale.xy, _FogVolumeOverdrawMaxValue, _ScreenSize, color.rgb);

                    return color;
                }
                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_VERTEX_DENSITY)
                {
                    uint2 samplePosition = (uint2)((input.texcoord / _RTHandleScale.xy) * _DebugViewportSize.xy);
                    uint2 quad = (uint2)samplePosition.xy;
                    uint2 renderScreenSize = (uint2)_DebugViewportSize.xy;
                    uint quad_idx = renderScreenSize.x * (renderScreenSize.y * SLICE_ARRAY_INDEX + quad.y) + quad.x;
                    float4 color = (float4)0;

                    float density = (float)_FullScreenDebugBuffer[quad_idx];
                    if ((density > 0.001))
                        color.rgb = HsvToRgb(float3(0.66 * saturate(1.0 - (1.0 / _VertexDensityMaxPixelCost) * density), 1.0, 1.0));

                    return color;
                }

                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_REQUESTED_VIRTUAL_TEXTURE_TILES)
                {
                    float4 color = SAMPLE_TEXTURE2D_X(_DebugFullScreenTexture, s_point_clamp_sampler, input.texcoord);
                    if (!any(color))
                        return float4(0, 0, 0, 0);

                    float tileX = color.r;
                    float tileY = color.g;
                    float level = color.b;
                    float tex = color.a;
                    float3 hsv = RgbToHsv(VTDebugColors[level].rgb);

                    //dont adjust hue/saturation when trying to show white or grey (on mips 0 and 7)
                    if (level == 0 || level == 7)
                    {
                        hsv.z = ((uint)tileY % 5) / 5.0f + 1.0f - (((uint)tileX % 5) / 5.0f);
                        hsv.z /= 2.0f;
                        hsv.x = hsv.y = 0.0f;
                    }
                    else
                    {
                        hsv.y = ((uint)tileY % 5) / 10.0f + 0.5f;
                        hsv.z = 1.0f - (((uint)tileX % 5) / 10.0f + 0.5f);
                    }

                    return float4(HsvToRgb(hsv), 1.0f);

                }

                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_LENS_FLARE_DATA_DRIVEN || _FullScreenDebugMode == FULLSCREENDEBUGMODE_LENS_FLARE_SCREEN_SPACE)
                {
                    uint2 samplePosition = (uint2)((input.texcoord / _RTHandleScale.xy) * _DebugViewportSize.xy);
                    return LOAD_TEXTURE2D_X(_DebugFullScreenTexture, samplePosition);
                }

                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_COMPUTE_THICKNESS)
                {
                    float2 pos = input.texcoord / _RTHandleScale.xy;
                    float2 thicknessValues = SampleThickness(pos, _ComputeThicknessLayerIndex);

                    float thickness = _ComputeThicknessShowOverlapCount ? thicknessValues.y : thicknessValues.x;

                    float thicknessRemap = 1.0f - exp2(-thickness * _ComputeThicknessScale);

                    // Layer not used
                    if (thickness == -1.0f)
                        return float4(0.50888f, 0.14413f, 0.50888f, 1.0f);
                    // Open meshes, double sided
                    else if (thickness < 0.0f)
                        return float4(0.87962f, 0.0356f, 0.02956f, 1.0f);
                    // Compute Thickness feature not enabled
                    else if (_EnableComputeThickness == 0)
                        return float4(0.82279f, 0.11193f, 0.0f, 1.0f);
                    // Backround or Z-Fight
                    else if (thickness == 0.0f)
                        return float4(0.03955f, 0.03955f, 0.03955f, 1.0f);
                    else
                    {
                        float v = saturate(thicknessRemap);
                        return float4(SRGBToLinear(Viridis(v)), 1.0f);
                    }
                }

                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_HIGH_QUALITY_LINES)
                {
                    uint2 samplePosition = (uint2)((input.texcoord / _RTHandleScale.xy) * _DebugViewportSize.xy);
                    samplePosition.y = _DebugViewportSize.y - samplePosition.y;
                    return LOAD_TEXTURE2D_X(_DebugFullScreenTexture, samplePosition);
                }

                if (_FullScreenDebugMode == FULLSCREENDEBUGMODE_STP)
                {
                    uint2 samplePosition = (uint2)((input.texcoord / _RTHandleScale.xy) * _DebugViewportSize.xy);
                    float4 stp = LOAD_TEXTURE2D_X(_DebugFullScreenTexture, samplePosition);

                    // This is encoded in gamma 2.0 (so the square is needed to get it back to linear).
                    stp.rgb *= stp.rgb;

                    return stp;
                }

                return float4(0.0, 0.0, 0.0, 0.0);
            }

            ENDHLSL
        }

    }
    Fallback Off
}
