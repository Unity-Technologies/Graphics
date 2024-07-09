// Setup function used by no one
void SetupInstanceID() {}

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
StructuredBuffer<float2> _WaterPatchData;
#endif

float3 WaterSimulationPosition(float3 objectPosition, uint instanceID = 0)
{
    // This branch is useless but it improves occupancy
    if (_GridSize.x < 0)
        return 0;

    float3 simulationPos = objectPosition;

    float2 gridSize = _GridSize;
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    // Grab the patch data for the current instance/patch
    float2 patchData = _WaterPatchData[instanceID];
    simulationPos.x = objectPosition.x * patchData.x - objectPosition.z * patchData.y;
    simulationPos.z = objectPosition.x * patchData.y + objectPosition.z * patchData.x;
    #elif !defined(WATER_DISPLACEMENT)
    gridSize *= _GridSizeMultiplier;
    #endif

    // Scale and offset the position to where it should be
    simulationPos.xz = simulationPos.xz * gridSize + _PatchOffset;

    #ifndef WATER_DISPLACEMENT
    // Clamp the mesh inside the region so that it's never empty
    simulationPos.xz = max(simulationPos.xz, -_RegionExtent);
    simulationPos.xz = min(simulationPos.xz,  _RegionExtent);
    #endif

    // Return the simulation position
    return simulationPos;
}

/// VERTEX STAGE START

#ifdef TESSELLATION_ON
#define VaryingsType VaryingsToDS
#define VaryingsMeshType VaryingsMeshToDS
#define PackedVaryingsType PackedVaryingsToDS
#define PackVaryingsType PackVaryingsToDS

struct PackedVaryingsToDS
{
    PackedVaryingsMeshToDS vmesh;
};

struct VaryingsToDS
{
    VaryingsMeshToDS vmesh;
};

PackedVaryingsToDS PackVaryingsToDS(VaryingsToDS input)
{
    PackedVaryingsToDS output;
    output.vmesh = PackVaryingsMeshToDS(input.vmesh);
    return output;
}

#else
#define VaryingsType VaryingsToPS
#define VaryingsMeshType VaryingsMeshToPS
#define PackedVaryingsType PackedVaryingsToPS
#define PackVaryingsType PackVaryingsToPS
#endif

VaryingsMeshType VertMeshWater(AttributesMesh input)
{
    VaryingsMeshType output;
    ZERO_INITIALIZE(VaryingsMeshType, output); // Only required with custom interpolator to quiet the shader compiler about not fully initialized struct

    // Deduce the actual instance ID of the current instance (it is then stored in unity_InstanceID)
    UNITY_SETUP_INSTANCE_ID(input);
    // Transfer the unprocessed instance ID to the next stage
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    // Scale the position by the size of the grid to get the position that will be used for sampling the simulation
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    // WARNING: Here we can only use unity_InstanceID and not GET_UNITY_INSTANCE_ID because the later function
    // does not return the expect value when we have both procedural and stereo instancing.
    // This variables is guaranted to be defined and set to the right value as soon as we have at least
    // one instancing technique.
    input.positionOS = WaterSimulationPosition(input.positionOS, unity_InstanceID);
#else
    input.positionOS = WaterSimulationPosition(input.positionOS);
    #if defined(WATER_DISPLACEMENT)
    // In case we are using custom geometries, we need to apply the tranform of each custom geometry to ensure
    // that they are correctly connected
    input.positionOS = mul(_WaterCustomMeshTransform, float4(input.positionOS, 1.0f)).xyz;
    input.normalOS = SafeNormalize(mul(input.normalOS, (float3x3)_WaterCustomMeshTransform_Inverse));
    #endif
#endif

    float3 positionPredisplacementOS = input.positionOS;

    VertexDescription vertex;
    ApplyMeshModification(input, _TimeParameters.xyz, output, vertex);

    // Export for the following stage
    PackWaterVertexData(vertex, output.texCoord0, output.texCoord1);

    output.normalWS = vertex.Normal;

    #ifdef TESSELLATION_ON
    output.tessellationFactor = _WaterMaxTessellationFactor;
    output.positionRWS = TransformObjectToWorld(vertex.Position + vertex.Displacement);
    #else
    output.positionCS = TransformWorldToHClip(output.texCoord1.xyz);
    #endif

    #if defined(WATER_DISPLACEMENT)
    // discard vertices outside of the region for non infinite surface
    // 0.1 offset is to account for precision issue. Should be dependent on the grid size but this also works
    if (any(abs(positionPredisplacementOS.xz) > _RegionExtent + 0.1f))
    {
        #ifdef TESSELLATION_ON
        output.tessellationFactor = -1;
        #else
        output.positionCS.w = FLT_NAN;
        #endif
    }
    #endif

    return output;
}

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMeshWater(inputMesh);
    return PackVaryingsType(varyingsType);
}

