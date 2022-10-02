#if SHADERPASS != SHADERPASS_MOTION_VECTORS
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/MotionVectorVertexShaderCommon.hlsl"
#if defined(WRITE_DECAL_BUFFER) && !defined(_DISABLE_DECALS)
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalPrepassBuffer.hlsl"
#endif

PackedVaryingsType Vert(AttributesMesh inputMesh,
                        AttributesPass inputPass)
{
    VaryingsType varyingsType;
#ifdef HAVE_VFX_MODIFICATION
    AttributesElement inputElement;
    varyingsType.vmesh = VertMesh(inputMesh, inputElement);
    return MotionVectorVS(varyingsType, inputMesh, inputPass, inputElement);
#else
    varyingsType.vmesh = VertMesh(inputMesh);
    return MotionVectorVS(varyingsType, inputMesh, inputPass);
#endif
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;
    output.vmesh = VertMeshTesselation(input.vmesh);
    return MotionVectorTessellation(output, input);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"

#endif // TESSELLATION_ON

#if defined(WRITE_DECAL_BUFFER) && defined(WRITE_MSAA_DEPTH)
#define SV_TARGET_NORMAL SV_Target3
#elif defined(WRITE_DECAL_BUFFER) || defined(WRITE_MSAA_DEPTH)
#define SV_TARGET_NORMAL SV_Target2
#else
#define SV_TARGET_NORMAL SV_Target1
#endif

// Caution: Motion vector pass is different from Depth prepass, it render normal buffer last instead of decal buffer last
// and thus, we force a write of 0 if _DISABLE_DECALS so we always write in the decal buffer.
// This is required as we can't make distinction  between deferred (write normal buffer) and forward (write normal buffer)
// in the context of the motion vector pass. The cost is acceptable as it is only do object with motion vector (usualy skin object)
// that most of the time use Forward Material (so are already writing motion vector data).
// So note that here unlike for depth prepass we don't check && !defined(_DISABLE_DECALS)
void Frag(  PackedVaryingsToPS packedInput
            #ifdef WRITE_MSAA_DEPTH
            // We need the depth color as SV_Target0 for alpha to coverage
            , out float4 depthColor : SV_Target0
            , out float4 outMotionVector : SV_Target1
                #ifdef WRITE_DECAL_BUFFER
                , out float4 outDecalBuffer : SV_Target2
                #endif
            #else
            // When no MSAA, the motion vector is always the first buffer
            , out float4 outMotionVector : SV_Target0
                #ifdef WRITE_DECAL_BUFFER
                , out float4 outDecalBuffer : SV_Target1
                #endif
            #endif

            // Decal buffer must be last as it is bind but we can optionally write into it (based on _DISABLE_DECALS)
            #ifdef WRITE_NORMAL_BUFFER
            , out float4 outNormalBuffer : SV_TARGET_NORMAL
            #endif

            #ifdef _DEPTHOFFSET_ON
            , out float outputDepth : DEPTH_OFFSET_SEMANTIC
            #endif
        )
{
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

    VaryingsPassToPS inputPass = UnpackVaryingsPassToPS(packedInput.vpass);
#ifdef _DEPTHOFFSET_ON
    inputPass.positionCS.w += builtinData.depthOffset;
    inputPass.previousPositionCS.w += builtinData.depthOffset;
#endif

    // TODO: How to allow overriden motion vector from GetSurfaceAndBuiltinData ?
    float2 motionVector = CalculateMotionVector(inputPass.positionCS, inputPass.previousPositionCS);

    // Convert from Clip space (-1..1) to NDC 0..1 space.
    // Note it doesn't mean we don't have negative value, we store negative or positive offset in NDC space.
    // Note: ((positionCS * 0.5 + 0.5) - (previousPositionCS * 0.5 + 0.5)) = (motionVector * 0.5)
    EncodeMotionVector(motionVector * 0.5, outMotionVector);

    // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
    bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;

    //Motion vector is enabled in SG but not active in VFX
#if defined(HAVE_VFX_MODIFICATION) && !VFX_FEATURE_MOTION_VECTORS
    forceNoMotion = true;
#endif

    // Setting the motionVector to a value more than 2 set as a flag for "force no motion". This is valid because, given that the velocities are in NDC,
    // a value of >1 can never happen naturally, unless explicitely set.
    if (forceNoMotion)
        outMotionVector = float4(2.0, 0.0, 0.0, 0.0);

// Depth and Alpha to coverage
#ifdef WRITE_MSAA_DEPTH
    // In case we are rendering in MSAA, reading the an MSAA depth buffer is way too expensive. To avoid that, we export the depth to a color buffer
    depthColor = packedInput.vmesh.positionCS.z;

    #ifdef _ALPHATOMASK_ON
    // Alpha channel is used for alpha to coverage
    depthColor.a = SharpenAlpha(builtinData.opacity, builtinData.alphaClipTreshold);
    #endif
#endif

// Normal Buffer Processing
#ifdef WRITE_NORMAL_BUFFER
    EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), outNormalBuffer);
#endif

#if defined(WRITE_DECAL_BUFFER)
    DecalPrepassData decalPrepassData;
    // Force a write in decal buffer even if decal is disab. This is a neutral value which have no impact for later pass
    #ifdef _DISABLE_DECALS
    ZERO_INITIALIZE(DecalPrepassData, decalPrepassData);
    #else
    // We don't have the right to access SurfaceData in a shaderpass.
    // However it would be painful to have to add a function like ConvertSurfaceDataToDecalPrepassData() to every Material to return geomNormalWS anyway
    // Here we will put the constrain that any Material requiring to support Decal, will need to have geomNormalWS as member of surfaceData (and we already require normalWS anyway)
    decalPrepassData.geomNormalWS = surfaceData.geomNormalWS;
    decalPrepassData.decalLayerMask = GetMeshRenderingDecalLayer();
    #endif
    EncodeIntoDecalPrepassBuffer(decalPrepassData, outDecalBuffer);

    // make sure we don't overwrite light layers
    outDecalBuffer.w = (GetMeshRenderingLightLayer() & 0x000000FF) / 255.0;
#endif

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.deviceDepth;
#endif
}
