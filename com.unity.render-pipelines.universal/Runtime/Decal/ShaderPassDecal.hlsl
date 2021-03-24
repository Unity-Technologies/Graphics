#if (SHADERPASS != SHADERPASS_DEPTHONLY) && (SHADERPASS != SHADERPASS_DBUFFER_PROJECTOR) && (SHADERPASS != SHADERPASS_DBUFFER_MESH) && (SHADERPASS != SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) && (SHADERPASS != SHADERPASS_FORWARD_EMISSIVE_MESH) && (SHADERPASS != SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) && (SHADERPASS != SHADERPASS_DECAL_SCREEN_SPACE_MESH) && (SHADERPASS != SHADERPASS_DECAL_GBUFFER_PROJECTOR) && (SHADERPASS != SHADERPASS_DECAL_GBUFFER_MESH)
#error SHADERPASS_is_not_correctly_define
#endif

#if !defined(SHADERPASS)
#error SHADERPASS_is_not_define
#endif

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR)
#define DECAL_PROJECTOR
#endif

#if (SHADERPASS == SHADERPASS_DBUFFER_MESH) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
#define DECAL_MESH
#endif

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
#define DECAL_DBUFFER
#endif

#if (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH)
#define DECAL_SCREEN_SPACE
#endif

#if (SHADERPASS == SHADERPASS_DECAL_GBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
#define DECAL_GBUFFER
#endif

#if (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_MESH)
#define DECAL_FORWARD_EMISSIVE
#endif

#if defined(DECAL_SCREEN_SPACE) || defined(DECAL_ANGLE_FADE)
#define DECAL_RECONSTRUCT_NORMAL

#if !defined(DECAL_SCREEN_SPACE)
#define _RECONSTRUCT_NORMAL_LOW
#endif
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#if defined(DECAL_PROJECTOR) || defined(DECAL_RECONSTRUCT_NORMAL)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl" // Load Scene Depth
#endif

#ifdef DECAL_PROJECTOR // TODO
#include "Packages/com.unity.render-pipelines.universal/Runtime/Decal/DecalPrepassBuffer.hlsl"
#endif
#ifdef DECAL_MESH
#include "Packages/com.unity.render-pipelines.universal/Runtime/Decal/DecalMeshBiasTypeEnum.cs.hlsl"
#endif
#ifdef DECAL_SCREEN_SPACE
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#endif
#ifdef DECAL_GBUFFER
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#endif
#ifdef DECAL_RECONSTRUCT_NORMAL
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/NormalReconstruction.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl" // VertMesh

void MeshDecalsPositionZBias(inout Varyings input)
{
#if UNITY_REVERSED_Z
    input.positionCS.z -= _DecalMeshDepthBias;
#else
    input.positionCS.z += _DecalMeshDepthBias;
#endif
}

PackedVaryings Vert(Attributes inputMesh)
{
    Varyings varyingsType = (Varyings)0;
#ifdef DECAL_MESH
    if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_VIEW_BIAS) // TODO: Check performance of branch
    {
        float3 viewDirectionOS = GetObjectSpaceNormalizeViewDir(inputMesh.positionOS);
        inputMesh.positionOS += viewDirectionOS * (_DecalMeshViewBias);
    }
    varyingsType = BuildVaryings(inputMesh);
#if (SHADERPASS == SHADERPASS_DECAL_SCREEN_SPACE_MESH) || (SHADERPASS == SHADERPASS_DECAL_GBUFFER_MESH)
    OUTPUT_LIGHTMAP_UV(inputMesh.uv1, unity_LightmapST, varyingsType.lightmapUV);
    OUTPUT_SH(varyingsType.normalWS, varyingsType.sh);
#endif
    if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_DEPTH_BIAS) // TODO: Check performance of branch
    {
        MeshDecalsPositionZBias(varyingsType);
    }
#else
    varyingsType = BuildVaryings(inputMesh);
