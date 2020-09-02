namespace UnityEngine.Rendering.HighDefinition
{
    static class HDShaderPassNames
    {
        // ShaderPass string - use to have consistent name through the code
        public static readonly string s_EmptyStr = "";
        public static readonly string s_ForwardStr = "Forward";
        public static readonly string s_DepthOnlyStr = "DepthOnly";
        public static readonly string s_DepthForwardOnlyStr = "DepthForwardOnly";
        public static readonly string s_ForwardOnlyStr = "ForwardOnly";
        public static readonly string s_GBufferStr = "GBuffer";
        public static readonly string s_GBufferWithPrepassStr = "GBufferWithPrepass";
        public static readonly string s_SRPDefaultUnlitStr = "SRPDefaultUnlit";
        public static readonly string s_MotionVectorsStr = "MotionVectors";
        public static readonly string s_DistortionVectorsStr = "DistortionVectors";
        public static readonly string s_TransparentDepthPrepassStr = "TransparentDepthPrepass";
        public static readonly string s_TransparentBackfaceStr = "TransparentBackface";
        public static readonly string s_TransparentDepthPostpassStr = "TransparentDepthPostpass";
        public static readonly string s_MetaStr = "META";
        public static readonly string s_ShadowCasterStr = "ShadowCaster";
        public static readonly string s_MeshDecalsMStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh_M];
        public static readonly string s_MeshDecalsSStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh_S];
        public static readonly string s_MeshDecalsMSStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh_MS];
        public static readonly string s_MeshDecalsAOStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh_AO];
        public static readonly string s_MeshDecalsMAOStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh_MAO];
        public static readonly string s_MeshDecalsAOSStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh_AOS];
        public static readonly string s_MeshDecalsMAOSStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh_MAOS];
        public static readonly string s_MeshDecals3RTStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh_3RT];
        public static readonly string s_ShaderGraphMeshDecals4RT = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh4RT];
        public static readonly string s_ShaderGraphMeshDecals3RT = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_DBufferMesh3RT];
        public static readonly string s_MeshDecalsForwardEmissive = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.Mesh_Emissive];
        public static readonly string s_ShaderGraphMeshDecalForwardEmissive = DecalSystem.s_MaterialSGDecalPassNames[(int)DecalSystem.MaterialSGDecalPass.ShaderGraph_MeshEmissive];

        // ShaderPass name
        public static readonly ShaderTagId s_EmptyName = new ShaderTagId(s_EmptyStr);
        public static readonly ShaderTagId s_ForwardName = new ShaderTagId(s_ForwardStr);
        public static readonly ShaderTagId s_DepthOnlyName = new ShaderTagId(s_DepthOnlyStr);
        public static readonly ShaderTagId s_DepthForwardOnlyName = new ShaderTagId(s_DepthForwardOnlyStr);
        public static readonly ShaderTagId s_ForwardOnlyName = new ShaderTagId(s_ForwardOnlyStr);
        public static readonly ShaderTagId s_GBufferName = new ShaderTagId(s_GBufferStr);
        public static readonly ShaderTagId s_GBufferWithPrepassName = new ShaderTagId(s_GBufferWithPrepassStr);
        public static readonly ShaderTagId s_SRPDefaultUnlitName = new ShaderTagId(s_SRPDefaultUnlitStr);
        public static readonly ShaderTagId s_MotionVectorsName = new ShaderTagId(s_MotionVectorsStr);
        public static readonly ShaderTagId s_DistortionVectorsName = new ShaderTagId(s_DistortionVectorsStr);
        public static readonly ShaderTagId s_TransparentDepthPrepassName = new ShaderTagId(s_TransparentDepthPrepassStr);
        public static readonly ShaderTagId s_TransparentBackfaceName = new ShaderTagId(s_TransparentBackfaceStr);
        public static readonly ShaderTagId s_TransparentDepthPostpassName = new ShaderTagId(s_TransparentDepthPostpassStr);
        public static readonly ShaderTagId s_MeshDecalsMName = new ShaderTagId(s_MeshDecalsMStr);
        public static readonly ShaderTagId s_MeshDecalsSName = new ShaderTagId(s_MeshDecalsSStr);
        public static readonly ShaderTagId s_MeshDecalsMSName = new ShaderTagId(s_MeshDecalsMSStr);
        public static readonly ShaderTagId s_MeshDecalsAOName = new ShaderTagId(s_MeshDecalsAOStr);
        public static readonly ShaderTagId s_MeshDecalsMAOName = new ShaderTagId(s_MeshDecalsMAOStr);
        public static readonly ShaderTagId s_MeshDecalsAOSName = new ShaderTagId(s_MeshDecalsAOSStr);
        public static readonly ShaderTagId s_MeshDecalsMAOSName = new ShaderTagId(s_MeshDecalsMAOSStr);
        public static readonly ShaderTagId s_MeshDecals3RTName = new ShaderTagId(s_MeshDecals3RTStr);
        public static readonly ShaderTagId s_ShaderGraphMeshDecalsName4RT = new ShaderTagId(s_ShaderGraphMeshDecals4RT);
        public static readonly ShaderTagId s_ShaderGraphMeshDecalsName3RT = new ShaderTagId(s_ShaderGraphMeshDecals3RT);
        public static readonly ShaderTagId s_MeshDecalsForwardEmissiveName = new ShaderTagId(s_MeshDecalsForwardEmissive);
        public static readonly ShaderTagId s_ShaderGraphMeshDecalsForwardEmissiveName = new ShaderTagId(s_ShaderGraphMeshDecalForwardEmissive);

        // Legacy name
        public static readonly ShaderTagId s_AlwaysName = new ShaderTagId("Always");
        public static readonly ShaderTagId s_ForwardBaseName = new ShaderTagId("ForwardBase");
        public static readonly ShaderTagId s_DeferredName = new ShaderTagId("Deferred");
        public static readonly ShaderTagId s_PrepassBaseName = new ShaderTagId("PrepassBase");
        public static readonly ShaderTagId s_VertexName = new ShaderTagId("Vertex");
        public static readonly ShaderTagId s_VertexLMRGBMName = new ShaderTagId("VertexLMRGBM");
        public static readonly ShaderTagId s_VertexLMName = new ShaderTagId("VertexLM");
    }

    // Pre-hashed shader ids - naming conventions are a bit off in this file as we use the same
    // fields names as in the shaders for ease of use...
    // TODO: Would be nice to clean this up at some point
    static class HDShaderIDs
    {
        public static readonly int _ZClip = Shader.PropertyToID("_ZClip");

        public static readonly int _HDShadowDatas = Shader.PropertyToID("_HDShadowDatas");
        public static readonly int _HDDirectionalShadowData = Shader.PropertyToID("_HDDirectionalShadowData");
        public static readonly int _ShadowmapAtlas = Shader.PropertyToID("_ShadowmapAtlas");
        public static readonly int _AreaLightShadowmapAtlas = Shader.PropertyToID("_AreaShadowmapAtlas");
        public static readonly int _AreaShadowmapMomentAtlas = Shader.PropertyToID("_AreaShadowmapMomentAtlas");
        public static readonly int _ShadowmapCascadeAtlas = Shader.PropertyToID("_ShadowmapCascadeAtlas");
        public static readonly int _AreaShadowAtlasSize = Shader.PropertyToID("_AreaShadowAtlasSize");
        public static readonly int _ShadowAtlasSize = Shader.PropertyToID("_ShadowAtlasSize");
        public static readonly int _CascadeShadowAtlasSize = Shader.PropertyToID("_CascadeShadowAtlasSize");
        public static readonly int _CascadeShadowCount = Shader.PropertyToID("_CascadeShadowCount");

        // Moment shadow map data
        public static readonly int _MomentShadowAtlas = Shader.PropertyToID("_MomentShadowAtlas");
        public static readonly int _MomentShadowmapSlotST = Shader.PropertyToID("_MomentShadowmapSlotST");
        public static readonly int _MomentShadowmapSize = Shader.PropertyToID("_MomentShadowmapSize");
        public static readonly int _SummedAreaTableInputInt = Shader.PropertyToID("_SummedAreaTableInputInt");
        public static readonly int _SummedAreaTableOutputInt = Shader.PropertyToID("_SummedAreaTableOutputInt");
        public static readonly int _SummedAreaTableInputFloat = Shader.PropertyToID("_SummedAreaTableInputFloat");
        public static readonly int _IMSKernelSize = Shader.PropertyToID("_IMSKernelSize");
        public static readonly int _SrcRect = Shader.PropertyToID("_SrcRect");
        public static readonly int _DstRect = Shader.PropertyToID("_DstRect");
        public static readonly int _EVSMExponent = Shader.PropertyToID("_EVSMExponent");
        public static readonly int _BlurWeightsStorage = Shader.PropertyToID("_BlurWeightsStorage");

        public static readonly int g_LayeredSingleIdxBuffer = Shader.PropertyToID("g_LayeredSingleIdxBuffer");
        public static readonly int _EnvLightIndexShift = Shader.PropertyToID("_EnvLightIndexShift");
        public static readonly int _DensityVolumeIndexShift = Shader.PropertyToID("_DensityVolumeIndexShift");
        public static readonly int g_isOrthographic = Shader.PropertyToID("g_isOrthographic");
        public static readonly int g_iNrVisibLights = Shader.PropertyToID("g_iNrVisibLights");

        public static readonly int g_mScrProjectionArr = Shader.PropertyToID("g_mScrProjectionArr");
        public static readonly int g_mInvScrProjectionArr = Shader.PropertyToID("g_mInvScrProjectionArr");

        public static readonly int g_iLog2NumClusters = Shader.PropertyToID("g_iLog2NumClusters");
        public static readonly int g_screenSize = Shader.PropertyToID("g_screenSize");
        public static readonly int g_iNumSamplesMSAA = Shader.PropertyToID("g_iNumSamplesMSAA");
        public static readonly int g_fNearPlane = Shader.PropertyToID("g_fNearPlane");
        public static readonly int g_fFarPlane = Shader.PropertyToID("g_fFarPlane");
        public static readonly int g_fClustScale = Shader.PropertyToID("g_fClustScale");
        public static readonly int g_fClustBase = Shader.PropertyToID("g_fClustBase");
        public static readonly int g_depth_tex = Shader.PropertyToID("g_depth_tex");
        public static readonly int g_vLayeredLightList = Shader.PropertyToID("g_vLayeredLightList");
        public static readonly int g_LayeredOffset = Shader.PropertyToID("g_LayeredOffset");
        public static readonly int g_vBigTileLightList = Shader.PropertyToID("g_vBigTileLightList");
        public static readonly int g_vLightListGlobal = Shader.PropertyToID("g_vLightListGlobal");
        public static readonly int g_logBaseBuffer = Shader.PropertyToID("g_logBaseBuffer");
        public static readonly int g_vBoundsBuffer = Shader.PropertyToID("g_vBoundsBuffer");
        public static readonly int _LightVolumeData = Shader.PropertyToID("_LightVolumeData");
        public static readonly int g_data = Shader.PropertyToID("g_data");
        public static readonly int g_mProjectionArr = Shader.PropertyToID("g_mProjectionArr");
        public static readonly int g_mInvProjectionArr = Shader.PropertyToID("g_mInvProjectionArr");
        public static readonly int g_viDimensions = Shader.PropertyToID("g_viDimensions");
        public static readonly int g_vLightList = Shader.PropertyToID("g_vLightList");

        public static readonly int g_BaseFeatureFlags = Shader.PropertyToID("g_BaseFeatureFlags");
        public static readonly int g_TileFeatureFlags = Shader.PropertyToID("g_TileFeatureFlags");

        public static readonly int g_DispatchIndirectBuffer = Shader.PropertyToID("g_DispatchIndirectBuffer");
        public static readonly int g_TileList = Shader.PropertyToID("g_TileList");
        public static readonly int g_NumTiles = Shader.PropertyToID("g_NumTiles");
        public static readonly int g_NumTilesX = Shader.PropertyToID("g_NumTilesX");
        public static readonly int g_VertexPerTile = Shader.PropertyToID("g_VertexPerTile");

        public static readonly int _NumTiles = Shader.PropertyToID("_NumTiles");

        public static readonly int _CookieAtlas = Shader.PropertyToID("_CookieAtlas");
        public static readonly int _CookieAtlasSize = Shader.PropertyToID("_CookieAtlasSize");
        public static readonly int _CookieAtlasData = Shader.PropertyToID("_CookieAtlasData");
        public static readonly int _CookieCubeTextures = Shader.PropertyToID("_CookieCubeTextures");
        public static readonly int _PlanarAtlasData = Shader.PropertyToID("_PlanarAtlasData");
        public static readonly int _EnvCubemapTextures = Shader.PropertyToID("_EnvCubemapTextures");
        public static readonly int _EnvSliceSize = Shader.PropertyToID("_EnvSliceSize");
        public static readonly int _Env2DTextures = Shader.PropertyToID("_Env2DTextures");
        public static readonly int _Env2DCaptureVP = Shader.PropertyToID("_Env2DCaptureVP");
        public static readonly int _Env2DCaptureForward = Shader.PropertyToID("_Env2DCaptureForward");
        public static readonly int _Env2DAtlasScaleOffset = Shader.PropertyToID("_Env2DAtlasScaleOffset");
        public static readonly int _DirectionalLightDatas = Shader.PropertyToID("_DirectionalLightDatas");
        public static readonly int _DirectionalLightCount = Shader.PropertyToID("_DirectionalLightCount");
        public static readonly int _LightDatas = Shader.PropertyToID("_LightDatas");
        public static readonly int _PunctualLightCount = Shader.PropertyToID("_PunctualLightCount");
        public static readonly int _AreaLightCount = Shader.PropertyToID("_AreaLightCount");
        public static readonly int _EnvLightDatas = Shader.PropertyToID("_EnvLightDatas");
        public static readonly int _EnvLightCount = Shader.PropertyToID("_EnvLightCount");
        public static readonly int _EnvProxyCount = Shader.PropertyToID("_EnvProxyCount");
        public static readonly int _NumTileBigTileX = Shader.PropertyToID("_NumTileBigTileX");
        public static readonly int _NumTileBigTileY = Shader.PropertyToID("_NumTileBigTileY");
        public static readonly int _NumTileFtplX = Shader.PropertyToID("_NumTileFtplX");
        public static readonly int _NumTileFtplY = Shader.PropertyToID("_NumTileFtplY");
        public static readonly int _NumTileClusteredX = Shader.PropertyToID("_NumTileClusteredX");
        public static readonly int _NumTileClusteredY = Shader.PropertyToID("_NumTileClusteredY");

        public static readonly int _IndirectLightingMultiplier = Shader.PropertyToID("_IndirectLightingMultiplier");

        public static readonly int g_isLogBaseBufferEnabled = Shader.PropertyToID("g_isLogBaseBufferEnabled");
        public static readonly int g_vLayeredOffsetsBuffer = Shader.PropertyToID("g_vLayeredOffsetsBuffer");

        public static readonly int _LightListToClear = Shader.PropertyToID("_LightListToClear");
        public static readonly int _LightListEntries = Shader.PropertyToID("_LightListEntries");

        public static readonly int _ViewTilesFlags = Shader.PropertyToID("_ViewTilesFlags");
        public static readonly int _MousePixelCoord = Shader.PropertyToID("_MousePixelCoord");
        public static readonly int _MouseClickPixelCoord = Shader.PropertyToID("_MouseClickPixelCoord");
        public static readonly int _DebugFont = Shader.PropertyToID("_DebugFont");
        public static readonly int _SliceIndex = Shader.PropertyToID("_SliceIndex");
        public static readonly int _DebugContactShadowLightIndex = Shader.PropertyToID("_DebugContactShadowLightIndex");

        public static readonly int _DebugViewMaterial = Shader.PropertyToID("_DebugViewMaterialArray");
        public static readonly int _DebugLightingMode = Shader.PropertyToID("_DebugLightingMode");
        public static readonly int _DebugShadowMapMode = Shader.PropertyToID("_DebugShadowMapMode");
        public static readonly int _DebugLightingAlbedo = Shader.PropertyToID("_DebugLightingAlbedo");
        public static readonly int _DebugLightingSmoothness = Shader.PropertyToID("_DebugLightingSmoothness");
        public static readonly int _DebugLightingNormal = Shader.PropertyToID("_DebugLightingNormal");
        public static readonly int _DebugLightingAmbientOcclusion = Shader.PropertyToID("_DebugLightingAmbientOcclusion");
        public static readonly int _DebugLightingSpecularColor = Shader.PropertyToID("_DebugLightingSpecularColor");
        public static readonly int _DebugLightingEmissiveColor = Shader.PropertyToID("_DebugLightingEmissiveColor");
        public static readonly int _AmbientOcclusionTexture = Shader.PropertyToID("_AmbientOcclusionTexture");
        public static readonly int _AmbientOcclusionTextureRW = Shader.PropertyToID("_AmbientOcclusionTextureRW");
        public static readonly int _MultiAmbientOcclusionTexture = Shader.PropertyToID("_MultiAmbientOcclusionTexture");
        public static readonly int _DebugMipMapMode = Shader.PropertyToID("_DebugMipMapMode");
        public static readonly int _DebugMipMapModeTerrainTexture = Shader.PropertyToID("_DebugMipMapModeTerrainTexture");
        public static readonly int _DebugSingleShadowIndex = Shader.PropertyToID("_DebugSingleShadowIndex");
        public static readonly int _DebugDepthPyramidMip = Shader.PropertyToID("_DebugDepthPyramidMip");
        public static readonly int _DebugDepthPyramidOffsets = Shader.PropertyToID("_DebugDepthPyramidOffsets");
        public static readonly int _DebugLightingMaterialValidateHighColor = Shader.PropertyToID("_DebugLightingMaterialValidateHighColor");
        public static readonly int _DebugLightingMaterialValidateLowColor = Shader.PropertyToID("_DebugLightingMaterialValidateLowColor");
        public static readonly int _DebugLightingMaterialValidatePureMetalColor = Shader.PropertyToID("_DebugLightingMaterialValidatePureMetalColor");
        public static readonly int _DebugFullScreenMode = Shader.PropertyToID("_DebugFullScreenMode");
        public static readonly int _DebugTransparencyOverdrawWeight = Shader.PropertyToID("_DebugTransparencyOverdrawWeight");

        public static readonly int _UseTileLightList = Shader.PropertyToID("_UseTileLightList");

        public static readonly int _FrameCount          = Shader.PropertyToID("_FrameCount");
        public static readonly int _Time                = Shader.PropertyToID("_Time");
        public static readonly int _SinTime             = Shader.PropertyToID("_SinTime");
        public static readonly int _CosTime             = Shader.PropertyToID("_CosTime");
        public static readonly int unity_DeltaTime      = Shader.PropertyToID("unity_DeltaTime");
        public static readonly int _TimeParameters      = Shader.PropertyToID("_TimeParameters");
        public static readonly int _LastTimeParameters  = Shader.PropertyToID("_LastTimeParameters");

        public static readonly int _EnvLightSkyEnabled = Shader.PropertyToID("_EnvLightSkyEnabled");
        public static readonly int _AmbientOcclusionParam = Shader.PropertyToID("_AmbientOcclusionParam");
        public static readonly int _SkyTexture = Shader.PropertyToID("_SkyTexture");

        public static readonly int _EnableSubsurfaceScattering = Shader.PropertyToID("_EnableSubsurfaceScattering");
        public static readonly int _TransmittanceMultiplier = Shader.PropertyToID("_TransmittanceMultiplier");
        public static readonly int _TexturingModeFlags = Shader.PropertyToID("_TexturingModeFlags");
        public static readonly int _TransmissionFlags = Shader.PropertyToID("_TransmissionFlags");
        public static readonly int _ThicknessRemaps = Shader.PropertyToID("_ThicknessRemaps");
        public static readonly int _ShapeParams = Shader.PropertyToID("_ShapeParams");
        public static readonly int _TransmissionTintsAndFresnel0 = Shader.PropertyToID("_TransmissionTintsAndFresnel0");
        public static readonly int specularLightingUAV = Shader.PropertyToID("specularLightingUAV");
        public static readonly int diffuseLightingUAV = Shader.PropertyToID("diffuseLightingUAV");
        public static readonly int _DiffusionProfileHashTable = Shader.PropertyToID("_DiffusionProfileHashTable");
        public static readonly int _DiffusionProfileCount = Shader.PropertyToID("_DiffusionProfileCount");
        public static readonly int _DiffusionProfileAsset = Shader.PropertyToID("_DiffusionProfileAsset");
        public static readonly int _MaterialID = Shader.PropertyToID("_MaterialID");

        public static readonly int g_TileListOffset = Shader.PropertyToID("g_TileListOffset");

        public static readonly int _LtcData = Shader.PropertyToID("_LtcData");
        public static readonly int _LtcGGXMatrix = Shader.PropertyToID("_LtcGGXMatrix");
        public static readonly int _LtcDisneyDiffuseMatrix = Shader.PropertyToID("_LtcDisneyDiffuseMatrix");
        public static readonly int _LtcMultiGGXFresnelDisneyDiffuse = Shader.PropertyToID("_LtcMultiGGXFresnelDisneyDiffuse");

        public static readonly int _ScreenSpaceShadowsTexture = Shader.PropertyToID("_ScreenSpaceShadowsTexture");
        public static readonly int _ContactShadowTexture = Shader.PropertyToID("_ContactShadowTexture");
        public static readonly int _ContactShadowTextureUAV = Shader.PropertyToID("_ContactShadowTextureUAV");
        public static readonly int _DirectionalShadowIndex = Shader.PropertyToID("_DirectionalShadowIndex");
        public static readonly int _ContactShadowOpacity = Shader.PropertyToID("_ContactShadowOpacity");
        public static readonly int _ContactShadowParamsParameters = Shader.PropertyToID("_ContactShadowParamsParameters");
        public static readonly int _ContactShadowParamsParameters2 = Shader.PropertyToID("_ContactShadowParamsParameters2");
        public static readonly int _DirectionalContactShadowSampleCount = Shader.PropertyToID("_SampleCount");
        public static readonly int _MicroShadowOpacity = Shader.PropertyToID("_MicroShadowOpacity");
        public static readonly int _DirectionalTransmissionMultiplier = Shader.PropertyToID("_DirectionalTransmissionMultiplier");
        public static readonly int _ShadowFrustumPlanes = Shader.PropertyToID("_ShadowFrustumPlanes");

        public static readonly int _StencilMask = Shader.PropertyToID("_StencilMask");
        public static readonly int _StencilRef = Shader.PropertyToID("_StencilRef");
        public static readonly int _StencilCmp = Shader.PropertyToID("_StencilCmp");

        public static readonly int _InputDepth = Shader.PropertyToID("_InputDepthTexture");

        public static readonly int _SrcBlend = Shader.PropertyToID("_SrcBlend");
        public static readonly int _DstBlend = Shader.PropertyToID("_DstBlend");

        public static readonly int _ColorMaskTransparentVel = Shader.PropertyToID("_ColorMaskTransparentVel");

        public static readonly int _StencilTexture = Shader.PropertyToID("_StencilTexture");

        // Used in the stencil resolve pass
        public static readonly int _OutputStencilBuffer = Shader.PropertyToID("_OutputStencilBuffer");
        public static readonly int _CoarseStencilBuffer = Shader.PropertyToID("_CoarseStencilBuffer");
        public static readonly int _CoarseStencilBufferSize = Shader.PropertyToID("_CoarseStencilBufferSize");


        // all decal properties
        public static readonly int _NormalToWorldID = Shader.PropertyToID("_NormalToWorld");
        public static readonly int _DecalAtlas2DID = Shader.PropertyToID("_DecalAtlas2D");
        public static readonly int _DecalHTileTexture = Shader.PropertyToID("_DecalHTileTexture");
        public static readonly int _DecalIndexShift = Shader.PropertyToID("_DecalIndexShift");
        public static readonly int _DecalCount = Shader.PropertyToID("_DecalCount");
        public static readonly int _DecalDatas = Shader.PropertyToID("_DecalDatas");
        public static readonly int _DecalNormalBufferStencilReadMask = Shader.PropertyToID("_DecalNormalBufferStencilReadMask");
        public static readonly int _DecalNormalBufferStencilRef = Shader.PropertyToID("_DecalNormalBufferStencilRef");
        public static readonly int _DecalPropertyMaskBuffer = Shader.PropertyToID("_DecalPropertyMaskBuffer");
        public static readonly int _DecalPropertyMaskBufferSRV = Shader.PropertyToID("_DecalPropertyMaskBufferSRV");


        public static readonly int _WorldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");
        public static readonly int _PrevCamPosRWS = Shader.PropertyToID("_PrevCamPosRWS");
        public static readonly int _ViewMatrix = Shader.PropertyToID("_ViewMatrix");
        public static readonly int _InvViewMatrix = Shader.PropertyToID("_InvViewMatrix");
        public static readonly int _ProjMatrix = Shader.PropertyToID("_ProjMatrix");
        public static readonly int _InvProjMatrix = Shader.PropertyToID("_InvProjMatrix");
        public static readonly int _NonJitteredViewProjMatrix = Shader.PropertyToID("_NonJitteredViewProjMatrix");
        public static readonly int _ViewProjMatrix = Shader.PropertyToID("_ViewProjMatrix");
        public static readonly int _CameraViewProjMatrix = Shader.PropertyToID("_CameraViewProjMatrix");
        public static readonly int _InvViewProjMatrix = Shader.PropertyToID("_InvViewProjMatrix");
        public static readonly int _ZBufferParams = Shader.PropertyToID("_ZBufferParams");
        public static readonly int _ProjectionParams = Shader.PropertyToID("_ProjectionParams");
        public static readonly int unity_OrthoParams = Shader.PropertyToID("unity_OrthoParams");
        public static readonly int _InvProjParam = Shader.PropertyToID("_InvProjParam");
        public static readonly int _ScreenSize = Shader.PropertyToID("_ScreenSize");
        public static readonly int _ScreenParams = Shader.PropertyToID("_ScreenParams");
        public static readonly int _RTHandleScale = Shader.PropertyToID("_RTHandleScale");
        public static readonly int _RTHandleScaleHistory = Shader.PropertyToID("_RTHandleScaleHistory");
        public static readonly int _PrevViewProjMatrix = Shader.PropertyToID("_PrevViewProjMatrix");
        public static readonly int _PrevInvViewProjMatrix = Shader.PropertyToID("_PrevInvViewProjMatrix");
        public static readonly int _FrustumPlanes = Shader.PropertyToID("_FrustumPlanes");
        public static readonly int _TaaFrameInfo = Shader.PropertyToID("_TaaFrameInfo");
        public static readonly int _TaaJitterStrength = Shader.PropertyToID("_TaaJitterStrength");

        public static readonly int _WorldSpaceCameraPos1 = Shader.PropertyToID("_WorldSpaceCameraPos1");
        public static readonly int _ViewMatrix1 = Shader.PropertyToID("_ViewMatrix1");

        // XR View Constants
        public static readonly int _XRViewCount = Shader.PropertyToID("_XRViewCount");
        public static readonly int _XRViewMatrix = Shader.PropertyToID("_XRViewMatrix");
        public static readonly int _XRInvViewMatrix = Shader.PropertyToID("_XRInvViewMatrix");
        public static readonly int _XRProjMatrix = Shader.PropertyToID("_XRProjMatrix");
        public static readonly int _XRInvProjMatrix = Shader.PropertyToID("_XRInvProjMatrix");
        public static readonly int _XRViewProjMatrix = Shader.PropertyToID("_XRViewProjMatrix");
        public static readonly int _XRInvViewProjMatrix = Shader.PropertyToID("_XRInvViewProjMatrix");
        public static readonly int _XRNonJitteredViewProjMatrix = Shader.PropertyToID("_XRNonJitteredViewProjMatrix");
        public static readonly int _XRPrevViewProjMatrix = Shader.PropertyToID("_XRPrevViewProjMatrix");
        public static readonly int _XRPrevInvViewProjMatrix = Shader.PropertyToID("_XRPrevInvViewProjMatrix");
        public static readonly int _XRPrevViewProjMatrixNoCameraTrans = Shader.PropertyToID("_XRPrevViewProjMatrixNoCameraTrans");
        public static readonly int _XRPixelCoordToViewDirWS = Shader.PropertyToID("_XRPixelCoordToViewDirWS");
        public static readonly int _XRWorldSpaceCameraPos = Shader.PropertyToID("_XRWorldSpaceCameraPos");
        public static readonly int _XRWorldSpaceCameraPosViewOffset = Shader.PropertyToID("_XRWorldSpaceCameraPosViewOffset");
        public static readonly int _XRPrevWorldSpaceCameraPos = Shader.PropertyToID("_XRPrevWorldSpaceCameraPos");

        public static readonly int _ColorTexture                   = Shader.PropertyToID("_ColorTexture");
        public static readonly int _DepthTexture                   = Shader.PropertyToID("_DepthTexture");
        public static readonly int _DepthValuesTexture             = Shader.PropertyToID("_DepthValuesTexture");
        public static readonly int _CameraColorTexture             = Shader.PropertyToID("_CameraColorTexture");
        public static readonly int _CameraColorTextureRW           = Shader.PropertyToID("_CameraColorTextureRW");
        public static readonly int _CameraSssDiffuseLightingBuffer = Shader.PropertyToID("_CameraSssDiffuseLightingTexture");
        public static readonly int _CameraFilteringBuffer          = Shader.PropertyToID("_CameraFilteringTexture");
        public static readonly int _IrradianceSource               = Shader.PropertyToID("_IrradianceSource");

        public static readonly int _EnableDecals = Shader.PropertyToID("_EnableDecals");
        public static readonly int _DecalAtlasResolution = Shader.PropertyToID("_DecalAtlasResolution");

        // MSAA shader properties
        public static readonly int _ColorTextureMS = Shader.PropertyToID("_ColorTextureMS");
        public static readonly int _DepthTextureMS = Shader.PropertyToID("_DepthTextureMS");
        public static readonly int _NormalTextureMS = Shader.PropertyToID("_NormalTextureMS");
        public static readonly int _MotionVectorTextureMS = Shader.PropertyToID("_MotionVectorTextureMS");
        public static readonly int _CameraDepthValuesTexture = Shader.PropertyToID("_CameraDepthValues");

        public static readonly int[] _GBufferTexture =
        {
            Shader.PropertyToID("_GBufferTexture0"),
            Shader.PropertyToID("_GBufferTexture1"),
            Shader.PropertyToID("_GBufferTexture2"),
            Shader.PropertyToID("_GBufferTexture3"),
            Shader.PropertyToID("_GBufferTexture4"),
            Shader.PropertyToID("_GBufferTexture5"),
            Shader.PropertyToID("_GBufferTexture6"),
            Shader.PropertyToID("_GBufferTexture7")
        };

        public static readonly int[] _GBufferTextureRW =
        {
            Shader.PropertyToID("_GBufferTexture0RW"),
            Shader.PropertyToID("_GBufferTexture1RW"),
            Shader.PropertyToID("_GBufferTexture2RW"),
            Shader.PropertyToID("_GBufferTexture3RW"),
            Shader.PropertyToID("_GBufferTexture4RW"),
            Shader.PropertyToID("_GBufferTexture5RW"),
            Shader.PropertyToID("_GBufferTexture6RW"),
            Shader.PropertyToID("_GBufferTexture7RW")
        };

        public static readonly int[] _DBufferTexture =
        {
            Shader.PropertyToID("_DBufferTexture0"),
            Shader.PropertyToID("_DBufferTexture1"),
            Shader.PropertyToID("_DBufferTexture2"),
            Shader.PropertyToID("_DBufferTexture3")
        };

        public static readonly int _SSSBufferTexture = Shader.PropertyToID("_SSSBufferTexture");
        public static readonly int _NormalBufferTexture = Shader.PropertyToID("_NormalBufferTexture");

        public static readonly int _EnableSSRefraction = Shader.PropertyToID("_EnableSSRefraction");
        public static readonly int _SSRefractionInvScreenWeightDistance = Shader.PropertyToID("_SSRefractionInvScreenWeightDistance");

        public static readonly int _SsrIterLimit                      = Shader.PropertyToID("_SsrIterLimit");
        public static readonly int _SsrThicknessScale                 = Shader.PropertyToID("_SsrThicknessScale");
        public static readonly int _SsrThicknessBias                  = Shader.PropertyToID("_SsrThicknessBias");
        public static readonly int _SsrRoughnessFadeEnd               = Shader.PropertyToID("_SsrRoughnessFadeEnd");
        public static readonly int _SsrRoughnessFadeRcpLength         = Shader.PropertyToID("_SsrRoughnessFadeRcpLength");
        public static readonly int _SsrRoughnessFadeEndTimesRcpLength = Shader.PropertyToID("_SsrRoughnessFadeEndTimesRcpLength");
        public static readonly int _SsrDepthPyramidMaxMip             = Shader.PropertyToID("_SsrDepthPyramidMaxMip");
        public static readonly int _SsrColorPyramidMaxMip             = Shader.PropertyToID("_SsrColorPyramidMaxMip");
        public static readonly int _SsrEdgeFadeRcpLength              = Shader.PropertyToID("_SsrEdgeFadeRcpLength");
        public static readonly int _SsrLightingTexture                = Shader.PropertyToID("_SsrLightingTexture");
        public static readonly int _SsrLightingTextureRW              = Shader.PropertyToID("_SsrLightingTextureRW");
        public static readonly int _SsrHitPointTexture                = Shader.PropertyToID("_SsrHitPointTexture");
        public static readonly int _SsrClearCoatMaskTexture           = Shader.PropertyToID("_SsrClearCoatMaskTexture");
        public static readonly int _SsrStencilBit                     = Shader.PropertyToID("_SsrStencilBit");
        public static readonly int _SsrReflectsSky                    = Shader.PropertyToID("_SsrReflectsSky");

        public static readonly int _DepthPyramidMipLevelOffsets       = Shader.PropertyToID("_DepthPyramidMipLevelOffsets");


        public static readonly int _ShadowMaskTexture = Shader.PropertyToID("_ShadowMaskTexture");
        public static readonly int _LightLayersTexture = Shader.PropertyToID("_LightLayersTexture");
        public static readonly int _DistortionTexture = Shader.PropertyToID("_DistortionTexture");
        public static readonly int _ColorPyramidTexture = Shader.PropertyToID("_ColorPyramidTexture");
        public static readonly int _ColorPyramidScale = Shader.PropertyToID("_ColorPyramidScale");
        public static readonly int _ColorPyramidUvScaleAndLimitPrevFrame = Shader.PropertyToID("_ColorPyramidUvScaleAndLimitPrevFrame");
        public static readonly int _DepthPyramidScale = Shader.PropertyToID("_DepthPyramidScale");

        public static readonly int _DebugColorPickerTexture = Shader.PropertyToID("_DebugColorPickerTexture");
        public static readonly int _ColorPickerMode = Shader.PropertyToID("_ColorPickerMode");
        public static readonly int _ApplyLinearToSRGB = Shader.PropertyToID("_ApplyLinearToSRGB");
        public static readonly int _ColorPickerFontColor = Shader.PropertyToID("_ColorPickerFontColor");
        public static readonly int _FalseColorEnabled = Shader.PropertyToID("_FalseColor");
        public static readonly int _FalseColorThresholds = Shader.PropertyToID("_FalseColorThresholds");

        public static readonly int _DebugMatCapTexture = Shader.PropertyToID("_DebugMatCapTexture");
        public static readonly int _MatcapViewScale = Shader.PropertyToID("_MatcapViewScale");
        public static readonly int _MatcapMixAlbedo = Shader.PropertyToID("_MatcapMixAlbedo");

        public static readonly int _DebugFullScreenTexture = Shader.PropertyToID("_DebugFullScreenTexture");
        public static readonly int _BlitTexture = Shader.PropertyToID("_BlitTexture");
        public static readonly int _BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
        public static readonly int _BlitMipLevel = Shader.PropertyToID("_BlitMipLevel");
        public static readonly int _BlitScaleBiasRt = Shader.PropertyToID("_BlitScaleBiasRt");
        public static readonly int _BlitTextureSize = Shader.PropertyToID("_BlitTextureSize");
        public static readonly int _BlitPaddingSize = Shader.PropertyToID("_BlitPaddingSize");
        public static readonly int _BlitTexArraySlice = Shader.PropertyToID("_BlitTexArraySlice");

        public static readonly int _WorldScales = Shader.PropertyToID("_WorldScales");
        public static readonly int _FilterKernels = Shader.PropertyToID("_FilterKernels");
        public static readonly int _FilterKernelsBasic = Shader.PropertyToID("_FilterKernelsBasic");
        public static readonly int _HalfRcpWeightedVariances = Shader.PropertyToID("_HalfRcpWeightedVariances");

        public static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
        public static readonly int _CameraMotionVectorsTexture = Shader.PropertyToID("_CameraMotionVectorsTexture");
        public static readonly int _CameraMotionVectorsSize = Shader.PropertyToID("_CameraMotionVectorsSize");
        public static readonly int _CameraMotionVectorsScale = Shader.PropertyToID("_CameraMotionVectorsScale");
        public static readonly int _FullScreenDebugMode = Shader.PropertyToID("_FullScreenDebugMode");
        public static readonly int _TransparencyOverdrawMaxPixelCost = Shader.PropertyToID("_TransparencyOverdrawMaxPixelCost");
        public static readonly int _CustomDepthTexture = Shader.PropertyToID("_CustomDepthTexture");
        public static readonly int _CustomColorTexture = Shader.PropertyToID("_CustomColorTexture");
        public static readonly int _CustomPassInjectionPoint = Shader.PropertyToID("_CustomPassInjectionPoint");
        public static readonly int _AfterPostProcessColorBuffer = Shader.PropertyToID("_AfterPostProcessColorBuffer");

        public static readonly int _InputCubemap = Shader.PropertyToID("_InputCubemap");
        public static readonly int _Mipmap = Shader.PropertyToID("_Mipmap");
        public static readonly int _ApplyExposure = Shader.PropertyToID("_ApplyExposure");        

        public static readonly int _DiffusionProfileHash = Shader.PropertyToID("_DiffusionProfileHash");
        public static readonly int _MaxRadius = Shader.PropertyToID("_MaxRadius");
        public static readonly int _ShapeParam = Shader.PropertyToID("_ShapeParam");
        public static readonly int _StdDev1 = Shader.PropertyToID("_StdDev1");
        public static readonly int _StdDev2 = Shader.PropertyToID("_StdDev2");
        public static readonly int _LerpWeight = Shader.PropertyToID("_LerpWeight");
        public static readonly int _HalfRcpVarianceAndWeight1 = Shader.PropertyToID("_HalfRcpVarianceAndWeight1");
        public static readonly int _HalfRcpVarianceAndWeight2 = Shader.PropertyToID("_HalfRcpVarianceAndWeight2");
        public static readonly int _TransmissionTint = Shader.PropertyToID("_TransmissionTint");
        public static readonly int _ThicknessRemap = Shader.PropertyToID("_ThicknessRemap");

        public static readonly int _Cubemap = Shader.PropertyToID("_Cubemap");
        public static readonly int _InvOmegaP = Shader.PropertyToID("_InvOmegaP");
        public static readonly int _SkyParam = Shader.PropertyToID("_SkyParam");
        public static readonly int _BackplateParameters0 = Shader.PropertyToID("_BackplateParameters0");
        public static readonly int _BackplateParameters1 = Shader.PropertyToID("_BackplateParameters1");
        public static readonly int _BackplateParameters2 = Shader.PropertyToID("_BackplateParameters2");
        public static readonly int _BackplateShadowTint = Shader.PropertyToID("_BackplateShadowTint");
        public static readonly int _BackplateShadowFilter = Shader.PropertyToID("_BackplateShadowFilter");
        public static readonly int _SkyIntensity = Shader.PropertyToID("_SkyIntensity");
        public static readonly int _PixelCoordToViewDirWS = Shader.PropertyToID("_PixelCoordToViewDirWS");

        public static readonly int _Size = Shader.PropertyToID("_Size");
        public static readonly int _Source = Shader.PropertyToID("_Source");
        public static readonly int _Destination = Shader.PropertyToID("_Destination");
        public static readonly int _Mip0 = Shader.PropertyToID("_Mip0");
        public static readonly int _SourceMip = Shader.PropertyToID("_SourceMip");
        public static readonly int _SrcOffsetAndLimit = Shader.PropertyToID("_SrcOffsetAndLimit");
        public static readonly int _SrcScaleBias = Shader.PropertyToID("_SrcScaleBias");
        public static readonly int _SrcUvLimits = Shader.PropertyToID("_SrcUvLimits");
        public static readonly int _DstOffset         = Shader.PropertyToID("_DstOffset");
        public static readonly int _DepthMipChain = Shader.PropertyToID("_DepthMipChain");


        public static readonly int _FogEnabled                        = Shader.PropertyToID("_FogEnabled");
        public static readonly int _PBRFogEnabled                     = Shader.PropertyToID("_PBRFogEnabled");
        public static readonly int _MaxFogDistance                    = Shader.PropertyToID("_MaxFogDistance");
        public static readonly int _AmbientProbeCoeffs                = Shader.PropertyToID("_AmbientProbeCoeffs");
        public static readonly int _HeightFogBaseExtinction           = Shader.PropertyToID("_HeightFogBaseExtinction");
        public static readonly int _HeightFogBaseScattering           = Shader.PropertyToID("_HeightFogBaseScattering");
        public static readonly int _HeightFogBaseHeight               = Shader.PropertyToID("_HeightFogBaseHeight");
        public static readonly int _HeightFogExponents                = Shader.PropertyToID("_HeightFogExponents");
        public static readonly int _EnableVolumetricFog               = Shader.PropertyToID("_EnableVolumetricFog");
        public static readonly int _GlobalFogAnisotropy               = Shader.PropertyToID("_GlobalFogAnisotropy");
        public static readonly int _CornetteShanksConstant            = Shader.PropertyToID("_CornetteShanksConstant");
        public static readonly int _VBufferViewportSize               = Shader.PropertyToID("_VBufferViewportSize");
        public static readonly int _VBufferSliceCount                 = Shader.PropertyToID("_VBufferSliceCount");
        public static readonly int _VBufferRcpSliceCount              = Shader.PropertyToID("_VBufferRcpSliceCount");
        public static readonly int _VBufferRcpInstancedViewCount      = Shader.PropertyToID("_VBufferRcpInstancedViewCount");
        public static readonly int _VBufferSharedUvScaleAndLimit      = Shader.PropertyToID("_VBufferSharedUvScaleAndLimit");
        public static readonly int _VBufferDistanceEncodingParams     = Shader.PropertyToID("_VBufferDistanceEncodingParams");
        public static readonly int _VBufferDistanceDecodingParams     = Shader.PropertyToID("_VBufferDistanceDecodingParams");
        public static readonly int _VBufferPrevViewportSize           = Shader.PropertyToID("_VBufferPrevViewportSize");
        public static readonly int _VBufferHistoryPrevUvScaleAndLimit = Shader.PropertyToID("_VBufferHistoryPrevUvScaleAndLimit");
        public static readonly int _VBufferPrevDepthEncodingParams    = Shader.PropertyToID("_VBufferPrevDepthEncodingParams");
        public static readonly int _VBufferPrevDepthDecodingParams    = Shader.PropertyToID("_VBufferPrevDepthDecodingParams");
        public static readonly int _VBufferLastSliceDist              = Shader.PropertyToID("_VBufferLastSliceDist");
        public static readonly int _VBufferCoordToViewDirWS           = Shader.PropertyToID("_VBufferCoordToViewDirWS");
        public static readonly int _VBufferUnitDepthTexelSpacing      = Shader.PropertyToID("_VBufferUnitDepthTexelSpacing");
        public static readonly int _VBufferDensity                    = Shader.PropertyToID("_VBufferDensity");
        public static readonly int _VBufferLighting                   = Shader.PropertyToID("_VBufferLighting");
        public static readonly int _VBufferLightingIntegral           = Shader.PropertyToID("_VBufferLightingIntegral");
        public static readonly int _VBufferLightingHistory            = Shader.PropertyToID("_VBufferLightingHistory");
        public static readonly int _VBufferLightingHistoryIsValid     = Shader.PropertyToID("_VBufferLightingHistoryIsValid");
        public static readonly int _VBufferLightingFeedback           = Shader.PropertyToID("_VBufferLightingFeedback");
        public static readonly int _VBufferSampleOffset               = Shader.PropertyToID("_VBufferSampleOffset");
        public static readonly int _VolumeBounds                      = Shader.PropertyToID("_VolumeBounds");
        public static readonly int _VolumeData                        = Shader.PropertyToID("_VolumeData");
        public static readonly int _NumVisibleDensityVolumes          = Shader.PropertyToID("_NumVisibleDensityVolumes");
        public static readonly int _VolumeMaskAtlas                   = Shader.PropertyToID("_VolumeMaskAtlas");
        public static readonly int _VolumeMaskDimensions              = Shader.PropertyToID("_VolumeMaskDimensions");

        public static readonly int _EnableLightLayers                 = Shader.PropertyToID("_EnableLightLayers");
        public static readonly int _OffScreenRendering                = Shader.PropertyToID("_OffScreenRendering");
        public static readonly int _OffScreenDownsampleFactor         = Shader.PropertyToID("_OffScreenDownsampleFactor");
        public static readonly int _ReplaceDiffuseForIndirect         = Shader.PropertyToID("_ReplaceDiffuseForIndirect");
        public static readonly int _EnableSkyReflection               = Shader.PropertyToID("_EnableSkyReflection");

        public static readonly int _GroundIrradianceTexture           = Shader.PropertyToID("_GroundIrradianceTexture");
        public static readonly int _GroundIrradianceTable             = Shader.PropertyToID("_GroundIrradianceTable");
        public static readonly int _GroundIrradianceTableOrder        = Shader.PropertyToID("_GroundIrradianceTableOrder");
        public static readonly int _AirSingleScatteringTexture        = Shader.PropertyToID("_AirSingleScatteringTexture");
        public static readonly int _AirSingleScatteringTable          = Shader.PropertyToID("_AirSingleScatteringTable");
        public static readonly int _AerosolSingleScatteringTexture    = Shader.PropertyToID("_AerosolSingleScatteringTexture");
        public static readonly int _AerosolSingleScatteringTable      = Shader.PropertyToID("_AerosolSingleScatteringTable");
        public static readonly int _MultipleScatteringTexture         = Shader.PropertyToID("_MultipleScatteringTexture");
        public static readonly int _MultipleScatteringTable           = Shader.PropertyToID("_MultipleScatteringTable");
        public static readonly int _MultipleScatteringTableOrder      = Shader.PropertyToID("_MultipleScatteringTableOrder");

        public static readonly int _PlanetaryRadius                   = Shader.PropertyToID("_PlanetaryRadius");
        public static readonly int _RcpPlanetaryRadius                = Shader.PropertyToID("_RcpPlanetaryRadius");
        public static readonly int _AtmosphericDepth                  = Shader.PropertyToID("_AtmosphericDepth");
        public static readonly int _RcpAtmosphericDepth               = Shader.PropertyToID("_RcpAtmosphericDepth");

        public static readonly int _AtmosphericRadius                 = Shader.PropertyToID("_AtmosphericRadius");
        public static readonly int _AerosolAnisotropy                 = Shader.PropertyToID("_AerosolAnisotropy");
        public static readonly int _AerosolPhasePartConstant          = Shader.PropertyToID("_AerosolPhasePartConstant");

        public static readonly int _AirDensityFalloff                 = Shader.PropertyToID("_AirDensityFalloff");
        public static readonly int _AirScaleHeight                    = Shader.PropertyToID("_AirScaleHeight");
        public static readonly int _AerosolDensityFalloff             = Shader.PropertyToID("_AerosolDensityFalloff");
        public static readonly int _AerosolScaleHeight                = Shader.PropertyToID("_AerosolScaleHeight");

        public static readonly int _AirSeaLevelExtinction             = Shader.PropertyToID("_AirSeaLevelExtinction");
        public static readonly int _AerosolSeaLevelExtinction         = Shader.PropertyToID("_AerosolSeaLevelExtinction");

        public static readonly int _AirSeaLevelScattering             = Shader.PropertyToID("_AirSeaLevelScattering");
        public static readonly int _AerosolSeaLevelScattering         = Shader.PropertyToID("_AerosolSeaLevelScattering");

        public static readonly int _GroundAlbedo                      = Shader.PropertyToID("_GroundAlbedo");
        public static readonly int _IntensityMultiplier               = Shader.PropertyToID("_IntensityMultiplier");

        public static readonly int _PlanetCenterPosition              = Shader.PropertyToID("_PlanetCenterPosition");

        public static readonly int _PlanetRotation                    = Shader.PropertyToID("_PlanetRotation");
        public static readonly int _SpaceRotation                     = Shader.PropertyToID("_SpaceRotation");

        public static readonly int _HasGroundAlbedoTexture            = Shader.PropertyToID("_HasGroundAlbedoTexture");
        public static readonly int _GroundAlbedoTexture               = Shader.PropertyToID("_GroundAlbedoTexture");

        public static readonly int _HasGroundEmissionTexture          = Shader.PropertyToID("_HasGroundEmissionTexture");
        public static readonly int _GroundEmissionTexture             = Shader.PropertyToID("_GroundEmissionTexture");
        public static readonly int _GroundEmissionMultiplier          = Shader.PropertyToID("_GroundEmissionMultiplier");

        public static readonly int _HasSpaceEmissionTexture           = Shader.PropertyToID("_HasSpaceEmissionTexture");
        public static readonly int _SpaceEmissionTexture              = Shader.PropertyToID("_SpaceEmissionTexture");
        public static readonly int _SpaceEmissionMultiplier           = Shader.PropertyToID("_SpaceEmissionMultiplier");

        public static readonly int _RenderSunDisk                     = Shader.PropertyToID("_RenderSunDisk");

        public static readonly int _ColorSaturation                   = Shader.PropertyToID("_ColorSaturation");
        public static readonly int _AlphaSaturation                   = Shader.PropertyToID("_AlphaSaturation");
        public static readonly int _AlphaMultiplier                   = Shader.PropertyToID("_AlphaMultiplier");
        public static readonly int _HorizonTint                       = Shader.PropertyToID("_HorizonTint");
        public static readonly int _ZenithTint                        = Shader.PropertyToID("_ZenithTint");
        public static readonly int _HorizonZenithShiftPower           = Shader.PropertyToID("_HorizonZenithShiftPower");
        public static readonly int _HorizonZenithShiftScale           = Shader.PropertyToID("_HorizonZenithShiftScale");

        // Raytracing variables
        public static readonly int _RaytracingRayBias               = Shader.PropertyToID("_RaytracingRayBias");
        public static readonly int _RayTracingLayerMask             = Shader.PropertyToID("_RayTracingLayerMask");
        public static readonly int _RaytracingNumSamples            = Shader.PropertyToID("_RaytracingNumSamples");
        public static readonly int _RaytracingSampleIndex           = Shader.PropertyToID("_RaytracingSampleIndex");
        public static readonly int _RaytracingRayMaxLength          = Shader.PropertyToID("_RaytracingRayMaxLength");
        public static readonly int _PixelSpreadAngleTangent         = Shader.PropertyToID("_PixelSpreadAngleTangent");
        public static readonly int _RaytracingFrameIndex            = Shader.PropertyToID("_RaytracingFrameIndex");
        public static readonly int _RaytracingPixelSpreadAngle      = Shader.PropertyToID("_RaytracingPixelSpreadAngle");
        public static readonly string _RaytracingAccelerationStructureName          = "_RaytracingAccelerationStructure";

        // Light Cluster
        public static readonly int _MinClusterPos                   = Shader.PropertyToID("_MinClusterPos");
        public static readonly int _MaxClusterPos                   = Shader.PropertyToID("_MaxClusterPos");
        public static readonly int _LightPerCellCount               = Shader.PropertyToID("_LightPerCellCount");
        public static readonly int _LightDatasRT                    = Shader.PropertyToID("_LightDatasRT");
        public static readonly int _EnvLightDatasRT                 = Shader.PropertyToID("_EnvLightDatasRT");
        public static readonly int _PunctualLightCountRT            = Shader.PropertyToID("_PunctualLightCountRT");
        public static readonly int _AreaLightCountRT                = Shader.PropertyToID("_AreaLightCountRT");
        public static readonly int _EnvLightCountRT                 = Shader.PropertyToID("_EnvLightCountRT");
        public static readonly int _RaytracingLightCluster          = Shader.PropertyToID("_RaytracingLightCluster");

        // Denoising
        public static readonly int _HistoryBuffer                   = Shader.PropertyToID("_HistoryBuffer");
        public static readonly int _ValidationBuffer                = Shader.PropertyToID("_ValidationBuffer");
        public static readonly int _ValidationBufferRW              = Shader.PropertyToID("_ValidationBufferRW");
        public static readonly int _HistoryDepthTexture             = Shader.PropertyToID("_HistoryDepthTexture");
        public static readonly int _HistoryNormalBufferTexture      = Shader.PropertyToID("_HistoryNormalBufferTexture");
        public static readonly int _RaytracingDenoiseRadius         = Shader.PropertyToID("_RaytracingDenoiseRadius");
        public static readonly int _DenoiserFilterRadius            = Shader.PropertyToID("_DenoiserFilterRadius");
        public static readonly int _NormalHistoryCriterion          = Shader.PropertyToID("_NormalHistoryCriterion");
        public static readonly int _DenoiseInputTexture             = Shader.PropertyToID("_DenoiseInputTexture");
        public static readonly int _DenoiseOutputTextureRW          = Shader.PropertyToID("_DenoiseOutputTextureRW");
        public static readonly int _HalfResolutionFilter            = Shader.PropertyToID("_HalfResolutionFilter");
        public static readonly int _DenoisingHistorySlot            = Shader.PropertyToID("_DenoisingHistorySlot");
        public static readonly int _HistoryValidity                 = Shader.PropertyToID("_HistoryValidity");
        public static readonly int _ReflectionFilterMapping         = Shader.PropertyToID("_ReflectionFilterMapping");
        public static readonly int _DenoisingHistorySlice           = Shader.PropertyToID("_DenoisingHistorySlice");
        public static readonly int _DenoisingHistoryMask            = Shader.PropertyToID("_DenoisingHistoryMask");
        public static readonly int _DenoisingHistoryMaskSn          = Shader.PropertyToID("_DenoisingHistoryMaskSn");
        public static readonly int _DenoisingHistoryMaskUn          = Shader.PropertyToID("_DenoisingHistoryMaskUn");
        public static readonly int _HistoryValidityBuffer           = Shader.PropertyToID("_HistoryValidityBuffer");
        public static readonly int _ValidityOutputTextureRW         = Shader.PropertyToID("_ValidityOutputTextureRW");
        public static readonly int _VelocityBuffer                  = Shader.PropertyToID("_VelocityBuffer");

        // Reflections
        public static readonly int _ReflectionHistorybufferRW       = Shader.PropertyToID("_ReflectionHistorybufferRW");
        public static readonly int _CurrentFrameTexture             = Shader.PropertyToID("_CurrentFrameTexture");
        public static readonly int _AccumulatedFrameTexture         = Shader.PropertyToID("_AccumulatedFrameTexture");
        public static readonly int _TemporalAccumuationWeight       = Shader.PropertyToID("_TemporalAccumuationWeight");
        public static readonly int _SpatialFilterRadius             = Shader.PropertyToID("_SpatialFilterRadius");
        public static readonly int _RaytracingReflectionMaxDistance = Shader.PropertyToID("_RaytracingReflectionMaxDistance");
        public static readonly int _RaytracingHitDistanceTexture    = Shader.PropertyToID("_RaytracingHitDistanceTexture");
        public static readonly int _RaytracingIntensityClamp        = Shader.PropertyToID("_RaytracingIntensityClamp");
        public static readonly int _RaytracingPreExposition         = Shader.PropertyToID("_RaytracingPreExposition");
        public static readonly int _RaytracingReflectionMinSmoothness   = Shader.PropertyToID("_RaytracingReflectionMinSmoothness");
        public static readonly int _RaytracingReflectionSmoothnessFadeStart   = Shader.PropertyToID("_RaytracingReflectionSmoothnessFadeStart");
        public static readonly int _RaytracingVSNormalTexture       = Shader.PropertyToID("_RaytracingVSNormalTexture");
        public static readonly int _RaytracingIncludeSky            = Shader.PropertyToID("_RaytracingIncludeSky");
        public static readonly int _UseRayTracedReflections         = Shader.PropertyToID("_UseRayTracedReflections");

        // Shadows
        public static readonly int _RaytracingTargetAreaLight       = Shader.PropertyToID("_RaytracingTargetAreaLight");
        public static readonly int _RaytracingShadowSlot            = Shader.PropertyToID("_RaytracingShadowSlot");
        public static readonly int _RaytracingChannelMask           = Shader.PropertyToID("_RaytracingChannelMask");
        public static readonly int _RaytracingAreaWorldToLocal      = Shader.PropertyToID("_RaytracingAreaWorldToLocal");
        public static readonly int _RaytracedAreaShadowSample       = Shader.PropertyToID("_RaytracedAreaShadowSample");
        public static readonly int _RaytracedAreaShadowIntegration = Shader.PropertyToID("_RaytracedAreaShadowIntegration");
        public static readonly int _RaytracingDirectionBuffer       = Shader.PropertyToID("_RaytracingDirectionBuffer");
        public static readonly int _RaytracingDistanceBuffer        = Shader.PropertyToID("_RaytracingDistanceBuffer");
        public static readonly int _AreaShadowTexture               = Shader.PropertyToID("_AreaShadowTexture");
        public static readonly int _AreaShadowTextureRW             = Shader.PropertyToID("_AreaShadowTextureRW");
        public static readonly int _ScreenSpaceShadowsTextureRW     = Shader.PropertyToID("_ScreenSpaceShadowsTextureRW");
        public static readonly int _AreaShadowHistory               = Shader.PropertyToID("_AreaShadowHistory");
        public static readonly int _AreaShadowHistoryRW             = Shader.PropertyToID("_AreaShadowHistoryRW");
        public static readonly int _AnalyticProbBuffer              = Shader.PropertyToID("_AnalyticProbBuffer");
        public static readonly int _AnalyticHistoryBuffer           = Shader.PropertyToID("_AnalyticHistoryBuffer");
        public static readonly int _RaytracingLightRadius           = Shader.PropertyToID("_RaytracingLightRadius");

        public static readonly int _RaytracingSpotAngle             = Shader.PropertyToID("_RaytracingSpotAngle");
        public static readonly int _RaytracedShadowIntegration      = Shader.PropertyToID("_RaytracedShadowIntegration");
        public static readonly int _RaytracedColorShadowIntegration = Shader.PropertyToID("_RaytracedColorShadowIntegration");
        public static readonly int _DirectionalLightAngle            = Shader.PropertyToID("_DirectionalLightAngle");

        // Ambient occlusion
        public static readonly int _RaytracingAOIntensity           = Shader.PropertyToID("_RaytracingAOIntensity");

        // Ray count
        public static readonly int _RayCountEnabled                 = Shader.PropertyToID("_RayCountEnabled");
        public static readonly int _RayCountTexture                 = Shader.PropertyToID("_RayCountTexture");
        public static readonly int _RayCountType                    = Shader.PropertyToID("_RayCountType");
        public static readonly int _InputRayCountTexture            = Shader.PropertyToID("_InputRayCountTexture");
        public static readonly int _InputRayCountBuffer             = Shader.PropertyToID("_InputRayCountBuffer");
        public static readonly int _OutputRayCountBuffer            = Shader.PropertyToID("_OutputRayCountBuffer");
        public static readonly int _InputBufferDimension            = Shader.PropertyToID("_InputBufferDimension");
        public static readonly int _OutputBufferDimension           = Shader.PropertyToID("_OutputBufferDimension");

        // Primary Visibility
        public static readonly int _RaytracingFlagMask              = Shader.PropertyToID("_RaytracingFlagMask");
        public static readonly int _RaytracingMinRecursion          = Shader.PropertyToID("_RaytracingMinRecursion");
        public static readonly int _RaytracingMaxRecursion          = Shader.PropertyToID("_RaytracingMaxRecursion");
        public static readonly int _RaytracingPrimaryDebug          = Shader.PropertyToID("_RaytracingPrimaryDebug");
        public static readonly int _RaytracingCameraNearPlane       = Shader.PropertyToID("_RaytracingCameraNearPlane");

        // Indirect diffuse
        public static readonly int _RaytracedIndirectDiffuse            = Shader.PropertyToID("_RaytracedIndirectDiffuse");
        public static readonly int _IndirectDiffuseTexture              = Shader.PropertyToID("_IndirectDiffuseTexture");
        public static readonly int _IndirectDiffuseTextureRW            = Shader.PropertyToID("_IndirectDiffuseTextureRW");
        public static readonly int _IndirectDiffuseHitPointTextureRW    = Shader.PropertyToID("_IndirectDiffuseHitPointTextureRW");
        public static readonly int _UpscaledIndirectDiffuseTextureRW    = Shader.PropertyToID("_UpscaledIndirectDiffuseTextureRW");

        // Deferred Lighting
        public static readonly int _RaytracingLitBufferRW           = Shader.PropertyToID("_RaytracingLitBufferRW");
        public static readonly int _RaytracingDiffuseRay            = Shader.PropertyToID("_RaytracingDiffuseRay");

        // Ray binning
        public static readonly int _RayBinResult                    = Shader.PropertyToID("_RayBinResult");
        public static readonly int _RayBinSizeResult                = Shader.PropertyToID("_RayBinSizeResult");
        public static readonly int _RayBinTileCountX                = Shader.PropertyToID("_RayBinTileCountX");

        // Sub Surface
        public static readonly int _ThroughputTextureRW             = Shader.PropertyToID("_ThroughputTextureRW");
        public static readonly int _NormalTextureRW                 = Shader.PropertyToID("_NormalTextureRW");
        public static readonly int _PositionTextureRW               = Shader.PropertyToID("_PositionTextureRW");
        public static readonly int _DiffuseLightingTextureRW        = Shader.PropertyToID("_DiffuseLightingTextureRW");
        
        // Preintegrated texture name
        public static readonly int _PreIntegratedFGD_GGXDisneyDiffuse = Shader.PropertyToID("_PreIntegratedFGD_GGXDisneyDiffuse");
        public static readonly int _PreIntegratedFGD_CharlieAndFabric = Shader.PropertyToID("_PreIntegratedFGD_CharlieAndFabric");

        public static readonly int _ExposureTexture                = Shader.PropertyToID("_ExposureTexture");
        public static readonly int _PrevExposureTexture            = Shader.PropertyToID("_PrevExposureTexture");
        public static readonly int _PreviousExposureTexture        = Shader.PropertyToID("_PreviousExposureTexture");
        public static readonly int _ExposureParams                 = Shader.PropertyToID("_ExposureParams");
        public static readonly int _AdaptationParams               = Shader.PropertyToID("_AdaptationParams");
        public static readonly int _ExposureCurveTexture           = Shader.PropertyToID("_ExposureCurveTexture");
        public static readonly int _ProbeExposureScale             = Shader.PropertyToID("_ProbeExposureScale");
        public static readonly int _Variants                       = Shader.PropertyToID("_Variants");
        public static readonly int _InputTexture                   = Shader.PropertyToID("_InputTexture");
        public static readonly int _OutputTexture                  = Shader.PropertyToID("_OutputTexture");
        public static readonly int _SourceTexture                  = Shader.PropertyToID("_SourceTexture");
        public static readonly int _InputHistoryTexture            = Shader.PropertyToID("_InputHistoryTexture");
        public static readonly int _OutputHistoryTexture           = Shader.PropertyToID("_OutputHistoryTexture");

        public static readonly int _TargetScale                    = Shader.PropertyToID("_TargetScale");
        public static readonly int _Params                         = Shader.PropertyToID("_Params");
        public static readonly int _Params1                        = Shader.PropertyToID("_Params1");
        public static readonly int _Params2                        = Shader.PropertyToID("_Params2");
        public static readonly int _BokehKernel                    = Shader.PropertyToID("_BokehKernel");
        public static readonly int _InputCoCTexture                = Shader.PropertyToID("_InputCoCTexture");
        public static readonly int _InputHistoryCoCTexture         = Shader.PropertyToID("_InputHistoryCoCTexture");
        public static readonly int _OutputCoCTexture               = Shader.PropertyToID("_OutputCoCTexture");
        public static readonly int _OutputNearCoCTexture           = Shader.PropertyToID("_OutputNearCoCTexture");
        public static readonly int _OutputNearTexture              = Shader.PropertyToID("_OutputNearTexture");
        public static readonly int _OutputFarCoCTexture            = Shader.PropertyToID("_OutputFarCoCTexture");
        public static readonly int _OutputFarTexture               = Shader.PropertyToID("_OutputFarTexture");
        public static readonly int _OutputMip1                     = Shader.PropertyToID("_OutputMip1");
        public static readonly int _OutputMip2                     = Shader.PropertyToID("_OutputMip2");
        public static readonly int _OutputMip3                     = Shader.PropertyToID("_OutputMip3");
        public static readonly int _OutputMip4                     = Shader.PropertyToID("_OutputMip4");
        public static readonly int _IndirectBuffer                 = Shader.PropertyToID("_IndirectBuffer");
        public static readonly int _InputNearCoCTexture            = Shader.PropertyToID("_InputNearCoCTexture");
        public static readonly int _NearTileList                   = Shader.PropertyToID("_NearTileList");
        public static readonly int _InputFarTexture                = Shader.PropertyToID("_InputFarTexture");
        public static readonly int _InputNearTexture               = Shader.PropertyToID("_InputNearTexture");
        public static readonly int _InputFarCoCTexture             = Shader.PropertyToID("_InputFarCoCTexture");
        public static readonly int _FarTileList                    = Shader.PropertyToID("_FarTileList");
        public static readonly int _TileList                       = Shader.PropertyToID("_TileList");
        public static readonly int _TexelSize                      = Shader.PropertyToID("_TexelSize");
        public static readonly int _InputDilatedCoCTexture         = Shader.PropertyToID("_InputDilatedCoCTexture");
        public static readonly int _OutputAlphaTexture             = Shader.PropertyToID("_OutputAlphaTexture");
        public static readonly int _InputNearAlphaTexture          = Shader.PropertyToID("_InputNearAlphaTexture");
        public static readonly int _CoCTargetScale                 = Shader.PropertyToID("_CoCTargetScale");

        public static readonly int _BloomParams                    = Shader.PropertyToID("_BloomParams");
        public static readonly int _BloomTint                      = Shader.PropertyToID("_BloomTint");
        public static readonly int _BloomTexture                   = Shader.PropertyToID("_BloomTexture");
        public static readonly int _BloomDirtTexture               = Shader.PropertyToID("_BloomDirtTexture");
        public static readonly int _BloomDirtScaleOffset           = Shader.PropertyToID("_BloomDirtScaleOffset");
        public static readonly int _InputLowTexture                = Shader.PropertyToID("_InputLowTexture");
        public static readonly int _InputHighTexture               = Shader.PropertyToID("_InputHighTexture");
        public static readonly int _BloomBicubicParams             = Shader.PropertyToID("_BloomBicubicParams");
        public static readonly int _BloomThreshold                 = Shader.PropertyToID("_BloomThreshold");

        public static readonly int _ChromaSpectralLut              = Shader.PropertyToID("_ChromaSpectralLut");
        public static readonly int _ChromaParams                   = Shader.PropertyToID("_ChromaParams");

        public static readonly int _VignetteParams1                = Shader.PropertyToID("_VignetteParams1");
        public static readonly int _VignetteParams2                = Shader.PropertyToID("_VignetteParams2");
        public static readonly int _VignetteColor                  = Shader.PropertyToID("_VignetteColor");
        public static readonly int _VignetteMask                   = Shader.PropertyToID("_VignetteMask");

        public static readonly int _DistortionParams1              = Shader.PropertyToID("_DistortionParams1");
        public static readonly int _DistortionParams2              = Shader.PropertyToID("_DistortionParams2");

        public static readonly int _LogLut3D                       = Shader.PropertyToID("_LogLut3D");
        public static readonly int _LogLut3D_Params                = Shader.PropertyToID("_LogLut3D_Params");
        public static readonly int _ColorBalance                   = Shader.PropertyToID("_ColorBalance");
        public static readonly int _ColorFilter                    = Shader.PropertyToID("_ColorFilter");
        public static readonly int _ChannelMixerRed                = Shader.PropertyToID("_ChannelMixerRed");
        public static readonly int _ChannelMixerGreen              = Shader.PropertyToID("_ChannelMixerGreen");
        public static readonly int _ChannelMixerBlue               = Shader.PropertyToID("_ChannelMixerBlue");
        public static readonly int _HueSatCon                      = Shader.PropertyToID("_HueSatCon");
        public static readonly int _Lift                           = Shader.PropertyToID("_Lift");
        public static readonly int _Gamma                          = Shader.PropertyToID("_Gamma");
        public static readonly int _Gain                           = Shader.PropertyToID("_Gain");
        public static readonly int _Shadows                        = Shader.PropertyToID("_Shadows");
        public static readonly int _Midtones                       = Shader.PropertyToID("_Midtones");
        public static readonly int _Highlights                     = Shader.PropertyToID("_Highlights");
        public static readonly int _ShaHiLimits                    = Shader.PropertyToID("_ShaHiLimits");
        public static readonly int _SplitShadows                   = Shader.PropertyToID("_SplitShadows");
        public static readonly int _SplitHighlights                = Shader.PropertyToID("_SplitHighlights");
        public static readonly int _CurveMaster                    = Shader.PropertyToID("_CurveMaster");
        public static readonly int _CurveRed                       = Shader.PropertyToID("_CurveRed");
        public static readonly int _CurveGreen                     = Shader.PropertyToID("_CurveGreen");
        public static readonly int _CurveBlue                      = Shader.PropertyToID("_CurveBlue");
        public static readonly int _CurveHueVsHue                  = Shader.PropertyToID("_CurveHueVsHue");
        public static readonly int _CurveHueVsSat                  = Shader.PropertyToID("_CurveHueVsSat");
        public static readonly int _CurveSatVsSat                  = Shader.PropertyToID("_CurveSatVsSat");
        public static readonly int _CurveLumVsSat                  = Shader.PropertyToID("_CurveLumVsSat");

        public static readonly int _CustomToneCurve                = Shader.PropertyToID("_CustomToneCurve");
        public static readonly int _ToeSegmentA                    = Shader.PropertyToID("_ToeSegmentA");
        public static readonly int _ToeSegmentB                    = Shader.PropertyToID("_ToeSegmentB");
        public static readonly int _MidSegmentA                    = Shader.PropertyToID("_MidSegmentA");
        public static readonly int _MidSegmentB                    = Shader.PropertyToID("_MidSegmentB");
        public static readonly int _ShoSegmentA                    = Shader.PropertyToID("_ShoSegmentA");
        public static readonly int _ShoSegmentB                    = Shader.PropertyToID("_ShoSegmentB");

        public static readonly int _Depth                          = Shader.PropertyToID("_Depth");
        public static readonly int _LinearZ                        = Shader.PropertyToID("_LinearZ");
        public static readonly int _DS2x                           = Shader.PropertyToID("_DS2x");
        public static readonly int _DS4x                           = Shader.PropertyToID("_DS4x");
        public static readonly int _DS8x                           = Shader.PropertyToID("_DS8x");
        public static readonly int _DS16x                          = Shader.PropertyToID("_DS16x");
        public static readonly int _DS2xAtlas                      = Shader.PropertyToID("_DS2xAtlas");
        public static readonly int _DS4xAtlas                      = Shader.PropertyToID("_DS4xAtlas");
        public static readonly int _DS8xAtlas                      = Shader.PropertyToID("_DS8xAtlas");
        public static readonly int _DS16xAtlas                     = Shader.PropertyToID("_DS16xAtlas");
        public static readonly int _InvThicknessTable              = Shader.PropertyToID("_InvThicknessTable");
        public static readonly int _SampleWeightTable              = Shader.PropertyToID("_SampleWeightTable");
        public static readonly int _InvSliceDimension              = Shader.PropertyToID("_InvSliceDimension");
        public static readonly int _AdditionalParams               = Shader.PropertyToID("_AdditionalParams");
        public static readonly int _Occlusion                      = Shader.PropertyToID("_Occlusion");
        public static readonly int _InvLowResolution               = Shader.PropertyToID("_InvLowResolution");
        public static readonly int _InvHighResolution              = Shader.PropertyToID("_InvHighResolution");
        public static readonly int _LoResDB                        = Shader.PropertyToID("_LoResDB");
        public static readonly int _HiResDB                        = Shader.PropertyToID("_HiResDB");
        public static readonly int _LoResAO1                       = Shader.PropertyToID("_LoResAO1");
        public static readonly int _HiResAO                        = Shader.PropertyToID("_HiResAO");
        public static readonly int _AoResult                       = Shader.PropertyToID("_AoResult");

        public static readonly int _GrainTexture                   = Shader.PropertyToID("_GrainTexture");
        public static readonly int _GrainParams                    = Shader.PropertyToID("_GrainParams");
        public static readonly int _GrainTextureParams             = Shader.PropertyToID("_GrainTextureParams");
        public static readonly int _BlueNoiseTexture               = Shader.PropertyToID("_BlueNoiseTexture");
        public static readonly int _AlphaTexture                   = Shader.PropertyToID("_AlphaTexture");
        public static readonly int _OwenScrambledRGTexture         = Shader.PropertyToID("_OwenScrambledRGTexture");
        public static readonly int _OwenScrambledTexture           = Shader.PropertyToID("_OwenScrambledTexture");
        public static readonly int _ScramblingTileXSPP             = Shader.PropertyToID("_ScramblingTileXSPP");
        public static readonly int _RankingTileXSPP                = Shader.PropertyToID("_RankingTileXSPP");
        public static readonly int _ScramblingTexture              = Shader.PropertyToID("_ScramblingTexture");
        public static readonly int _AfterPostProcessTexture        = Shader.PropertyToID("_AfterPostProcessTexture");
        public static readonly int _DitherParams                   = Shader.PropertyToID("_DitherParams");
        public static readonly int _KeepAlpha                      = Shader.PropertyToID("_KeepAlpha");
        public static readonly int _UVTransform                    = Shader.PropertyToID("_UVTransform");

        public static readonly int _MotionVecAndDepth              = Shader.PropertyToID("_MotionVecAndDepth");
        public static readonly int _TileMinMaxMotionVec            = Shader.PropertyToID("_TileMinMaxMotionVec");
        public static readonly int _TileMaxNeighbourhood           = Shader.PropertyToID("_TileMaxNeighbourhood");
        public static readonly int _TileToScatterMax               = Shader.PropertyToID("_TileToScatterMax");
        public static readonly int _TileToScatterMin               = Shader.PropertyToID("_TileToScatterMin");
        public static readonly int _TileTargetSize                 = Shader.PropertyToID("_TileTargetSize");
        public static readonly int _MotionBlurParams               = Shader.PropertyToID("_MotionBlurParams0");
        public static readonly int _MotionBlurParams1              = Shader.PropertyToID("_MotionBlurParams1");
        public static readonly int _MotionBlurParams2              = Shader.PropertyToID("_MotionBlurParams2");
        public static readonly int _PrevVPMatrixNoTranslation      = Shader.PropertyToID("_PrevVPMatrixNoTranslation");

        public static readonly int _SMAAAreaTex                    = Shader.PropertyToID("_AreaTex");
        public static readonly int _SMAASearchTex                  = Shader.PropertyToID("_SearchTex");
        public static readonly int _SMAABlendTex                   = Shader.PropertyToID("_BlendTex");
        public static readonly int _SMAARTMetrics                  = Shader.PropertyToID("_SMAARTMetrics");

        public static readonly int _LowResDepthTexture             = Shader.PropertyToID("_LowResDepthTexture");
        public static readonly int _LowResTransparent              = Shader.PropertyToID("_LowResTransparent");

        public static readonly int _AOBufferSize                   = Shader.PropertyToID("_AOBufferSize");
        public static readonly int _AOParams0                      = Shader.PropertyToID("_AOParams0");
        public static readonly int _AOParams1                      = Shader.PropertyToID("_AOParams1");
        public static readonly int _AOParams2                      = Shader.PropertyToID("_AOParams2");
        public static readonly int _AOParams3                      = Shader.PropertyToID("_AOParams3");
        public static readonly int _AOParams4                      = Shader.PropertyToID("_AOParams4");
        public static readonly int _FirstTwoDepthMipOffsets        = Shader.PropertyToID("_FirstTwoDepthMipOffsets");
        public static readonly int _OcclusionTexture               = Shader.PropertyToID("_OcclusionTexture");
        public static readonly int _BentNormalsTexture             = Shader.PropertyToID("_BentNormalsTexture");
        public static readonly int _AOPackedData                   = Shader.PropertyToID("_AOPackedData");
        public static readonly int _AOPackedHistory                = Shader.PropertyToID("_AOPackedHistory");
        public static readonly int _AODepthToViewParams            = Shader.PropertyToID("_AODepthToViewParams");
        public static readonly int _AOPackedBlurred                = Shader.PropertyToID("_AOPackedBlurred");
        public static readonly int _AOOutputHistory                = Shader.PropertyToID("_AOOutputHistory");

        // Contrast Adaptive Sharpening
        public static readonly int _Sharpness                      = Shader.PropertyToID("Sharpness");
        public static readonly int _InputTextureDimensions         = Shader.PropertyToID("InputTextureDimensions");
        public static readonly int _OutputTextureDimensions        = Shader.PropertyToID("OutputTextureDimensions");

        // BlitCubeTextureFace.shader
        public static readonly int _InputTex                       = Shader.PropertyToID("_InputTex");
        public static readonly int _LoD                            = Shader.PropertyToID("_LoD");
        public static readonly int _FaceIndex                      = Shader.PropertyToID("_FaceIndex");
    }

    // Shared material property names
    static class HDMaterialProperties
    {
        // Stencil properties
        public const string kStencilRef = "_StencilRef";
        public const string kStencilWriteMask = "_StencilWriteMask";
        public const string kStencilRefDepth = "_StencilRefDepth";
        public const string kStencilWriteMaskDepth = "_StencilWriteMaskDepth";
        public const string kStencilRefGBuffer = "_StencilRefGBuffer";
        public const string kStencilWriteMaskGBuffer = "_StencilWriteMaskGBuffer";
        public const string kStencilRefMV = "_StencilRefMV";
        public const string kStencilWriteMaskMV = "_StencilWriteMaskMV";
        public const string kStencilRefDistortionVec = "_StencilRefDistortionVec";
        public const string kStencilWriteMaskDistortionVec = "_StencilWriteMaskDistortionVec";
        public const string kUseSplitLighting = "_RequireSplitLighting";

        public const string kZWrite = "_ZWrite";
        public const string kTransparentZWrite = "_TransparentZWrite";
        public const string kTransparentCullMode = "_TransparentCullMode";
        public const string kZTestTransparent = "_ZTestTransparent";

        public const string kEmissiveColorMap = "_EmissiveColorMap";

        public const string kSurfaceType = "_SurfaceType";
        public const string kMaterialID = "_MaterialID";
        public const string kTransmissionEnable = "_TransmissionEnable";
        public const string kEnableDecals = "_SupportDecals";
        public const string kSupportDecals = kEnableDecals;
        public const string kEnableSSR = "_ReceivesSSR";

        public const string kLayerCount = "_LayerCount";

        public const string kAlphaCutoffEnabled = "_AlphaCutoffEnable";
        public const string kZTestGBuffer = "_ZTestGBuffer";
        public const string kZTestDepthEqualForOpaque = "_ZTestDepthEqualForOpaque";
        public const string kBlendMode = "_BlendMode";
        public const string kEnableFogOnTransparent = "_EnableFogOnTransparent";
        public const string kDistortionDepthTest = "_DistortionDepthTest";
        public const string kDistortionEnable = "_DistortionEnable";
        public const string kZTestModeDistortion = "_ZTestModeDistortion";
        public const string kDistortionBlendMode = "_DistortionBlendMode";
        public const string kTransparentWritingMotionVec = "_TransparentWritingMotionVec";
        public const string kEnableBlendModePreserveSpecularLighting = "_EnableBlendModePreserveSpecularLighting";
        public const string kEmissionColor = "_EmissionColor";
        public const string kTransparentBackfaceEnable = "_TransparentBackfaceEnable";
        public const string kDoubleSidedEnable = "_DoubleSidedEnable";
        public const string kDoubleSidedNormalMode = "_DoubleSidedNormalMode";
        public const string kDistortionOnly = "_DistortionOnly";
        public const string kTransparentDepthPrepassEnable = "_TransparentDepthPrepassEnable";
        public const string kTransparentDepthPostpassEnable = "_TransparentDepthPostpassEnable";
        public const string kTransparentSortPriority = "_TransparentSortPriority";

        public const int kMaxLayerCount = 4;

        public const string kUVBase = "_UVBase";
        public const string kTexWorldScale = "_TexWorldScale";
        public const string kUVMappingMask = "_UVMappingMask";
        public const string kUVDetail = "_UVDetail";
        public const string kUVDetailsMappingMask = "_UVDetailsMappingMask";
        public const string kReceivesSSR = "_ReceivesSSR";
        public const string kAddPrecomputedVelocity = "_AddPrecomputedVelocity";
        public const string kShadowMatteFilter = "_ShadowMatteFilter";

        public static readonly Color[] kLayerColors =
        {
            Color.white,
            Color.red,
            Color.green,
            Color.blue
        };
    }
}
