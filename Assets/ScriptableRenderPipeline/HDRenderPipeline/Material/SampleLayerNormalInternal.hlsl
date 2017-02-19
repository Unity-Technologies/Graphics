float3 ADD_FUNC_SUFFIX(ADD_NORMAL_FUNC_SUFFIX(SampleLayerNormal))(TEXTURE2D_ARGS(layerTex, layerSampler), LayerUV layerUV, CommonLayerUV common, float scale, float param)
{
    if (layerUV.isTriplanar)
    {
        float3 triplanarWeights = common.triplanarWeights;

#ifdef SURFACE_GRADIENT
        float2 derivXplane;
        float2 derivYPlane;
        float2 derivZPlane;
        derivXplane = derivYPlane = derivZPlane = float2(0.0, 0.0);

        if (triplanarWeights.x > 0.0)
            derivXplane = triplanarWeights.x * UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvZY, param), scale);
        if (triplanarWeights.y > 0.0)
            derivYPlane = triplanarWeights.y * UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvXZ, param), scale);
        if (triplanarWeights.z > 0.0)
            derivZPlane = triplanarWeights.z * UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvXY, param), scale);

        // Assume derivXplane, derivYPlane and derivZPlane sampled using (z,y), (z,x) and (x,y) respectively.
        // TODO: Check with morten convention! Do it follow ours ?
        float3 volumeGrad = float3(derivZPlane.x + derivYPlane.y, derivZPlane.y + derivXplane.y, derivXplane.x + derivYPlane.x);
        return surfgradFromVolumeGradient(layerUV.vertexNormalWS, volumeGrad);
#else
        float3 val = float3(0.0, 0.0, 0.0);

        if (triplanarWeights.x > 0.0)
            val += triplanarWeights.x * UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvZY, param), scale);
        if (triplanarWeights.y > 0.0)
            val += triplanarWeights.y * UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvXZ, param), scale);
        if (triplanarWeights.z > 0.0)
            val += triplanarWeights.z * UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvXY, param), scale);

        return normalize(val);
#endif
    }
#ifdef SURFACE_GRADIENT
    else if (layerUV.isPlanar)
    {
        float2 derivYPlane = UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uvXZ, param), scale);
        // See comment above
        float3 volumeGrad = float3(derivYPlane.y, 0.0, derivYPlane.x);
        return surfgradFromVolumeGradient(layerUV.vertexNormalWS, volumeGrad);
    }
#endif
    else
    {
#ifdef SURFACE_GRADIENT
        float2 deriv = UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uv, param), scale);
        return SurfaceGradientFromTBN(deriv, layerUV.vT, layerUV.vB);
#else
        return UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV.uv, param), scale);
#endif
    }
}