#endif
    return PackVaryings(varyingsType);
}

void Frag(PackedVaryings packedInput,
#if defined(DECAL_DBUFFER)
    OUTPUT_DBUFFER(outDBuffer)
#elif defined(DECAL_SCREEN_SPACE)
    out half4 outColor : SV_Target0
#elif defined(DECAL_GBUFFER)
    out FragmentOutput fragmentOutput
#elif defined(DECAL_FORWARD_EMISSIVE)
    out half4 outEmissive : SV_Target0
#elif defined(SCENEPICKINGPASS)
    out float4 outColor : SV_Target0
#else
#error SHADERPASS_is_not_correctly_define
#endif
)
{
#ifdef SCENEPICKINGPASS
    outColor = _SelectionID;
#else
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    UNITY_SETUP_INSTANCE_ID(packedInput);
    Varyings input = UnpackVaryings(packedInput);

    DecalSurfaceData surfaceData;
    float angleFadeFactor = 1.0;

#ifdef DECAL_PROJECTOR
    float depth = LoadSceneDepth(input.positionCS.xy);
    PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

    // Transform from relative world space to decal space (DS) to clip the decal
    float3 positionDS = TransformWorldToObject(posInput.positionWS);
    positionDS = positionDS * float3(1.0, -1.0, 1.0);

    // call clip as early as possible
    float clipValue = 0.5 - Max3(abs(positionDS).x, abs(positionDS).y, abs(positionDS).z);
    clip(clipValue);

    float2 texCoord = positionDS.xz + float2(0.5, 0.5);
#ifdef VARYINGS_NEED_TEXCOORD0
    input.texCoord0.xy = texCoord;
#endif

#ifdef DECAL_RECONSTRUCT_NORMAL
    float linearDepth = RawToLinearDepth(depth);
    float3 vpos = ReconstructViewPos(posInput.positionNDC, linearDepth);
    float3 normalWS = ReconstructNormal(posInput.positionNDC, linearDepth, vpos);
#endif

#ifdef DECAL_ANGLE_FADE
    // Check if this decal projector require angle fading
    float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
    float2 angleFade = float2(normalToWorld[1][3], normalToWorld[2][3]);

    if (angleFade.y < 0.0f) // if angle fade is enabled
    {
        float3 decalNormal = float3(normalToWorld[0].z, normalToWorld[1].z, normalToWorld[2].z);
        float dotAngle = dot(normalWS, decalNormal);
        // See equation in DecalSystem.cs - simplified to a madd mul add here
        angleFadeFactor = saturate(angleFade.x + angleFade.y * (dotAngle * (dotAngle - 2.0)));
    }
#endif


#else // Decal mesh
    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, input.positionCS.z, input.positionCS.w, input.positionWS.xyz, uint2(0, 0));

#ifdef DECAL_RECONSTRUCT_NORMAL
    float depth = LoadSceneDepth(input.positionCS.xy);

    float linearDepth = RawToLinearDepth(depth);
    float3 vpos = ReconstructViewPos(posInput.positionNDC, linearDepth);
    float3 normalWS = ReconstructNormal(posInput.positionNDC, linearDepth, vpos);
#endif

#endif

#ifdef VARYINGS_NEED_VIEWDIRECTION_WS
    float3 viewDirectionWS = input.viewDirectionWS;
#else
    // Unused
    float3 viewDirectionWS = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    GetSurfaceData(input, viewDirectionWS, posInput.positionSS, angleFadeFactor, surfaceData);

#if defined(DECAL_DBUFFER)
    ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
#elif defined(DECAL_SCREEN_SPACE)

    InputData inputData = (InputData)0;
    inputData.positionWS = posInput.positionWS;

