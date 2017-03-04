float4 GetTessellationFactors(float3 p0, float3 p1, float3 p2, float3 n0, float3 n1, float3 n2)
{
    float maxDisplacement = GetMaxDisplacement();

    bool frustumCulled = WorldViewFrustumCull(p0, p1, p2, maxDisplacement, (float4[4])unity_CameraWorldClipPlanes);

    bool faceCull = false;

    // We use the position of the primary (scene view) camera in order
    // to have identical tessellation levels for both the scene view and
    // shadow views. Otherwise, depth comparisons become meaningless!
    float3 camPosWS = _WorldSpaceCameraPos;

#ifndef _DOUBLESIDED_ON
    // TODO: Handle inverse culling (for mirror)!
    if (_TessellationBackFaceCullEpsilon > -0.99) // Is backface culling enabled ?
    {
        faceCull = BackFaceCullTriangle(p0, p1, p2, _TessellationBackFaceCullEpsilon, camPosWS);
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
        float3 distFactor = GetDistanceBasedTessFactor(p0, p1, p2, camPosWS, _TessellationFactorMinDistance, _TessellationFactorMaxDistance);
        // We square the disance factor as it allow a better percptual descrease of vertex density.
        tessFactor *= distFactor * distFactor;
    }

    tessFactor *= _TessellationFactor;

    // TessFactor below 1.0 have no effect. At 0 it kill the triangle, so clamp it to 1.0
    tessFactor.xyz = float3(max(1.0, tessFactor.x), max(1.0, tessFactor.y), max(1.0, tessFactor.z));

    return CalcTriEdgeTessFactors(tessFactor);
}

// tessellationFactors
// x - 1->2 edge
// y - 2->0 edge
// z - 0->1 edge
// w - inside tessellation factor
float3 GetTessellationDisplacement(VaryingsMeshToDS input)
{
    // This call will work for both LayeredLit and Lit shader
    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);
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
        input.normalWS,
        layerTexCoord);

    // http://www.sebastiansylvan.com/post/the-problem-with-tessellation-in-directx-11/
    float lod = 0.0;
    float4 vertexColor = float4(0.0, 0.0, 0.0, 0.0);
#ifdef VARYINGS_DS_NEED_COLOR
    vertexColor = input.color;
#endif
    float height = ComputePerVertexDisplacement(layerTexCoord, vertexColor, lod);
    float3 displ = height * input.normalWS;

        // Applying scaling of the object if requested
#ifdef _TESSELLATION_OBJECT_SCALE
    displ *= input.objectScale;
#endif

    return displ;
}
