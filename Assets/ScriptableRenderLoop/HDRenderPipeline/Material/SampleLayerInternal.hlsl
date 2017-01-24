
// These functions are use to hide the handling of triplanar mapping
// Normal need a specific treatment as they use special encoding for both base and detail map
// Also we use multiple inclusion to handle the various variation for lod and bias

// param can be unused, lod or bias
float4 ADD_FUNC_SUFFIX(SampleLayer)(TEXTURE2D_ARGS(layerTex, layerSampler), LayerUV layerUV, float3 weights, float param)
{
    if (layerUV.isTriplanar)
    {
        float4 val = float4(0.0, 0.0, 0.0, 0.0);

        if (weights.x > 0.0)
            val += weights.x * SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvYZ, param);
        if (weights.y > 0.0)
            val += weights.y * SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvZX, param);
        if (weights.z > 0.0)
            val += weights.z * SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvXY, param);

        return val;
    }
    else
    {
        return SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uv, param);
    }
}

// TODO: Handle BC5 format, currently this code is for DXT5nm - After the change, rename this function UnpackNormalmapRGorAG
// This version is use for the base normal map
float3 ADD_FUNC_SUFFIX(SampleLayerNormal)(TEXTURE2D_ARGS(layerTex, layerSampler), LayerUV layerUV, float3 weights, float scale, float param)
{
    if (layerUV.isTriplanar)
    {
        float3 val = float3(0.0, 0.0, 0.0);

        if (weights.x > 0.0)
            val += weights.x * UnpackNormalAG(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvYZ, param), scale);
        if (weights.y > 0.0)
            val += weights.y * UnpackNormalAG(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvZX, param), scale);
        if (weights.z > 0.0)
            val += weights.z * UnpackNormalAG(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvXY, param), scale);

        return normalize(val);
    }
    else
    {
        return UnpackNormalAG(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uv, param), scale);
    }
}

// This version is for normalmap with AG encoding only. Mainly use with details map.
float3 ADD_FUNC_SUFFIX(SampleLayerNormalAG)(TEXTURE2D_ARGS(layerTex, layerSampler), LayerUV layerUV, float3 weights, float scale, float param)
{
    if (layerUV.isTriplanar)
    {
        float3 val = float3(0.0, 0.0, 0.0);

        if (weights.x > 0.0)
            val += weights.x * UnpackNormalAG(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvYZ, param), scale);
        if (weights.y > 0.0)
            val += weights.y * UnpackNormalAG(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvZX, param), scale);
        if (weights.z > 0.0)
            val += weights.z * UnpackNormalAG(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvXY, param), scale);

        return normalize(val);
    }
    else
    {
        return UnpackNormalAG(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uv, param), scale);
    }
}

// This version is for normalmap with RGB encoding only, i.e uncompress or BC7. Mainly used for object space normal.
float3 ADD_FUNC_SUFFIX(SampleLayerNormalRGB)(TEXTURE2D_ARGS(layerTex, layerSampler), LayerUV layerUV, float3 weights, float scale, float param)
{
    if (layerUV.isTriplanar)
    {
        float3 val = float3(0.0, 0.0, 0.0);

        if (weights.x > 0.0)
            val += weights.x * UnpackNormalRGB(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvYZ, param), scale);
        if (weights.y > 0.0)
            val += weights.y * UnpackNormalRGB(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvZX, param), scale);
        if (weights.z > 0.0)
            val += weights.z * UnpackNormalRGB(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvXY, param), scale);

        return normalize(val);
    }
    else
    {
        return UnpackNormalRGB(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uv, param), scale);
    }
}