#if defined(DECALS_NORMAL_BLEND_LOW) || defined(DECALS_NORMAL_BLEND_MEDIUM) || defined(DECALS_NORMAL_BLEND_HIGH)
    float normalAlpha = 1.0 - surfaceData.normalWS.w;
    float3 decalNormal = surfaceData.normalWS.xyz * surfaceData.normalWS.w;// ((surfaceData.normalWS.xyz * 0.5 + 0.5) * surfaceData.normalWS.w) * 2 - (254.0 / 255.0); // TODO
    inputData.normalWS.xyz = half3(
        normalize(normalWS.xyz *
            normalAlpha +
            decalNormal));
#else
    inputData.normalWS.xyz = half3(surfaceData.normalWS.xyz);
#endif

    inputData.viewDirectionWS = viewDirectionWS;

#if defined(VARYINGS_NEED_SHADOW_COORD) && defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

    inputData.fogCoord = input.fogFactorAndVertexLight.x;
#ifdef DECAL_MESH
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.sh, inputData.normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
#endif

    SurfaceData surface = (SurfaceData)0;
    surface.albedo = surfaceData.baseColor.rgb;
    surface.metallic = saturate(surfaceData.mask.x);
    surface.specular = 0;
    surface.smoothness = saturate(surfaceData.mask.z);
    surface.occlusion = surfaceData.mask.y;
    surface.emission = surfaceData.emissive;
    surface.alpha = saturate(surfaceData.baseColor.w);
    surface.clearCoatMask = 0;
    surface.clearCoatSmoothness = 1;

    half4 color = UniversalFragmentPBR(inputData, surface);

    color.rgb = MixFog(color.rgb, inputData.fogCoord);

    outColor = color;
#elif defined(DECAL_GBUFFER)

    InputData inputData = (InputData)0;
    inputData.normalWS.xyz = half3(surfaceData.normalWS.xyz);
    inputData.viewDirectionWS = viewDirectionWS;

#if defined(VARYINGS_NEED_SHADOW_COORD) && defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

#ifdef DECAL_MESH
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.sh, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
#endif

    SurfaceData surface = (SurfaceData)0;
    surface.albedo = surfaceData.baseColor.rgb;
    surface.metallic = saturate(surfaceData.mask.x);
    surface.specular = 0;
    surface.smoothness = saturate(surfaceData.mask.z);
    surface.occlusion = surfaceData.mask.y;
    surface.emission = surfaceData.emissive;
    surface.alpha = saturate(surfaceData.baseColor.w);
    surface.clearCoatMask = 0;
    surface.clearCoatSmoothness = 1;

    // in LitForwardPass GlobalIllumination (and temporarily LightingPhysicallyBased) are called inside UniversalFragmentPBR
    // in Deferred rendering we store the sum of these values (and of emission as well) in the GBuffer
    BRDFData brdfData;
    InitializeBRDFData(surface.albedo, surface.metallic, 0, surface.smoothness, surface.alpha, brdfData);

    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, inputData.shadowMask);
    half3 color = GlobalIllumination(brdfData, inputData.bakedGI, surface.occlusion, inputData.normalWS, inputData.viewDirectionWS);

    half3 packedNormalWS = PackNormal(surfaceData.normalWS.xyz);
    fragmentOutput.GBuffer0 = half4(surfaceData.baseColor.rgb, surfaceData.baseColor.a);
    fragmentOutput.GBuffer1 = 0;
    fragmentOutput.GBuffer2 = half4(packedNormalWS, surfaceData.normalWS.a);
    fragmentOutput.GBuffer3 = half4(surfaceData.emissive + color, surfaceData.baseColor.a);
#if OUTPUT_SHADOWMASK
    fragmentOutput.GBuffer4 = 0; // TODO: Does shader and pipeline target count must match?
#endif
#elif defined(DECAL_FORWARD_EMISSIVE)
    // Emissive need to be pre-exposed
    outEmissive.rgb = surfaceData.emissive;// *GetCurrentExposureMultiplier();
    outEmissive.a = 1.0;
#else
#endif
#endif
}
