


// TODO: Handle BC5 format, currently this code is for DXT5nm
// THis function below must call UnpackNormalmapRGorAG
float3 ADD_FUNC_SUFFIX(SampleLayerNormal)(TEXTURE2D_ARGS(layerTex, layerSampler), LayerUV layerUV, float3 weights, float scale, float bias)
{
    if (layerUV.isTriplanar)
    {
        float3 val = float3(0.0, 0.0, 0.0);

        if (weights.x > 0.0)
            val += weights.x * UnpackNormalAG(NORMAL_SAMPLE_FUNC(layerTex, layerSampler, layerUV.uvYZ, bias), scale);
        if (weights.y > 0.0)
            val += weights.y * UnpackNormalAG(NORMAL_SAMPLE_FUNC(layerTex, layerSampler, layerUV.uvZX, bias), scale);
        if (weights.z > 0.0)
            val += weights.z * UnpackNormalAG(NORMAL_SAMPLE_FUNC(layerTex, layerSampler, layerUV.uvXY, bias), scale);

        return normalize(val);
    }
    else
    {
        return UnpackNormalAG(NORMAL_SAMPLE_FUNC(layerTex, layerSampler, layerUV.uv, bias), scale);
    }
}

// This version is for normalmap with AG encoding only (use with details map)
float3 ADD_FUNC_SUFFIX(SampleLayerNormalAG)(TEXTURE2D_ARGS(layerTex, layerSampler), LayerUV layerUV, float3 weights, float scale, float bias)
{
    if (layerUV.isTriplanar)
    {
        float3 val = float3(0.0, 0.0, 0.0);

        if (weights.x > 0.0)
            val += weights.x * UnpackNormalAG(NORMAL_SAMPLE_FUNC(layerTex, layerSampler, layerUV.uvYZ, bias), scale);
        if (weights.y > 0.0)
            val += weights.y * UnpackNormalAG(NORMAL_SAMPLE_FUNC(layerTex, layerSampler, layerUV.uvZX, bias), scale);
        if (weights.z > 0.0)
            val += weights.z * UnpackNormalAG(NORMAL_SAMPLE_FUNC(layerTex, layerSampler, layerUV.uvXY, bias), scale);

        return normalize(val);
    }
    else
    {
        return UnpackNormalAG(NORMAL_SAMPLE_FUNC(layerTex, layerSampler, layerUV.uv, bias), scale);
    }
}

// This version is for normalmap with RGB encoding only, i.e non encoding. It is necessary to use this abstraction to handle correctly triplanar
// plus consistent with the normal scale parameter
float3 ADD_FUNC_SUFFIX(SampleLayerNormalRGB)(TEXTURE2D_ARGS(layerTex, layerSampler), LayerUV layerUV, float3 weights, float scale, float bias)
{
    if (layerUV.isTriplanar)
    {
        float3 val = float3(0.0, 0.0, 0.0);

        if (weights.x > 0.0)
            val += weights.x * UnpackNormalRGB(NORMAL_SAMPLE_FUNC(layerTex, layerSampler, layerUV.uvYZ, bias), scale);
        if (weights.y > 0.0)
            val += weights.y * UnpackNormalRGB(NORMAL_SAMPLE_FUNC(layerTex, layerSampler, layerUV.uvZX, bias), scale);
        if (weights.z > 0.0)
            val += weights.z * UnpackNormalRGB(NORMAL_SAMPLE_FUNC(layerTex, layerSampler, layerUV.uvXY, bias), scale);

        return normalize(val);
    }
    else
    {
        return UnpackNormalRGB(NORMAL_SAMPLE_FUNC(layerTex, layerSampler, layerUV.uv, bias), scale);
    }
}
