REAL3 ADD_FUNC_SUFFIX(ADD_NORMAL_FUNC_SUFFIX(SampleUVMappingNormal))(TEXTURE2D_ARGS(textureName, samplerName), UVMapping uvMapping, REAL scale, REAL param)
{
    if (uvMapping.mappingType == UV_MAPPING_TRIPLANAR)
    {
        REAL3 triplanarWeights = uvMapping.triplanarWeights;

#ifdef SURFACE_GRADIENT
        REAL2 derivXplane;
        REAL2 derivYPlane;
        REAL2 derivZPlane;
        derivXplane = derivYPlane = derivZPlane = REAL2(0.0, 0.0);

        if (triplanarWeights.x > 0.0)
            derivXplane = triplanarWeights.x * UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvZY, param), scale);
        if (triplanarWeights.y > 0.0)
            derivYPlane = triplanarWeights.y * UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvXZ, param), scale);
        if (triplanarWeights.z > 0.0)
            derivZPlane = triplanarWeights.z * UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvXY, param), scale);

        // Assume derivXplane, derivYPlane and derivZPlane sampled using (z,y), (z,x) and (x,y) respectively.
        // TODO: Check with morten convention! Do it follow ours ?
        REAL3 volumeGrad = REAL3(derivZPlane.x + derivYPlane.y, derivZPlane.y + derivXplane.y, derivXplane.x + derivYPlane.x);
        return SurfaceGradientFromVolumeGradient(uvMapping.normalWS, volumeGrad);
#else
        REAL3 val = REAL3(0.0, 0.0, 0.0);

        if (triplanarWeights.x > 0.0)
            val += triplanarWeights.x * UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvZY, param), scale);
        if (triplanarWeights.y > 0.0)
            val += triplanarWeights.y * UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvXZ, param), scale);
        if (triplanarWeights.z > 0.0)
            val += triplanarWeights.z * UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvXY, param), scale);

        return normalize(val);
#endif
    }
#ifdef SURFACE_GRADIENT
    else if (uvMapping.mappingType == UV_MAPPING_PLANAR)
    {
        // Note: Planar is on uv coordinate (and not uvXZ)
        REAL2 derivYPlane = UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uv, param), scale);
        // See comment above
        REAL3 volumeGrad = REAL3(derivYPlane.y, 0.0, derivYPlane.x);
        return SurfaceGradientFromVolumeGradient(uvMapping.normalWS, volumeGrad);
    }
#endif
    else
    {
#ifdef SURFACE_GRADIENT
        REAL2 deriv = UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uv, param), scale);
        return SurfaceGradientFromTBN(deriv, uvMapping.tangentWS, uvMapping.bitangentWS);
#else
        return UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uv, param), scale);
#endif
    }
}
