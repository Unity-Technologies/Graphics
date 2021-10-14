#if (SHADERPASS != SHADERPASS_DEPTH_ONLY && SHADERPASS != SHADERPASS_SHADOWS && SHADERPASS != SHADERPASS_TRANSPARENT_DEPTH_PREPASS && SHADERPASS != SHADERPASS_TRANSPARENT_DEPTH_POSTPASS)
#error SHADERPASS_is_not_correctly_define
#endif

#if !defined(DOTS_INSTANCING_ON) || !defined(SCENEPICKINGPASS)

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
#if defined(WRITE_DECAL_BUFFER) && !defined(_DISABLE_DECALS)
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalPrepassBuffer.hlsl"
#endif

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;

#if (SHADERPASS == SHADERPASS_DEPTH_ONLY) && defined(HAVE_RECURSIVE_RENDERING) && !defined(SCENESELECTIONPASS) && !defined(SCENEPICKINGPASS)
    // If we have a recursive raytrace object, we will not render it.
    // As we don't want to rely on renderqueue to exclude the object from the list,
    // we cull it by settings position to NaN value.
    // TODO: provide a solution to filter dyanmically recursive raytrace object in the DrawRenderer
    if (_EnableRecursiveRayTracing && _RayTracing > 0.0)
    {
        ZERO_INITIALIZE(VaryingsType, varyingsType); // Divide by 0 should produce a NaN and thus cull the primitive.
    }
    else
#endif
    {
        varyingsType.vmesh = VertMesh(inputMesh);
    }

    return PackVaryingsType(varyingsType);
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;
    output.vmesh = VertMeshTesselation(input.vmesh);
    return PackVaryingsToPS(output);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"

#endif // TESSELLATION_ON

#if defined(WRITE_NORMAL_BUFFER) && defined(WRITE_MSAA_DEPTH)
#define SV_TARGET_DECAL SV_Target2
#elif defined(WRITE_NORMAL_BUFFER) || defined(WRITE_MSAA_DEPTH)
#define SV_TARGET_DECAL SV_Target1
#else
#define SV_TARGET_DECAL SV_Target0
#endif

void Frag(  PackedVaryingsToPS packedInput
            #if defined(SCENESELECTIONPASS) || defined(SCENEPICKINGPASS)
            , out float4 outColor : SV_Target0
            #else
                #ifdef WRITE_MSAA_DEPTH
                // We need the depth color as SV_Target0 for alpha to coverage
                , out float4 depthColor : SV_Target0
                    #ifdef WRITE_NORMAL_BUFFER
                    , out float4 outNormalBuffer : SV_Target1
                    #endif
                #else
                    #ifdef WRITE_NORMAL_BUFFER
                    , out float4 outNormalBuffer : SV_Target0
                    #endif
                #endif

                // Decal buffer must be last as it is bind but we can optionally write into it (based on _DISABLE_DECALS)
                #if defined(WRITE_DECAL_BUFFER) && !defined(_DISABLE_DECALS)
                , out float4 outDecalBuffer : SV_TARGET_DECAL
                #endif
            #endif

            #if defined(_DEPTHOFFSET_ON) && !defined(SCENEPICKINGPASS)
            , out float outputDepth : DEPTH_OFFSET_SEMANTIC
            #endif
        )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

#if defined(_DEPTHOFFSET_ON) && !defined(SCENEPICKINGPASS)
    outputDepth = posInput.deviceDepth;


#if SHADERPASS == SHADERPASS_SHADOWS
    // If we are using the depth offset and manually outputting depth, the slope-scale depth bias is not properly applied
    // we need to manually apply.
    float bias = max(abs(ddx(posInput.deviceDepth)), abs(ddy(posInput.deviceDepth))) * _SlopeScaleDepthBias;
    outputDepth += bias;
#endif

#endif

#ifdef SCENESELECTIONPASS
    // We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
    outColor = float4(_ObjectId, _PassValue, 1.0, 1.0);