// VERTEX STAGE END

#ifdef TESSELLATION_ON
// TESSELATION STAGE START

VaryingsToDS UnpackVaryingsToDS(PackedVaryingsToDS input)
{
    VaryingsToDS output;
    output.vmesh = UnpackVaryingsMeshToDS(input.vmesh);
    return output;
}

VaryingsToDS InterpolateWithBaryCoordsToDS(VaryingsToDS input0, VaryingsToDS input1, VaryingsToDS input2, float3 baryCoords)
{
    VaryingsToDS output;
    output.vmesh = InterpolateWithBaryCoordsMeshToDS(input0.vmesh, input1.vmesh, input2.vmesh, baryCoords);
    return output;
}

VaryingsMeshToPS VertMeshTesselation(VaryingsMeshToDS input)
{
    VaryingsMeshToPS output;
    ZERO_INITIALIZE(VaryingsMeshToPS, output);

    // Deduce the actual instance ID of the current instance (it is then stored in unity_InstanceID)
    UNITY_SETUP_INSTANCE_ID(input);
    // Transfer the unprocessed instance ID to the next stage
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    VertexDescription vertex;
    ApplyTessellationModification(input, _TimeParameters.xyz, output, vertex);

    // Export for the following stage
    PackWaterVertexData(vertex, output.texCoord0, output.texCoord1);

    output.positionCS = TransformWorldToHClip(output.texCoord1.xyz);
    output.normalWS = vertex.Normal;


    return output;
}

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;
    output.vmesh = VertMeshTesselation(input.vmesh);
    return PackVaryingsToPS(output);
}

#if defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL)
// AMD recommand this value for GCN http://amd-dev.wpengine.netdna-cdn.com/wordpress/media/2013/05/GCNPerformanceTweets.pdf
#define MAX_TESSELLATION_FACTORS 15.0
#else
#define MAX_TESSELLATION_FACTORS 64.0
#endif

float4 GetTessellationFactors(float3 p0, float3 p1, float3 p2, float3 n0, float3 n1, float3 n2, float3 inputTessellationFactors)
{
    // For tessellation we want to process tessellation factor always from the point of view of the camera (to be consistent and avoid Z-fight).
    // For the culling part however we want to use the current view (shadow view).
    // Thus the following code play with both.
    float frustumEps = -_MaxWaveDisplacement; // "-" Expected parameter for CullTriangleEdgesFrustum

    // TODO: the only reason I test the near plane here is that I am not sure that the product of other tessellation factors
    // (such as screen-space/distance-based) results in the tessellation factor of 1 for the geometry behind the near plane.
    // If that is the case (and, IMHO, it should be), we shouldn't have to test the near plane here.
    bool4 frustumCullEdgesMainView = CullFullTriangleAndEdgesFrustum(p0, p1, p2, frustumEps, _FrustumPlanes, 5); // Do not test the far plane
    if (frustumCullEdgesMainView.w)
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

    // Distance based tessellation
    float3 distFactor = GetDistanceBasedTessFactor(p0, p1, p2, GetPrimaryCameraPosition(), _WaterTessellationFadeStart, _WaterTessellationFadeStart + _WaterTessellationFadeRange);  // Use primary camera view
    edgeTessFactors *= distFactor * distFactor;

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

    // Discard vertices outside of region
    if (varying0.vmesh.tessellationFactor < 0 ||
        varying1.vmesh.tessellationFactor < 0 ||
        varying2.vmesh.tessellationFactor < 0)
    {
        PackedVaryingsToPS output;
        ZERO_INITIALIZE(PackedVaryingsToPS, output);
        output.vmesh.positionCS.w = FLT_NAN;
        return output;
    }

    return VertTesselation(varying);
}

// TESSELATION STAGE END
#endif
