#if defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL)
// AMD recommand this value for GCN http://amd-dev.wpengine.netdna-cdn.com/wordpress/media/2013/05/GCNPerformanceTweets.pdf
#define MAX_TESSELLATION_FACTORS 15.0
#else
#define MAX_TESSELLATION_FACTORS 64.0
#endif

float4 GetTessellationFactors(float3 p0, float3 p1, float3 p2, float3 n0, float3 n1, float3 n2, float3 inputTessellationFactors)
{
    float maxDisplacement = GetMaxDisplacement();

    // For tessellation we want to process tessellation factor always from the point of view of the camera (to be consistent and avoid Z-fight).
    // For the culling part however we want to use the current view (shadow view).
    // Thus the following code play with both.
    float frustumEps = -maxDisplacement; // "-" Expected parameter for CullTriangleEdgesFrustum

#if !defined(SCENESELECTIONPASS) && !defined(SCENEPICKINGPASS)
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

    edgeTessFactors *= inputTessellationFactors * _GlobalTessellationFactorMultiplier;

    // TessFactor below 1.0 have no effect. At 0 it kill the triangle, so clamp it to 1.0
    edgeTessFactors = max(edgeTessFactors, float3(1.0, 1.0, 1.0));

    return CalcTriTessFactorsFromEdgeTessFactors(edgeTessFactors);
}

struct TessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

TessellationFactors HullConstant(InputPatch<PackedVaryingsToDS, 3> input)
{
    VaryingsToDS varying0 = UnpackVaryingsToDS(input[0]);
    VaryingsToDS varying1 = UnpackVaryingsToDS(input[1]);
    VaryingsToDS varying2 = UnpackVaryingsToDS(input[2]);

    float3 p0 = varying0.vmesh.positionRWS;
    float3 p1 = varying1.vmesh.positionRWS;
    float3 p2 = varying2.vmesh.positionRWS;

    float3 n0 = varying0.vmesh.normalWS;
    float3 n1 = varying1.vmesh.normalWS;
    float3 n2 = varying2.vmesh.normalWS;

    // x - 1->2 edge
    // y - 2->0 edge
    // z - 0->1 edge
    // w - inside tessellation factor (calculate as mean of three in GetTessellationFactors())
    float3 inputTessellationFactors;
    // TessellatinFactor is evaluate in vertex shader
    inputTessellationFactors.x = 0.5 * (varying1.vmesh.tessellationFactor + varying2.vmesh.tessellationFactor);
    inputTessellationFactors.y = 0.5 * (varying2.vmesh.tessellationFactor + varying0.vmesh.tessellationFactor);
    inputTessellationFactors.z = 0.5 * (varying0.vmesh.tessellationFactor + varying1.vmesh.tessellationFactor);

    float4 tf = GetTessellationFactors(p0, p1, p2, n0, n1, n2, inputTessellationFactors);

    TessellationFactors output;
    output.edge[0] = min(tf.x, MAX_TESSELLATION_FACTORS);
    output.edge[1] = min(tf.y, MAX_TESSELLATION_FACTORS);
    output.edge[2] = min(tf.z, MAX_TESSELLATION_FACTORS);
    output.inside  = min(tf.w, MAX_TESSELLATION_FACTORS);

    return output;
}

// ref: http://reedbeta.com/blog/tess-quick-ref/
[maxtessfactor(MAX_TESSELLATION_FACTORS)]
[domain("tri")]
[partitioning("fractional_odd")]
[outputtopology("triangle_cw")]
[patchconstantfunc("HullConstant")]
[outputcontrolpoints(3)]
PackedVaryingsToDS Hull(InputPatch<PackedVaryingsToDS, 3> input, uint id : SV_OutputControlPointID)
{
    // Pass-through
    return input[id];
}

[domain("tri")]
PackedVaryingsToPS Domain(TessellationFactors tessFactors, const OutputPatch<PackedVaryingsToDS, 3> input, float3 baryCoords : SV_DomainLocation)
{
    VaryingsToDS varying0 = UnpackVaryingsToDS(input[0]);
    VaryingsToDS varying1 = UnpackVaryingsToDS(input[1]);
    VaryingsToDS varying2 = UnpackVaryingsToDS(input[2]);

    VaryingsToDS varying = InterpolateWithBaryCoordsToDS(varying0, varying1, varying2, baryCoords);

    // We have Phong tessellation in all case where we don't have displacement only
#ifdef _TESSELLATION_PHONG

    float3 p0 = varying0.vmesh.positionRWS;
    float3 p1 = varying1.vmesh.positionRWS;
    float3 p2 = varying2.vmesh.positionRWS;

    float3 n0 = varying0.vmesh.normalWS;
    float3 n1 = varying1.vmesh.normalWS;
    float3 n2 = varying2.vmesh.normalWS;

    varying.vmesh.positionRWS = PhongTessellation(  varying.vmesh.positionRWS,
                                                    p0, p1, p2, n0, n1, n2,
                                                    baryCoords, _TessellationShapeFactor);
#endif
#ifdef VARYINGS_DS_NEED_POSITIONPREDISPLACEMENT
    varying.vmesh.positionPredisplacementRWS = varying.vmesh.positionRWS;
#endif

#ifdef HAVE_TESSELLATION_MODIFICATION
    varying.vmesh = ApplyTessellationModification(varying.vmesh, _TimeParameters.xyz);
#endif

    return VertTesselation(varying);
}