#elif defined(SCENEPICKINGPASS)
    outColor = _SelectionID;
#else

    // Depth and Alpha to coverage
    #ifdef WRITE_MSAA_DEPTH
        // In case we are rendering in MSAA, reading the an MSAA depth buffer is way too expensive. To avoid that, we export the depth to a color buffer
        depthColor = packedInput.vmesh.positionCS.z;

        #ifdef _ALPHATOMASK_ON
        // Alpha channel is used for alpha to coverage
        depthColor.a = SharpenAlpha(builtinData.opacity, builtinData.alphaClipTreshold);
        #endif
    #endif

    #if defined(WRITE_NORMAL_BUFFER)
    EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), outNormalBuffer);
    #endif

    #if defined(WRITE_DECAL_BUFFER) && !defined(_DISABLE_DECALS)
    DecalPrepassData decalPrepassData;
    // We don't have the right to access SurfaceData in a shaderpass.
    // However it would be painful to have to add a function like ConvertSurfaceDataToDecalPrepassData() to every Material to return geomNormalWS anyway
    // Here we will put the constrain that any Material requiring to support Decal, will need to have geomNormalWS as member of surfaceData (and we already require normalWS anyway)
    decalPrepassData.geomNormalWS = surfaceData.geomNormalWS;
    decalPrepassData.decalLayerMask = GetMeshRenderingDecalLayer();
    EncodeIntoDecalPrepassBuffer(decalPrepassData, outDecalBuffer);
    #endif

#endif // SCENESELECTIONPASS
}

#else
// ========================================================================================================================
// DOTS PICKING
// ========================================================================================================================

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

