TEXTURE3D(_PreIntegratedEyeCaustic);
SAMPLER(_CausticLUT_trilinear_clamp_sampler);

float ComputeCausticFromLUT(float2 irisPlanePosition, float irisHeight, float3 lightPosOS, float intensityMultiplier)
{
    //these need to match with the values LUT was generated with
    float causticLutThetaMin = -0.5f; //LUT generated with last slice 30 degrees below horizon, ie. cos(pi * 0.5 + 30 * toRadians) == -0.5
    bool causticMirrorV = true;
    float causticScleraMargin = 0.15f;

    lightPosOS.z -= irisHeight;

    float3 lightDirOS = normalize(lightPosOS);

    float2 xAxis = normalize(lightDirOS.xy);
    float2 yAxis = float2(-xAxis.y, xAxis.x);

    float cosTheta = lightDirOS.z;

    float w = (cosTheta - causticLutThetaMin) / (1.f - causticLutThetaMin);

    //fadeout when the light moves past the last LUT slice
    float blendToBlack = lerp(1.f, 0.f, saturate(-w * 10.f));

    w = saturate(1.f - w);
    float2 uv = irisPlanePosition;

    //orient and map from [-1, 1] -> [0,1]
    uv = float2(dot(uv, xAxis), dot(uv, yAxis));

    //caustic LUT has potentially mirrored V coordinate
    if(causticMirrorV)
    {
        uv.y = abs(uv.y) * 2.f - 1.f;
    }

    uv = uv * 0.5f + 0.5f;

    // margin at the U to have space for caustic hilight outside of cornea area
    uv.x *= 1.f - causticScleraMargin;
    uv.x += causticScleraMargin;

    float c = SAMPLE_TEXTURE3D_LOD(_PreIntegratedEyeCaustic, _CausticLUT_trilinear_clamp_sampler, float3(uv.x, uv.y,1.f - w), 0).x * intensityMultiplier;

    //clamp borders to black (uv.x < 0 smoothstepped since we might not have given enough margin in LUT for the sclera hotspot to falloff. Capturing it completely would waste a lot of space in the LUT)
    float2 bc = (step(0, uv.y) * step(uv, 1));
    c *= bc.x * bc.y * smoothstep(-0.2f, 0.0f, uv.x);
    c *= blendToBlack;
    return c;
}

float3 ApplyCausticToDiffuse(float3 diffuse, float causticIntensity, float corneaMask, float blend)
{
    return lerp(corneaMask, causticIntensity, blend) * diffuse;
}
