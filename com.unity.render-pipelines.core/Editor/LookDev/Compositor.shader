Shader "Hidden/LookDev/Compositor"
{
    Properties
    {
        _Tex0WithSun("First View", 2D) = "white" {}
        _Tex0WithoutSun("First View without sun", 2D) = "white" {}
        _Tex0Shadows("First View shadow mask", 2D) = "white" {}
        _ShadowColor0("Shadow Color for first view", Color) = (1.0, 1.0, 1.0, 1.0)
        _Tex1WithSun("Second View", 2D) = "white" {}
        _Tex1WithoutSun("Second View without sun", 2D) = "white" {}
        _Tex1Shadows("Second View shadow mask", 2D) = "white" {}
        _ShadowColor1("Shadow Color for second view", Color) = (1.0, 1.0, 1.0, 1.0)
        _CompositingParams("Blend Factor, exposure for first and second view, and current selected side", Vector) = (0.0, 1.0, 1.0, 1.0)
        _CompositingParams2("Drag and drop zone and shadow multipliers", Vector) = (0.0, 1.0, 1.0, 1.0) // Drag and Drop zone Left == 1.0, Right == -1.0, None == 0.0
        _FirstViewColor("Gizmo Color for first view", Color) = (0.5, 0.5, 0.5, 0.5)
        _SecondViewColor("Gizmo Color for second view", Color) = (0.5, 0.5, 0.5, 0.5)
        _GizmoPosition("Position of split view gizmo", Vector) = (0.5, 0.5, 0.0, 0.0)
        _GizmoZoneCenter("Center of Zone view gizmo", Vector) = (0.5, 0.5, 0.0, 0.0)
        _GizmoSplitPlane("2D plane of the gizmo", Vector) = (1.0, 1.0, 0.0, 0.0)
        _GizmoSplitPlaneOrtho("2D plane orthogonal to the gizmo", Vector) = (1.0, 1.0, 0.0, 0.0)
        _GizmoLength("Gizmo Length", Float) = 0.2
        _GizmoThickness("Gizmo Thickness", Vector) = (0.01, 0.08, 0.0, 0.0)
        _GizmoCircleRadius("Gizmo extremities radius", Vector) = (0.05, 0.4, 0.0, 0.0)
        _GizmoRenderMode("Render gizmo mode", Float) = 0.0
        _GetBlendFactorMaxGizmoDistance("Distance on the gizmo where the blend circle stops", Float) = 0.2
        _BlendFactorCircleRadius("Visual radius of the blend factor gizmo", Float) = 0.01
        _ScreenRatio("Screen ratio", Vector) = (1.0, 1.0, 0.0, 0.0) // xy screen ratio, zw screen size
        _ToneMapCoeffs1("Parameters for neutral tonemap", Vector) = (0.0, 0.0, 0.0, 0.0)
        _ToneMapCoeffs2("Parameters for neutral tonemap", Vector) = (0.0, 0.0, 0.0, 0.0)
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #pragma vertex vert

            // Enum matching GizmoOperationType in LookDevViews.cs
    #define kNone 0.0f
    #define kTranslation 1.0f
    #define kRotationZone1 2.0f
    #define kRotationZone2 3.0f
    #define kAll 4.0f


    sampler2D   _Tex0WithSun;
    sampler2D   _Tex0WithoutSun;
    sampler2D   _Tex0Shadows;
    float4      _ShadowColor0;
    sampler2D   _Tex1WithSun;
    sampler2D   _Tex1WithoutSun;
    sampler2D   _Tex1Shadows;
    float4      _ShadowColor1;
    float4      _CompositingParams; // x BlendFactor, yz ExposureValue (first/second view), w current selected side
    float4      _CompositingParams2; // x current drag context, y apply tonemap (bool), z shadow multiplier
    float4      _FirstViewColor;
    float4      _SecondViewColor;
    float4      _GizmoPosition;
    float4      _GizmoZoneCenter;
    float4      _GizmoThickness;
    float4      _GizmoCircleRadius;
    float4      _GizmoSplitPlane;
    float4      _GizmoSplitPlaneOrtho;
    float       _GizmoLength;
    float       _GizmoRenderMode;
    float       _GetBlendFactorMaxGizmoDistance;
    float       _BlendFactorCircleRadius;
    float4      _ScreenRatio;
    float4      _ToneMapCoeffs1;
    float4      _ToneMapCoeffs2;

    float4      _Tex0WithSun_ST;

    #define ShadowMultiplier0 _CompositingParams2.z
    #define ShadowMultiplier1 _CompositingParams2.w

    #define ExposureValue1 _CompositingParams.y
    #define ExposureValue2 _CompositingParams.z

    #define InBlack         _ToneMapCoeffs1.x
    #define OutBlack        _ToneMapCoeffs1.y
    #define InWhite         _ToneMapCoeffs1.z
    #define OutWhite        _ToneMapCoeffs1.w
    #define WhiteLevel      _ToneMapCoeffs2.z
    #define WhiteClip       _ToneMapCoeffs2.w

    struct appdata_t
    {
        float4 vertex : POSITION;
        float2 texcoord : TEXCOORD0;
    };

    struct v2f
    {
        float2 texcoord : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };

    float DistanceToSplit(float2 pos, float3 splitPlane)
    {
        return dot(float3(pos, 1), splitPlane);
    }

    bool IsInsideGizmo(float2 normalizedCoord, float absDistanceToPlane, float distanceFromCenter, float side, float3 orthoPlane, float gizmoCircleRadius, float gizmoThickness, out float outSmoothing, float mode)
    {
        bool result = false;
        outSmoothing = 0.0;
        if (absDistanceToPlane < gizmoCircleRadius) // First "thick" bar, as large as the radius at extremities.
        {
            if (distanceFromCenter < (_GizmoLength + gizmoCircleRadius))
            {
                // side < 0 is cyan circle, side > 0 is orange widget
                if (mode == kAll ||
                    (mode == kRotationZone1 && side > 0) ||
                    (mode == kRotationZone2 && side < 0))
                {
                    if (distanceFromCenter >= (_GizmoLength - gizmoCircleRadius)) // Inside circle at the extremities ?
                    {
                        float2 circleCenter = _GizmoPosition.xy + side * orthoPlane.xy * _GizmoLength;
                        float d = length(normalizedCoord - circleCenter);
                        if (d <= gizmoCircleRadius)
                        {
                            outSmoothing = smoothstep(1.0, 0.8, d / gizmoCircleRadius);
                            result = true;
                        }
                    }
                }

                if (mode == kAll || mode == kTranslation)
                {
                    if (absDistanceToPlane < gizmoThickness && distanceFromCenter < _GizmoLength)
                    {
                        outSmoothing = max(outSmoothing, smoothstep(1.0, 0.0, absDistanceToPlane / gizmoThickness));
                        result = true;
                    }
                }
            }
        }

        return result;
    }

    float4 GetGizmoColor(float2 normalizedCoord, float3 splitPlane, float3 orthoPlane)
    {
        float distanceToPlane = DistanceToSplit(normalizedCoord, splitPlane);
        float absDistanceToPlane = abs(distanceToPlane);
        float distanceFromCenter = length(normalizedCoord.xy - _GizmoPosition.xy);
        float distanceToOrtho = DistanceToSplit(normalizedCoord, orthoPlane);

        float4 result = float4(0.0, 0.0, 0.0, 0.0);
        float side = 0.0;
        if (distanceToOrtho > 0.0)
        {
            result.rgb = _FirstViewColor.rgb;
            side = 1.0;
        }
        else
        {
            result.rgb = _SecondViewColor.rgb;
            side = -1.0;
        }

        result.a = 0.0;

        // "normal" gizmo
        float smoothing = 1.0;
        if (IsInsideGizmo(normalizedCoord, absDistanceToPlane, distanceFromCenter, side, orthoPlane, _GizmoCircleRadius.x, _GizmoThickness.x, smoothing, kAll))
        {
            result.a = 1.0 * smoothing;
        }

        // large gizmo when in translation mode
        if (IsInsideGizmo(normalizedCoord, absDistanceToPlane, distanceFromCenter, side, orthoPlane, _GizmoCircleRadius.y, _GizmoThickness.y, smoothing, _GizmoRenderMode))
        {
            result.a = max(result.a, 0.25 * smoothing);
        }

        // Blend factor selection disc
        float2 blendCircleCenter = _GizmoPosition.xy - _CompositingParams.x * orthoPlane.xy * _GetBlendFactorMaxGizmoDistance;
        float distanceToBlendCircle = length(normalizedCoord.xy - blendCircleCenter);
        if (distanceToBlendCircle < _BlendFactorCircleRadius)
        {
            float alpha = smoothstep(1.0, 0.6, distanceToBlendCircle / _BlendFactorCircleRadius);
            result = lerp(result, float4(1.0, 1.0, 1.0, alpha), alpha);
        }

        // Display transparent disc if near the center where the blend factor selection disc will automatically snap back
        if (abs(_CompositingParams.x) < _GizmoCircleRadius.y / _GetBlendFactorMaxGizmoDistance)
        {
            if (distanceFromCenter < _BlendFactorCircleRadius)
            {
                float alpha = smoothstep(1.0, 0.6, distanceFromCenter / _BlendFactorCircleRadius) * 0.75;
                result = lerp(result, float4(1.0, 1.0, 1.0, alpha), alpha);
            }
        }

        return result;
    }

    float GetZoneViewFeedbackCircleFactor(float2 normalizedCoord, float radius, float circleSize)
    {
        float distanceToCenter = abs(length(_GizmoZoneCenter.xy - normalizedCoord) - radius);
        return saturate((circleSize - distanceToCenter) / circleSize);
    }

    float ComputeBorderFactor(float borderSize, float2 screenPos, bool sideBySideView)
    {
        float4 borderSize4 = float4(borderSize, borderSize, borderSize, borderSize);
        float4 distanceToBorder = float4(screenPos.x, screenPos.y, abs(_ScreenRatio.z - screenPos.x), abs(_ScreenRatio.w - screenPos.y));

        float4 factors = saturate((borderSize4 - distanceToBorder) / borderSize4); // Lerp from 1.0 to 0.0 alpha from screen border to border size
        float factor = max(factors.x, max(factors.y, max(factors.z, factors.w)));

        // Add middle of the screen for side by side view
        if (sideBySideView)
        {
            float distanceToCenterLine = abs(_ScreenRatio.z * 0.5 - screenPos.x);
            float factorForCenterLine = saturate((borderSize - distanceToCenterLine) / borderSize);
            factor = max(factor, factorForCenterLine);
        }

        return factor;
    }

    float ComputeSelectedSideColorFactor(float side, float2 screenPos, float2 normalizedCoord, bool sideBySideView, bool zoneView)
    {
        float borderSize = 2.0;
        bool selectedSide = side * _CompositingParams.w > 0.0;

        float factor = ComputeBorderFactor(borderSize, screenPos, sideBySideView);

        // Add circle for zone view
        if (zoneView)
        {
            float selectionCircleFeedbackFactor = GetZoneViewFeedbackCircleFactor(normalizedCoord, _GizmoCircleRadius.y, 0.002);
            factor = max(factor, selectionCircleFeedbackFactor);
        }

        // If not on the selected side, make it more transparent
        if (!selectedSide)
        {
            factor = factor * 0.2;
        }

        return factor;
    }

    float4 ComputeDragColorFactor(float side, float2 screenPos, float2 normalizedCoord, bool sideBySideView, bool zoneView)
    {
        float factor = 0;
        float borderSize = 40.0;
        bool sideIsDragZone = (side > 0.0 && _CompositingParams2.x > 0.0) || (side < 0.0 && _CompositingParams2.x < 0.0);
        if (sideIsDragZone)
        {
            factor = ComputeBorderFactor(borderSize, screenPos, sideBySideView);

            // Add circle for zone view
            if (zoneView && side < 0.0)
            {
                float feedbackRadius = _GizmoLength * 2.0 * 0.3; // make it proprtional to selection zone
                factor = max(factor, GetZoneViewFeedbackCircleFactor(normalizedCoord, _GizmoLength * 2.0, feedbackRadius));
            }

            factor = pow(factor, 8) * 0.7; // Casimir magics values for optimum fadeout :)
        }

        return factor;
    }

    float4 ComputeFeedbackColor(float4 inputColor, float side, float2 screenPos, float2 normalizedCoord, bool sideBySideView, bool zoneView)
    {
        float factor = ComputeSelectedSideColorFactor(side, screenPos, normalizedCoord, sideBySideView, zoneView);
        factor = max(factor, ComputeDragColorFactor(side, screenPos, normalizedCoord, sideBySideView, zoneView));

        float4 result = float4(0.0, 0.0, 0.0, 0.0);
        if (side > 0.0)
        {
            result = lerp(inputColor, _FirstViewColor, factor);
        }
        else
        {
            result = lerp(inputColor, _SecondViewColor, factor);
        }

        return result;
    }

    v2f vert(appdata_t IN)
    {
        v2f OUT;
        OUT.vertex = UnityObjectToClipPos(IN.vertex);
        OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _Tex0WithSun);
        return OUT;
    }

    float3 evalCurve(float3 x, float A, float B, float C, float D, float E, float F)
    {
        return ((x*(A*x + C*B) + D*E) / (x*(A*x + B) + D*F)) - E / F;
    }

    float3 applyTonemapFilmicAD(float3 linearColor)
    {
        float blackRatio = InBlack / OutBlack;
        float whiteRatio = InWhite / OutWhite;

        // blend tunable coefficients
        float B = lerp(0.57, 0.37, blackRatio);
        float C = lerp(0.01, 0.24, whiteRatio);
        float D = lerp(0.02, 0.20, blackRatio);

        // constants
        float A = 0.2;
        float E = 0.02;
        float F = 0.30;

        // eval and correct for white point
        float3 whiteScale = 1.0f / evalCurve(WhiteLevel, A, B, C, D, E, F);
        float3 curr = evalCurve(linearColor *whiteScale, A, B, C, D, E, F);

        return curr*whiteScale;
    }

    float3 remapWhite(float3 inPixel, float whitePt)
    {
        //  var breakout for readability
        const float inBlack = 0;
        const float outBlack = 0;
        float inWhite = whitePt;
        const float outWhite = 1;

        // remap input range to output range
        float3 outPixel = ((inPixel.rgb) - inBlack.xxx) / (inWhite.xxx - inBlack.xxx) * (outWhite.xxx - outBlack.xxx) + outBlack.xxx;
        return (outPixel.rgb);
    }

    float3 NeutralTonemap(float3 x)
    {
        float3 finalColor = applyTonemapFilmicAD(x); // curve (dynamic coeffs differ per level)
        finalColor = remapWhite(finalColor, WhiteClip); // post-curve white point adjustment
        finalColor = saturate(finalColor);
        return finalColor;
    }

    float3 ApplyToneMap(float3 color)
    {
        if (_CompositingParams2.y > 0.0)
        {
            return NeutralTonemap(color);
        }
        else
        {
            return saturate(color);
        }
    }

    float3 ComputeColor(sampler2D texNormal, sampler2D texWithoutSun, sampler2D texShadowMask, float shadowMultiplier, float4 shadowColor, float2 texcoord)
    {
        // Explanation of how this work:
        // To simulate the shadow of a directional light, we want to interpolate between two environments. One with a skybox without sun for shadowed area and the other with the sun.
        // To create the lerp mask we render the scene with a white diffuse material and a single shadow casting directional light.
        // This will create a mask where the shadowed area is 0 and the lit area is 1 with a smooth NDotL transition in-between.
        // However, the DNotL will create an unwanted darkening of the scene (it's not actually part of the lighting equation)
        // so we sqrt it in order to avoid too much darkening.
        float3 color = tex2D(texNormal, texcoord).rgb;
        float3 colorWithoutsun = tex2D(texWithoutSun, texcoord).rgb;
        float3 shadowMask = sqrt(tex2D(texShadowMask, texcoord).rgb);
        return lerp(colorWithoutsun * shadowColor.rgb * shadowMultiplier, color, saturate(shadowMask.r));
    }

    ENDCG

    SubShader
    {
        Tags
        {
            "ForceSupported" = "True"
        }

        Lighting Off
        Cull Off
        ZTest Always
        ZWrite Off
        Blend One Zero

        // Single view 1
        Pass
        {
            CGPROGRAM
            #pragma fragment frag
            #pragma target 3.0

            float4 frag(float2 texcoord : TEXCOORD0,
            UNITY_VPOS_TYPE vpos : VPOS) : COLOR
            {
                float4 color = float4(ComputeColor(_Tex0WithSun, _Tex0WithoutSun, _Tex0Shadows, ShadowMultiplier0, _ShadowColor0, texcoord) * exp2(ExposureValue1), 1.0);
                color.rgb = ApplyToneMap(color.rgb);
                color = ComputeFeedbackColor(color, 1.0, vpos.xy, float2(0.0, 0.0), false, false);
                return color;
            }
            ENDCG
        }

        // Single view 2
        Pass
        {
            CGPROGRAM
            #pragma fragment frag
            #pragma target 3.0

            float4 frag(float2 texcoord : TEXCOORD0,
                        UNITY_VPOS_TYPE vpos : VPOS) : COLOR
            {
                float4 color = float4(ComputeColor(_Tex1WithSun, _Tex1WithoutSun, _Tex1Shadows, ShadowMultiplier1, _ShadowColor1, texcoord) * exp2(ExposureValue2), 1.0);
                color.rgb = ApplyToneMap(color.rgb);
                color = ComputeFeedbackColor(color, -1.0, vpos.xy, float2(0.0, 0.0), false, false);
                return color;
            }
            ENDCG
        }

        // split
        Pass
        {
            CGPROGRAM
            #pragma fragment frag
            #pragma target 3.0

            float4 frag(float2 texcoord : TEXCOORD0,
            UNITY_VPOS_TYPE vpos : VPOS) : COLOR
            {
                float3 color1 = ComputeColor(_Tex0WithSun, _Tex0WithoutSun, _Tex0Shadows, ShadowMultiplier0, _ShadowColor0, texcoord) * exp2(ExposureValue1);
                float3 color2 = ComputeColor(_Tex1WithSun, _Tex1WithoutSun, _Tex1Shadows, ShadowMultiplier1, _ShadowColor1, texcoord) * exp2(ExposureValue2);

                float2 normalizedCoord = ((texcoord * 2.0 - 1.0) * _ScreenRatio.xy);

                float side = DistanceToSplit(normalizedCoord, _GizmoSplitPlane) < 0.0f ? -1.0f : 1.0f;
                float blendFactor = 0.0f;
                if (side < 0.0)
                {
                    blendFactor = 1.0 - saturate(side * _CompositingParams.x);
                }
                else
                {
                    blendFactor = saturate(side * _CompositingParams.x);
                }

                float4 finalColor = float4(lerp(color1, color2, blendFactor), 1.0);
                finalColor.rgb = ApplyToneMap(finalColor.rgb);

                float4 gizmoColor = GetGizmoColor(normalizedCoord, _GizmoSplitPlane, _GizmoSplitPlaneOrtho);
                finalColor = lerp(finalColor, gizmoColor, gizmoColor.a);
                finalColor = ComputeFeedbackColor(finalColor, side, vpos.xy, float2(0.0, 0.0), false, false);

                return finalColor;
            }
            ENDCG
        }
    }
}
