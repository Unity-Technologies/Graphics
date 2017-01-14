float4 TessellationEdge(float3 p0, float3 p1, float3 p2, float3 n0, float3 n1, float3 n2)
{
    // TODO: Handle inverse culling (for mirror)!
    if (_TessellationBackFaceCullEpsilon > -0.99) // Is backface culling enabled ?
    {
        if (BackFaceCullTriangle(p0, p1, p2, _TessellationBackFaceCullEpsilon, _WorldSpaceCameraPos))
        {
            return float4(0.0, 0.0, 0.0, 0.0);
        }
    }

    float3 tessFactor = float3(1.0, 1.0, 1.0);

    // Aaptive tessellation
    if (_TessellationFactorTriangleSize > 0.0)
    {
        // return a value between 0 and 1
        tessFactor *= GetScreenSpaceTessFactor( p0, p1, p2, GetWorldToHClipMatrix(), _ScreenParams,  _TessellationFactorTriangleSize);
    }

    if (_TessellationFactorMaxDistance > 0.0)
    {
        tessFactor *= GetDistanceBasedTessFactor(p0, p1, p2, _WorldSpaceCameraPos, _TessellationFactorMinDistance, _TessellationFactorMaxDistance);
    }

    tessFactor *= _TessellationFactor;

    // Clamp to be minimun 0.01
    tessFactor.xyz = float3(max(0.01, tessFactor.x), max(0.01, tessFactor.y), max(0.01, tessFactor.z));

    return CalcTriEdgeTessFactors(tessFactor);
}

float3 GetDisplacement(VaryingsMeshToDS input)
{
    // This call will work for both LayeredLit and Lit shader
    LayerTexCoord layerTexCoord;
    GetLayerTexCoord(
#ifdef VARYINGS_DS_NEED_TEXCOORD0
        input.texCoord0,
#else
        float2(0.0, 0.0),
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
        input.texCoord1,
#else
        float2(0.0, 0.0),
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2
        input.texCoord2,
#else
        float2(0.0, 0.0),
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3
        input.texCoord3,
#else
        float2(0.0, 0.0),
#endif
        input.positionWS, 
#ifdef VARYINGS_DS_NEED_NORMAL
        input.normalWS,
#else
        float3(0.0, 0.0, 1.0),
#endif
        layerTexCoord);

    // TODO: For now just use Layer0, but we are suppose to apply the same heightmap blending than in the pixel shader
#ifdef _HEIGHTMAP
    // TODO test mip lod to reduce texture cache miss
    // TODO: Move to camera relative and change distance to length
    //float dist = distance(input.positionWS, cameraPosWS);
    // No ddx/ddy to calculate LOD, use camera distance instead
    //float fadeDist = _TessellationFactorMaxDistance - _TessellationFactorMinDistance;
    //float heightMapLod = saturate((dist - _TessellationFactorMinDistance) / min(fadeDist, 0.01)) * 6; // 6 is an arbitrary number here
    float heightMapLod = 0.0;

    float height = (SAMPLE_LAYER_TEXTURE2D_LOD(ADD_ZERO_IDX(_HeightMap), ADD_ZERO_IDX(sampler_HeightMap), ADD_ZERO_IDX(layerTexCoord.base), heightMapLod).r - ADD_ZERO_IDX(_HeightCenter)) * ADD_ZERO_IDX(_HeightAmplitude);
#else
    float height = 0.0;
#endif

#ifdef VARYINGS_DS_NEED_NORMAL
    return height * input.normalWS;
#else
    return float3(0.0, 0.0, 0.0);
#endif
}
