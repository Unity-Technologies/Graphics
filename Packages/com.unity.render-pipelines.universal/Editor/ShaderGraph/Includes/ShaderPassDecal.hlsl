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

#if ((!defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_ALBEDO)) || (defined(_MATERIAL_AFFECTS_NORMAL) && defined(_MATERIAL_AFFECTS_NORMAL_BLEND))) && (defined(DECAL_SCREEN_SPACE) || defined(DECAL_GBUFFER))
#define DECAL_RECONSTRUCT_NORMAL
#elif defined(DECAL_ANGLE_FADE)
#define DECAL_LOAD_NORMAL
#endif

#ifdef _DECAL_LAYERS
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareRenderingLayerTexture.hlsl"
#endif

#if defined(DECAL_LOAD_NORMAL)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#endif

#if defined(DECAL_PROJECTOR) || defined(DECAL_RECONSTRUCT_NORMAL)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#endif

#ifdef DECAL_MESH
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DecalMeshBiasTypeEnum.cs.hlsl"
#endif
#ifdef DECAL_RECONSTRUCT_NORMAL
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/NormalReconstruction.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

void MeshDecalsPositionZBias(inout Varyings input)
{
#if UNITY_REVERSED_Z
    input.positionCS.z -= _DecalMeshDepthBias;
#else
    input.positionCS.z += _DecalMeshDepthBias;
#endif
}

void InitializeInputData(Varyings input, float3 positionWS, half3 normalWS, half3 viewDirectionWS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = positionWS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = viewDirectionWS;

#if defined(VARYINGS_NEED_SHADOW_COORD) && defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

#ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
    inputData.fogCoord = InitializeInputDataFog(float4(positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#endif

#if defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV.xy, half3(input.sh), normalWS);
#elif defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
#if !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.sh,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        input.positionCS.xy);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, half3(input.sh), normalWS);
#endif
#endif

#if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#endif

    #if defined(DEBUG_DISPLAY)
    #if defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
    #endif
    #if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV) && defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
    #elif defined(VARYINGS_NEED_SH)
    inputData.vertexSH = input.sh;
    #endif
    #endif

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
}

void GetSurface(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
{
    surfaceData.albedo = decalSurfaceData.baseColor.rgb;
    surfaceData.metallic = saturate(decalSurfaceData.metallic);
    surfaceData.specular = 0;
    surfaceData.smoothness = saturate(decalSurfaceData.smoothness);
    surfaceData.occlusion = decalSurfaceData.occlusion;
    surfaceData.emission = decalSurfaceData.emissive;
    surfaceData.alpha = saturate(decalSurfaceData.baseColor.w);
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;
}

PackedVaryings Vert(Attributes inputMesh)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(inputMesh);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#ifdef DECAL_MESH
    if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_VIEW_BIAS) // TODO: Check performance of branch
    {
        float3 viewDirectionOS = GetObjectSpaceNormalizeViewDir(inputMesh.positionOS);
        inputMesh.positionOS += viewDirectionOS * (_DecalMeshViewBias);
    }
    output = BuildVaryings(inputMesh);
    if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_DEPTH_BIAS) // TODO: Check performance of branch
    {
        MeshDecalsPositionZBias(output);
    }
#else
    output = BuildVaryings(inputMesh);
#endif

#if defined(VARYINGS_NEED_STATIC_LIGHTMAP_UV)
    OUTPUT_LIGHTMAP_UV(inputMesh.uv1, unity_LightmapST, output.staticLightmapUV);
#endif

#if defined(VARYINGS_NEED_DYNAMIC_LIGHTMAP_UV) && defined(DYNAMICLIGHTMAP_ON)
    output.dynamicLightmapUV.xy = inputMesh.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif

#if defined(VARYINGS_NEED_SH) && !defined(LIGHTMAP_ON)
    output.sh = float3(SampleSHVertex(half3(output.normalWS)));
#endif

    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);

    return packedOutput;
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

    half angleFadeFactor = 1.0;

    float2 positionCS = input.positionCS.xy;

    // Only screen space needs flip logic, other passes do not setup needed properties so we skip here
#if defined(DECAL_SCREEN_SPACE)
    TransformScreenUV(positionCS, _ScreenSize.y);
#endif

#ifdef _DECAL_LAYERS
#ifdef _RENDER_PASS_ENABLED
    uint surfaceRenderingLayer = DecodeMeshRenderingLayer(LOAD_FRAMEBUFFER_X_INPUT(GBUFFER4, positionCS.xy).r);
#else
    uint surfaceRenderingLayer = LoadSceneRenderingLayer(positionCS.xy);
#endif
    uint projectorRenderingLayer = uint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal));
    // This is simple trick to clip if there is no matching layers
    // Part (surfaceRenderingLayer & projectorRenderingLayer) will produce 0, 1, 2 ...
    // Finally we subtract with small value to remmap only zero to negative value
    clip((surfaceRenderingLayer & projectorRenderingLayer) - 0.1);
#endif

#if defined(DECAL_PROJECTOR)
#if UNITY_REVERSED_Z
#if _RENDER_PASS_ENABLED
    float depth = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, positionCS.xy).x;
#else
    float depth = LoadSceneDepth(positionCS.xy);
#endif
#else
#if _RENDER_PASS_ENABLED
    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LOAD_FRAMEBUFFER_X_INPUT(GBUFFER3, positionCS.xy));
#else
    // Adjust z to match NDC for OpenGL
    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, LoadSceneDepth(positionCS.xy));
#endif
#endif
#endif

#if defined(DECAL_RECONSTRUCT_NORMAL)
    #if defined(_DECAL_NORMAL_BLEND_HIGH)
        half3 normalWS = half3(ReconstructNormalTap9(positionCS.xy));
    #elif defined(_DECAL_NORMAL_BLEND_MEDIUM)
        half3 normalWS = half3(ReconstructNormalTap5(positionCS.xy));
    #else
        half3 normalWS = half3(ReconstructNormalDerivative(input.positionCS.xy));
    #endif
