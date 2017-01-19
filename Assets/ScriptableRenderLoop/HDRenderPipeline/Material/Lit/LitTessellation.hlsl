float4 TessellationEdge(float3 p0, float3 p1, float3 p2, float3 n0, float3 n1, float3 n2)
{
    float maxDisplacement = ADD_ZERO_IDX(_HeightAmplitude);
#ifdef _LAYER_COUNT
    maxDisplacement = max(maxDisplacement, _HeightAmplitude1);
    #if _LAYER_COUNT >= 3
    maxDisplacement = max(maxDisplacement, _HeightAmplitude2);
    #endif
    #if _LAYER_COUNT >= 4
    maxDisplacement = max(maxDisplacement, _HeightAmplitude3);
#endif
#endif

    bool frustumCulled = WorldViewFrustumCull(p0, p1, p2, maxDisplacement, (float4[4])unity_CameraWorldClipPlanes);

    bool faceCull = false;
#if !(defined(_DOUBLESIDED) || defined(_DOUBLESIDED_LIGHTING_FLIP) || defined(_DOUBLESIDED_LIGHTING_MIRROR))
    // TODO: Handle inverse culling (for mirror)!
    if (_TessellationBackFaceCullEpsilon > -0.99) // Is backface culling enabled ?
    {
        faceCull = BackFaceCullTriangle(p0, p1, p2, _TessellationBackFaceCullEpsilon, _WorldSpaceCameraPos);
    }
#endif

    if (frustumCulled || faceCull)
    {
        // Settings factor to 0 will kill the triangle
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    float3 tessFactor = float3(1.0, 1.0, 1.0);

    // Aaptive screen space tessellation
    if (_TessellationFactorTriangleSize > 0.0)
    {
        // return a value between 0 and 1
        tessFactor *= GetScreenSpaceTessFactor( p0, p1, p2, GetWorldToHClipMatrix(), _ScreenParams,  _TessellationFactorTriangleSize);
    }

    // Distance based tessellation
    if (_TessellationFactorMaxDistance > 0.0)
    {
        float3 distFactor = GetDistanceBasedTessFactor(p0, p1, p2, _WorldSpaceCameraPos, _TessellationFactorMinDistance, _TessellationFactorMaxDistance);
        // We square the disance factor as it allow a better percptual descrease of vertex density.
        tessFactor *= distFactor * distFactor;
    }

    tessFactor *= _TessellationFactor;

    // TessFactor below 1.0 have no effect. At 0 it kill the triangle, so clamp it to 1.0
    tessFactor.xyz = float3(max(1.0, tessFactor.x), max(1.0, tessFactor.y), max(1.0, tessFactor.z));

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