struct PickingAttributesMesh
{
    float3 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct PickingMeshToDS
{
    float3 positionRWS : INTERNALTESSPOS;
    float3 normalWS : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct PickingMeshToPS
{
    float4 positionCS : SV_Position;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

#ifdef TESSELLATION_ON
#define PickingVertexOutput PickingMeshToDS
#else
#define PickingVertexOutput PickingMeshToPS
#endif

float4x4 _DOTSPickingViewMatrix;
float4x4 _DOTSPickingProjMatrix;
float4 _DOTSPickingCameraWorldPos;

#undef unity_ObjectToWorld
float4x4 LoadObjectToWorldMatrixDOTSPicking()
{
    float4x4 objectToWorld = LoadDOTSInstancedData_float4x4_from_float3x4(UNITY_DOTS_INSTANCED_METADATA_NAME(float3x4, unity_ObjectToWorld));
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    objectToWorld._m03_m13_m23 -= _DOTSPickingCameraWorldPos.xyz;
 #endif
    return objectToWorld;
}

#undef unity_WorldToObject
float4x4 LoadWorldToObjectMatrixDOTSPicking()
{
    float4x4 worldToObject = LoadDOTSInstancedData_float4x4_from_float3x4(UNITY_DOTS_INSTANCED_METADATA_NAME(float3x4, unity_WorldToObject));

#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    // To handle camera relative rendering we need to apply translation before converting to object space
    float4x4 translationMatrix =
    {
        { 1.0, 0.0, 0.0, _DOTSPickingCameraWorldPos.x },
        { 0.0, 1.0, 0.0, _DOTSPickingCameraWorldPos.y },
        { 0.0, 0.0, 1.0, _DOTSPickingCameraWorldPos.z },
        { 0.0, 0.0, 0.0, 1.0 }
    };
    return mul(worldToObject, translationMatrix);
#else
    return worldToObject;
#endif
}

PickingVertexOutput Vert(PickingAttributesMesh input)
{
    PickingVertexOutput output;
    ZERO_INITIALIZE(PickingVertexOutput, output);
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float4x4 objectToWorld = LoadObjectToWorldMatrixDOTSPicking();
    float4 positionWS = mul(objectToWorld, float4(input.positionOS, 1.0));

#ifdef TESSELLATION_ON
    float4x4 worldToObject = LoadWorldToObjectMatrixDOTSPicking();
    // Normal need to be multiply by inverse transpose
    float3 normalWS = SafeNormalize(mul(input.normalOS, (float3x3)worldToObject));

    output.positionRWS = positionWS;
    output.normalWS = normalWS;
#else
    output.positionCS = mul(_DOTSPickingProjMatrix, mul(_DOTSPickingViewMatrix, positionWS));
#endif

    return output;
}

#ifdef TESSELLATION_ON

// AMD recommand this value for GCN http://amd-dev.wpengine.netdna-cdn.com/wordpress/media/2013/05/GCNPerformanceTweets.pdf
#if defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL)
#define MAX_TESSELLATION_FACTORS 15.0
#else
#define MAX_TESSELLATION_FACTORS 64.0
#endif

struct PickingTessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

PickingTessellationFactors HullConstant(InputPatch<PickingMeshToDS, 3> input)
{
    float3 p0 = input[0].positionRWS;
    float3 p1 = input[1].positionRWS;
    float3 p2 = input[2].positionRWS;

    float3 n0 = input[0].normalWS;
    float3 n1 = input[1].normalWS;
    float3 n2 = input[2].normalWS;

    // ref: http://reedbeta.com/blog/tess-quick-ref/
    // x - 1->2 edge
    // y - 2->0 edge
    // z - 0->1 edge
    // w - inside tessellation factor
    float4 tf = GetTessellationFactors(p0, p1, p2, n0, n1, n2);
    PickingTessellationFactors output;
    output.edge[0] = min(tf.x, MAX_TESSELLATION_FACTORS);
    output.edge[1] = min(tf.y, MAX_TESSELLATION_FACTORS);
    output.edge[2] = min(tf.z, MAX_TESSELLATION_FACTORS);
    output.inside  = min(tf.w, MAX_TESSELLATION_FACTORS);

    return output;
}

[maxtessfactor(MAX_TESSELLATION_FACTORS)]
[domain("tri")]
[partitioning("fractional_odd")]
[outputtopology("triangle_cw")]
[patchconstantfunc("HullConstant")]
[outputcontrolpoints(3)]
PickingMeshToDS Hull(InputPatch<PickingMeshToDS, 3> input, uint id : SV_OutputControlPointID)
{
    // Pass-through
    return input[id];
}

PickingMeshToDS PickingInterpolateWithBaryCoordsMeshToDS(PickingMeshToDS input0, PickingMeshToDS input1, PickingMeshToDS input2, float3 baryCoords)
{
    PickingMeshToDS output;
    UNITY_TRANSFER_INSTANCE_ID(input0, output);

    output.positionRWS = input0.positionRWS * baryCoords.x +  input1.positionRWS * baryCoords.y +  input2.positionRWS * baryCoords.z;
    output.normalWS = input0.normalWS * baryCoords.x +  input1.normalWS * baryCoords.y +  input2.normalWS * baryCoords.z;

    return output;
}

PickingMeshToPS PickingVertMeshTesselation(PickingMeshToDS input)
{
    PickingMeshToPS output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionCS = mul(_DOTSPickingProjMatrix, mul(_DOTSPickingViewMatrix, float4(input.positionRWS, 1)));

    return output;
}

[domain("tri")]
PickingMeshToPS Domain(PickingTessellationFactors tessFactors, const OutputPatch<PickingMeshToDS, 3> input, float3 baryCoords : SV_DomainLocation)
{
    PickingMeshToDS input0 = input[0];
    PickingMeshToDS input1 = input[1];
    PickingMeshToDS input2 = input[2];

    PickingMeshToDS interpolated = PickingInterpolateWithBaryCoordsMeshToDS(input0, input1, input2, baryCoords);

    return PickingVertMeshTesselation(interpolated);
}

#endif // TESSELLATION_ON

void Frag(PickingMeshToPS input, out float4 outColor : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    UNITY_SETUP_INSTANCE_ID(input);

    outColor = _SelectionID;
}

#endif
