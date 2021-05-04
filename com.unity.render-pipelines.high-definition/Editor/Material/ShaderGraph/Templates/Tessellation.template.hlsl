
// TODO => must be fully generated!
VaryingsMeshToDS InterpolateWithBaryCoordsMeshToDS(VaryingsMeshToDS input0, VaryingsMeshToDS input1, VaryingsMeshToDS input2, float3 baryCoords)
{
    VaryingsMeshToDS output;

    UNITY_TRANSFER_INSTANCE_ID(input0, output);

    TESSELLATION_INTERPOLATE_BARY(positionRWS, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(normalWS, baryCoords);
#ifdef VARYINGS_DS_NEED_TANGENT
    // This will interpolate the sign but should be ok in practice as we may expect a triangle to have same sign (? TO CHECK)
    TESSELLATION_INTERPOLATE_BARY(tangentWS, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD0
    TESSELLATION_INTERPOLATE_BARY(texCoord0, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    TESSELLATION_INTERPOLATE_BARY(texCoord1, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2
    TESSELLATION_INTERPOLATE_BARY(texCoord2, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3
    TESSELLATION_INTERPOLATE_BARY(texCoord3, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_COLOR
    TESSELLATION_INTERPOLATE_BARY(color, baryCoords);
#endif

    return output;
}

float4 GetTessellationFactors(float3 p0, float3 p1, float3 p2, float3 n0, float3 n1, float3 n2)
{
    float maxDisplacement = 0.1; // GetMaxDisplacement();

    // For tessellation we want to process tessellation factor always from the point of view of the camera (to be consistent and avoid Z-fight).
    // For the culling part however we want to use the current view (shadow view).
    // Thus the following code play with both.
    float frustumEps = -maxDisplacement; // "-" Expected parameter for CullTriangleEdgesFrustum

#ifndef SCENEPICKINGPASS
                                         // TODO: the only reason I test the near plane here is that I am not sure that the product of other tessellation factors
                                         // (such as screen-space/distance-based) results in the tessellation factor of 1 for the geometry behind the near plane.
                                         // If that is the case (and, IMHO, it should be), we shouldn't have to test the near plane here.
    bool4 frustumCullEdgesMainView = CullFullTriangleAndEdgesFrustum(p0, p1, p2, frustumEps, _FrustumPlanes, 5); // Do not test the far plane
#else
                                         // During the scene picking pass, we have no access to camera frustum planes
    bool4 frustumCullEdgesMainView = false;
#endif

#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_SHADOWS)
    bool frustumCullCurrView = frustumCullEdgesMainView.w;
#else
    bool frustumCullCurrView = CullTriangleFrustum(p0, p1, p2, frustumEps, _ShadowFrustumPlanes, 4); // Do not test near/far planes
#endif

    bool faceCull = false;

#if !defined(_DOUBLESIDED_ON) && !defined(SCENESELECTIONPASS) && !defined(SCENEPICKINGPASS)
    if (_TessellationBackFaceCullEpsilon > -1.0) // Is back-face culling enabled ?
    {
        // Handle transform mirroring (like negative scaling)
        // Note: We don't need to handle handness of view matrix here as the backface is perform in worldspace
        // note2: When we have an orthogonal matrix (cascade shadow map), we need to use the direction of the light.
        // Otherwise we use only p0 instead of the mean of P0, p1,p2 to save ALU as with tessellated geomerty it is rarely needed and user can still control _TessellationBackFaceCullEpsilon.
        float winding = unity_WorldTransformParams.w;
        faceCull = CullTriangleBackFaceView(p0, p1, p2, _TessellationBackFaceCullEpsilon, GetWorldSpaceNormalizeViewDir(p0), winding); // Use shadow view
    }
#endif

    if (frustumCullCurrView || faceCull)
    {
        // Settings factor to 0 will kill the triangle
        return 0;
    }

    // For performance reasons, we choose not to tessellate outside of the main camera view
    // (we perform this test both during the regular scene rendering and the shadow pass).
    // For edges not visible from the main view, our goal is to set the tessellation factor to 1.
    // In this case, we set the tessellation factor to 0 here.
    // That way, all scaling of this tessellation factor will still result in 0.
    // Before we call CalcTriTessFactorsFromEdgeTessFactors(), all factors are clamped by max(f, 1),
    // which achieves the desired effect.
    float3 edgeTessFactors = float3(frustumCullEdgesMainView.x ? 0 : 1, frustumCullEdgesMainView.y ? 0 : 1, frustumCullEdgesMainView.z ? 0 : 1);

    // Adaptive screen space tessellation
    if (_TessellationFactorTriangleSize > 0.0)
    {
        // return a value between 0 and 1
        // Warning: '_ViewProjMatrix' can be the viewproj matrix of the light when we render shadows, that's why we use _CameraViewProjMatrix instead
        edgeTessFactors *= GetScreenSpaceTessFactor(p0, p1, p2, _CameraViewProjMatrix, _ScreenSize, _TessellationFactorTriangleSize); // Use primary camera view
    }

    // Distance based tessellation
    if (_TessellationFactorMaxDistance > 0.0)
    {
        float3 distFactor = GetDistanceBasedTessFactor(p0, p1, p2, GetPrimaryCameraPosition(), _TessellationFactorMinDistance, _TessellationFactorMaxDistance);  // Use primary camera view
                                                                                                                                                                 // We square the disance factor as it allow a better percptual descrease of vertex density.
        edgeTessFactors *= distFactor * distFactor;
    }

    // TODO: This shouldn't be in SurfaceDescription but in TessellationDescription
    float tessellationFactor = 1.0;
    $vertexDescription.TessellationFactor: tessellationFactor = vertexDescription.TessellationFactor;

    edgeTessFactors *= tessellationFactor * _GlobalTessellationFactorMultiplier;

    // TessFactor below 1.0 have no effect. At 0 it kill the triangle, so clamp it to 1.0
    edgeTessFactors = max(edgeTessFactors, float3(1.0, 1.0, 1.0));

    return CalcTriTessFactorsFromEdgeTessFactors(edgeTessFactors);
}

#ifdef HAVE_TESSELLATION_MODIFICATION
// tessellationFactors
// x - 1->2 edge
// y - 2->0 edge
// z - 0->1 edge
// w - inside tessellation factor
void ApplyTessellationModification(VaryingsMeshToDS input, float3 normalWS, inout float3 positionRWS)
{
    // TODO: This shouldn't be in SurfaceDescription but in TessellationDescription
    $vertexDescription.TessellationDisplacement: float3 displacementWS = vertexDescription.TessellationDisplacement;

    return ;
}

#endif
