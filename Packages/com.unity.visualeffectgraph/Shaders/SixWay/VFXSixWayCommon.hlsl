float3 ApplyLightMapContrast(float3 originalValue, float2 remapControls)
{
    const bool3 overThreshold = originalValue > remapControls.x;
    float3 X = overThreshold ? float3(1,1,1) - originalValue : originalValue;
    float3 C = overThreshold ? float3(1,1,1) - remapControls.x : remapControls.x;
    float3 O = C * pow(saturate(X) / (C + VFX_EPSILON), remapControls.y);
    O = overThreshold ? float3(1,1,1) - O : O;

    return O;
}

void RemapLightMaps( inout float3 rightTopBack, inout float3 leftBottomFront, float2 remapControls)
{
    rightTopBack = ApplyLightMapContrast(rightTopBack, remapControls);
    leftBottomFront = ApplyLightMapContrast(leftBottomFront, remapControls);
}
void RemapLightMaps( inout float3 rightTopBack, inout float3 leftBottomFront, float4 remapCurve)
{
    [unroll]
    for(int i = 0; i < 3; i++)
    {
        rightTopBack[i] = SampleCurve(remapCurve, rightTopBack[i]);
        leftBottomFront[i] = SampleCurve(remapCurve, leftBottomFront[i]);
    }
}

void RemapLightMapsRangesFrom( inout float3 rightTopBack, inout float3 leftBottomFront, float alpha, float4 remapRanges)
{
    rightTopBack = RangeRemap(remapRanges.xxx ,remapRanges.yyy, rightTopBack);
    leftBottomFront = RangeRemap(remapRanges.xxx ,remapRanges.yyy, leftBottomFront);
}

void RemapLightMapsRangesTo( inout float3 rightTopBack, inout float3 leftBottomFront, float alpha, float4 remapRanges)
{
    rightTopBack = RangeRemapFrom01(remapRanges.zzz, remapRanges.www, rightTopBack);
    leftBottomFront = RangeRemapFrom01(remapRanges.zzz, remapRanges.www, leftBottomFront);

    rightTopBack = max(0.0f, rightTopBack);
    leftBottomFront = max(0.0f, leftBottomFront);
}

void SixWaySwapUV(inout float3 rightTopBack, inout float3 leftBottomFront)
{
    float right = rightTopBack.y;
    float top = leftBottomFront.x;
    float left = leftBottomFront.y;
    float bottom = rightTopBack.x;
    rightTopBack.x = right;
    leftBottomFront.x = left;
    rightTopBack.y = top;
    leftBottomFront.y = bottom;
}
