

float4 DiffFloat4(float4 expected, float4 actual, float sensitivity = 1000.0f)
{
#if _MODE_EXPECTED
    return expected;
#elif _MODE_ACTUAL
    return actual;
#else // _MODE_DIFF
    float4 delta = abs(expected - actual);
    float max_delta = max(max(delta.x, delta.y), max(delta.z, delta.w));
    float alpha = saturate(max_delta * sensitivity);
    float4 green = float4(0.0f, 1.0f, 0.0f, 1.0f);
    float4 red = float4(1.0f, 0.0f, 0.0f, 1.0f);
    return lerp(green, red, alpha);
#endif
}


float4 DiffFloat3(float3 expected, float3 actual, float sensitivity = 1000.0f)
{
#if _MODE_EXPECTED
    return float4(expected.rgb, 1.0f);
#elif _MODE_ACTUAL
    return float4(actual.rgb, 1.0f);
#else // _MODE_DIFF
    float3 delta = abs(expected - actual);
    float max_delta = max(max(delta.x, delta.y), delta.z);
    float alpha = saturate(max_delta * sensitivity);
    float4 green = float4(0.0f, 1.0f, 0.0f, 1.0f);
    float4 red = float4(1.0f, 0.0f, 0.0f, 1.0f);
    return lerp(green, red, alpha);
#endif
}


float4 DiffFloat2(float2 expected, float2 actual, float sensitivity = 1000.0f)
{
#if _MODE_EXPECTED
    return float4(expected.rg, 0.0f, 1.0f);
#elif _MODE_ACTUAL
    return float4(actual.rg, 0.0f, 1.0f);
#else // _MODE_DIFF
    float2 delta = abs(expected - actual);
    float max_delta = max(delta.x, delta.y);
    float alpha = saturate(max_delta * sensitivity);
    float4 green = float4(0.0f, 1.0f, 0.0f, 1.0f);
    float4 red = float4(1.0f, 0.0f, 0.0f, 1.0f);
    return lerp(green, red, alpha);
#endif
}


float4 DiffFloat(float expected, float actual, float sensitivity = 1000.0f)
{
#if _MODE_EXPECTED
    return float4(expected, 0.0f, 0.0f, 1.0f);
#elif _MODE_ACTUAL
    return float4(actual, 0.0f, 0.0f, 1.0f);
#else // _MODE_DIFF
    float delta = abs(expected - actual);
    float max_delta = delta;
    float alpha = saturate(max_delta * sensitivity);
    float4 green = float4(0.0f, 1.0f, 0.0f, 1.0f);
    float4 red = float4(1.0f, 0.0f, 0.0f, 1.0f);
    return lerp(green, red, alpha);
#endif
}
