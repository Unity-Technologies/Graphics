#ifndef UNITY_BLOOM
#define UNITY_BLOOM

// Downsample with a 4x4 box filter
float3 DownsampleFilter(TEXTURE2D_ARGS(tex, texSampler), float2 uv, float2 texelSize)
{
    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0);

    float3 s;
    s =  SAMPLE_TEXTURE2D(tex, texSampler, uv + d.xy).rgb;
    s += SAMPLE_TEXTURE2D(tex, texSampler, uv + d.zy).rgb;
    s += SAMPLE_TEXTURE2D(tex, texSampler, uv + d.xw).rgb;
    s += SAMPLE_TEXTURE2D(tex, texSampler, uv + d.zw).rgb;

    return s * (1.0 / 4.0);
}

float3 UpsampleFilter(TEXTURE2D_ARGS(tex, texSampler), float2 uv, float2 texelSize, float sampleScale)
{
#if 0
    // 4-tap bilinear upsampler
    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * (sampleScale * 0.5);

    float3 s;
    s =  SAMPLE_TEXTURE2D(tex, texSampler, uv + d.xy).rgb;
    s += SAMPLE_TEXTURE2D(tex, texSampler, uv + d.zy).rgb;
    s += SAMPLE_TEXTURE2D(tex, texSampler, uv + d.xw).rgb;
    s += SAMPLE_TEXTURE2D(tex, texSampler, uv + d.zw).rgb;

    return s * (1.0 / 4.0);
#else
    // 9-tap bilinear upsampler (tent filter)
    float4 d = texelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0) * sampleScale;

    float3 s;
    s =  SAMPLE_TEXTURE2D(tex, texSampler, uv - d.xy).rgb;
    s += SAMPLE_TEXTURE2D(tex, texSampler, uv - d.wy).rgb * 2.0;
    s += SAMPLE_TEXTURE2D(tex, texSampler, uv - d.zy).rgb;

    s += SAMPLE_TEXTURE2D(tex, texSampler, uv + d.zw).rgb * 2.0;
    s += SAMPLE_TEXTURE2D(tex, texSampler, uv).rgb        * 4.0;
    s += SAMPLE_TEXTURE2D(tex, texSampler, uv + d.xw).rgb * 2.0;

    s += SAMPLE_TEXTURE2D(tex, texSampler, uv + d.zy).rgb;
    s += SAMPLE_TEXTURE2D(tex, texSampler, uv + d.wy).rgb * 2.0;
    s += SAMPLE_TEXTURE2D(tex, texSampler, uv + d.xy).rgb;

    return s * (1.0 / 16.0);
#endif
}

#endif // UNITY_BLOOM