#elif defined(DECAL_LOAD_NORMAL)
    half3 normalWS = half3(LoadSceneNormals(positionCS.xy));
#endif

    float2 positionSS = input.positionCS.xy * _ScreenSize.zw;

#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        positionSS = RemapFoveatedRenderingNonUniformToLinearCS(input.positionCS.xy, true) * _ScreenSize.zw;
    }
#endif

#ifdef DECAL_PROJECTOR
    float3 positionWS = ComputeWorldSpacePosition(positionSS, depth, UNITY_MATRIX_I_VP);

#ifdef VARYINGS_NEED_POSITION_WS
    input.positionWS = positionWS;
#endif

    // Transform from relative world space to decal space (DS) to clip the decal
    float3 positionDS = TransformWorldToObject(positionWS);
    positionDS = positionDS * float3(1.0, -1.0, 1.0);

    // call clip as early as possible
    float clipValue = 0.5 - Max3(abs(positionDS).x, abs(positionDS).y, abs(positionDS).z);
    clip(clipValue);

    float2 texCoord = positionDS.xz + float2(0.5, 0.5);
#ifdef VARYINGS_NEED_TEXCOORD0
    input.texCoord0.xy = texCoord;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    input.texCoord1.xy = texCoord;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    input.texCoord2.xy = texCoord;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    input.texCoord3.xy = texCoord;
#endif

#ifdef DECAL_ANGLE_FADE
    // Check if this decal projector require angle fading
    half4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
    half2 angleFade = half2(normalToWorld[1][3], normalToWorld[2][3]);

    if (angleFade.y < 0.0f) // if angle fade is enabled
    {
        half3 decalNormal = half3(normalToWorld[0].z, normalToWorld[1].z, normalToWorld[2].z);
        half dotAngle = dot(normalWS, decalNormal);
        // See equation in DecalCreateDrawCallSystem.cs - simplified to a madd mul add here
        angleFadeFactor = saturate(angleFade.x + angleFade.y * (dotAngle * (dotAngle - 2.0)));
    }
#endif


#else // Decal mesh
    float3 positionWS = input.positionWS.xyz;
#endif

    half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);

    DecalSurfaceData surfaceData;
    GetSurfaceData(input, input.positionCS, angleFadeFactor, surfaceData);

#if defined(DECAL_DBUFFER)
    ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
#elif defined(DECAL_SCREEN_SPACE)

    // Blend normal with background
#ifdef DECAL_RECONSTRUCT_NORMAL
    surfaceData.normalWS.xyz = normalize(lerp(normalWS.xyz, surfaceData.normalWS.xyz, surfaceData.normalWS.w));
#endif

    InputData inputData;
    InitializeInputData(input, positionWS, surfaceData.normalWS.xyz, viewDirectionWS, inputData);

    SurfaceData surface = (SurfaceData)0;
    GetSurface(surfaceData, surface);

    half4 color = UniversalFragmentPBR(inputData, surface);

    color.rgb = MixFog(color.rgb, inputData.fogCoord);

    outColor = color;
#elif defined(DECAL_GBUFFER)

    // Need to reconstruct normal here for inputData.bakedGI, but also save off surfaceData.normalWS for correct GBuffer blending
    half3 normalToPack = surfaceData.normalWS.xyz;
#ifdef DECAL_RECONSTRUCT_NORMAL
    surfaceData.normalWS.xyz = normalize(lerp(normalWS.xyz, surfaceData.normalWS.xyz, surfaceData.normalWS.w));
#endif

    InputData inputData;
    InitializeInputData(input, positionWS, surfaceData.normalWS.xyz, viewDirectionWS, inputData);

    SurfaceData surface = (SurfaceData)0;
    GetSurface(surfaceData, surface);

    BRDFData brdfData;
    InitializeBRDFData(surface.albedo, surface.metallic, 0, surface.smoothness, surface.alpha, brdfData);

    // Skip GI if there is no abledo
#ifdef _MATERIAL_AFFECTS_ALBEDO
    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
    MixRealtimeAndBakedGI(mainLight, surfaceData.normalWS.xyz, inputData.bakedGI, inputData.shadowMask);
    half3 color = GlobalIllumination(brdfData, inputData.bakedGI, surface.occlusion, surfaceData.normalWS.xyz, inputData.viewDirectionWS);
#else
    half3 color = 0;
#endif

    // We can not use usual GBuffer functions (etc. BRDFDataToGbuffer) as we use alpha for blending
    #pragma warning (disable : 3578) // The output value isn't completely initialized.
    half3 packedNormalWS = PackNormal(normalToPack);
    fragmentOutput.GBuffer0 = half4(surfaceData.baseColor.rgb, surfaceData.baseColor.a);
    fragmentOutput.GBuffer1 = 0;
    fragmentOutput.GBuffer2 = half4(packedNormalWS, surfaceData.normalWS.a);
    fragmentOutput.GBuffer3 = half4(surfaceData.emissive + color, surfaceData.baseColor.a);
#if OUTPUT_SHADOWMASK
    fragmentOutput.GBuffer4 = inputData.shadowMask; // will have unity_ProbesOcclusion value if subtractive lighting is used (baked)
#endif
    #pragma warning (default : 3578) // Restore output value isn't completely initialized.

#elif defined(DECAL_FORWARD_EMISSIVE)
    // Emissive need to be pre-exposed
    outEmissive.rgb = surfaceData.emissive * GetCurrentExposureMultiplier();
    outEmissive.a = surfaceData.baseColor.a;
#else
#endif
#endif
}
