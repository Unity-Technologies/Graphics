namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    static class HDShaderPassNames
    {
        // ShaderPass name
        internal static readonly ShaderPassName s_EmptyName = new ShaderPassName("");
        internal static readonly ShaderPassName m_ForwardName = new ShaderPassName("Forward");
        internal static readonly ShaderPassName m_ForwardDisplayDebugName = new ShaderPassName("ForwardDisplayDebug");
        internal static readonly ShaderPassName m_DepthOnlyName = new ShaderPassName("DepthOnly");
        internal static readonly ShaderPassName m_ForwardOnlyOpaqueDepthOnlyName = new ShaderPassName("ForwardOnlyOpaqueDepthOnly");
        internal static readonly ShaderPassName m_ForwardOnlyOpaqueName = new ShaderPassName("ForwardOnlyOpaque");
        internal static readonly ShaderPassName m_ForwardOnlyOpaqueDisplayDebugName = new ShaderPassName("ForwardOnlyOpaqueDisplayDebug");
        internal static readonly ShaderPassName m_GBufferName = new ShaderPassName("GBuffer");
        internal static readonly ShaderPassName m_GBufferWithPrepassName = new ShaderPassName("GBufferWithPrepass");
        internal static readonly ShaderPassName m_GBufferDebugDisplayName = new ShaderPassName("GBufferDebugDisplay");
        internal static readonly ShaderPassName m_SRPDefaultUnlitName = new ShaderPassName("SRPDefaultUnlit");
        internal static readonly ShaderPassName m_MotionVectorsName = new ShaderPassName("MotionVectors");
        internal static readonly ShaderPassName m_DistortionVectorsName = new ShaderPassName("DistortionVectors");

        // Legacy name
        internal static readonly ShaderPassName m_AlwaysName = new ShaderPassName("Always");
        internal static readonly ShaderPassName m_ForwardBaseName = new ShaderPassName("ForwardBase");
        internal static readonly ShaderPassName m_DeferredName = new ShaderPassName("Deferred");
        internal static readonly ShaderPassName m_PrepassBaseName = new ShaderPassName("PrepassBase");
        internal static readonly ShaderPassName m_VertexName = new ShaderPassName("Vertex");
        internal static readonly ShaderPassName m_VertexLMRGBMName = new ShaderPassName("VertexLMRGBM");
        internal static readonly ShaderPassName m_VertexLMName = new ShaderPassName("VertexLM");
    }
    // Pre-hashed shader ids - naming conventions are a bit off in this file as we use the same
    // fields names as in the shaders for ease of use... Would be nice to clean this up at some
    // point.
    static class HDShaderIDs
    {
        internal static readonly int _ShadowDatasExp = Shader.PropertyToID("_ShadowDatasExp");
        internal static readonly int _ShadowPayloads = Shader.PropertyToID("_ShadowPayloads");
        internal static readonly int _ShadowmapExp_VSM_0 = Shader.PropertyToID("_ShadowmapExp_VSM_0");
        internal static readonly int _ShadowmapExp_VSM_1 = Shader.PropertyToID("_ShadowmapExp_VSM_1");
        internal static readonly int _ShadowmapExp_VSM_2 = Shader.PropertyToID("_ShadowmapExp_VSM_2");
        internal static readonly int _ShadowmapExp_PCF = Shader.PropertyToID("_ShadowmapExp_PCF");

        internal static readonly int g_LayeredSingleIdxBuffer = Shader.PropertyToID("g_LayeredSingleIdxBuffer");
        internal static readonly int _EnvLightIndexShift = Shader.PropertyToID("_EnvLightIndexShift");
        internal static readonly int g_isOrthographic = Shader.PropertyToID("g_isOrthographic");
        internal static readonly int g_iNrVisibLights = Shader.PropertyToID("g_iNrVisibLights");
        internal static readonly int g_mScrProjection = Shader.PropertyToID("g_mScrProjection");
        internal static readonly int g_mInvScrProjection = Shader.PropertyToID("g_mInvScrProjection");
        internal static readonly int g_iLog2NumClusters = Shader.PropertyToID("g_iLog2NumClusters");
        internal static readonly int g_fNearPlane = Shader.PropertyToID("g_fNearPlane");
        internal static readonly int g_fFarPlane = Shader.PropertyToID("g_fFarPlane");
        internal static readonly int g_fClustScale = Shader.PropertyToID("g_fClustScale");
        internal static readonly int g_fClustBase = Shader.PropertyToID("g_fClustBase");
        internal static readonly int g_depth_tex = Shader.PropertyToID("g_depth_tex");
        internal static readonly int g_vLayeredLightList = Shader.PropertyToID("g_vLayeredLightList");
        internal static readonly int g_LayeredOffset = Shader.PropertyToID("g_LayeredOffset");
        internal static readonly int g_vBigTileLightList = Shader.PropertyToID("g_vBigTileLightList");
        internal static readonly int g_logBaseBuffer = Shader.PropertyToID("g_logBaseBuffer");
        internal static readonly int g_vBoundsBuffer = Shader.PropertyToID("g_vBoundsBuffer");
        internal static readonly int _LightVolumeData = Shader.PropertyToID("_LightVolumeData");
        internal static readonly int g_data = Shader.PropertyToID("g_data");
        internal static readonly int g_mProjection = Shader.PropertyToID("g_mProjection");
        internal static readonly int g_mInvProjection = Shader.PropertyToID("g_mInvProjection");
        internal static readonly int g_viDimensions = Shader.PropertyToID("g_viDimensions");
        internal static readonly int g_vLightList = Shader.PropertyToID("g_vLightList");

        internal static readonly int g_BaseFeatureFlags = Shader.PropertyToID("g_BaseFeatureFlags");
        internal static readonly int g_TileFeatureFlags = Shader.PropertyToID("g_TileFeatureFlags");

        internal static readonly int _GBufferTexture0 = Shader.PropertyToID("_GBufferTexture0");
        internal static readonly int _GBufferTexture1 = Shader.PropertyToID("_GBufferTexture1");
        internal static readonly int _GBufferTexture2 = Shader.PropertyToID("_GBufferTexture2");
        internal static readonly int _GBufferTexture3 = Shader.PropertyToID("_GBufferTexture3");

        internal static readonly int g_DispatchIndirectBuffer = Shader.PropertyToID("g_DispatchIndirectBuffer");
        internal static readonly int g_TileList = Shader.PropertyToID("g_TileList");
        internal static readonly int g_NumTiles = Shader.PropertyToID("g_NumTiles");
        internal static readonly int g_NumTilesX = Shader.PropertyToID("g_NumTilesX");

        internal static readonly int _NumTiles = Shader.PropertyToID("_NumTiles");

        internal static readonly int _CookieTextures = Shader.PropertyToID("_CookieTextures");
        internal static readonly int _CookieCubeTextures = Shader.PropertyToID("_CookieCubeTextures");
        internal static readonly int _EnvTextures = Shader.PropertyToID("_EnvTextures");
        internal static readonly int _DirectionalLightDatas = Shader.PropertyToID("_DirectionalLightDatas");
        internal static readonly int _DirectionalLightCount = Shader.PropertyToID("_DirectionalLightCount");
        internal static readonly int _LightDatas = Shader.PropertyToID("_LightDatas");
        internal static readonly int _PunctualLightCount = Shader.PropertyToID("_PunctualLightCount");
        internal static readonly int _AreaLightCount = Shader.PropertyToID("_AreaLightCount");
        internal static readonly int g_vLightListGlobal = Shader.PropertyToID("g_vLightListGlobal");
        internal static readonly int _EnvLightDatas = Shader.PropertyToID("_EnvLightDatas");
        internal static readonly int _EnvLightCount = Shader.PropertyToID("_EnvLightCount");
        internal static readonly int _ShadowDatas = Shader.PropertyToID("_ShadowDatas");
        internal static readonly int _DirShadowSplitSpheres = Shader.PropertyToID("_DirShadowSplitSpheres");
        internal static readonly int _NumTileFtplX = Shader.PropertyToID("_NumTileFtplX");
        internal static readonly int _NumTileFtplY = Shader.PropertyToID("_NumTileFtplY");
        internal static readonly int _NumTileClusteredX = Shader.PropertyToID("_NumTileClusteredX");
        internal static readonly int _NumTileClusteredY = Shader.PropertyToID("_NumTileClusteredY");

        internal static readonly int g_isLogBaseBufferEnabled = Shader.PropertyToID("g_isLogBaseBufferEnabled");
        internal static readonly int g_vLayeredOffsetsBuffer = Shader.PropertyToID("g_vLayeredOffsetsBuffer");

        internal static readonly int _ViewTilesFlags = Shader.PropertyToID("_ViewTilesFlags");
        internal static readonly int _MousePixelCoord = Shader.PropertyToID("_MousePixelCoord");

        internal static readonly int _DebugViewMaterial = Shader.PropertyToID("_DebugViewMaterial");
        internal static readonly int _DebugLightingMode = Shader.PropertyToID("_DebugLightingMode");
        internal static readonly int _DebugLightingAlbedo = Shader.PropertyToID("_DebugLightingAlbedo");
        internal static readonly int _DebugLightingSmoothness = Shader.PropertyToID("_DebugLightingSmoothness");
        internal static readonly int _AmbientOcclusionTexture = Shader.PropertyToID("_AmbientOcclusionTexture");

        internal static readonly int _UseTileLightList = Shader.PropertyToID("_UseTileLightList");
        internal static readonly int _Time = Shader.PropertyToID("_Time");
        internal static readonly int _SinTime = Shader.PropertyToID("_SinTime");
        internal static readonly int _CosTime = Shader.PropertyToID("_CosTime");
        internal static readonly int unity_DeltaTime = Shader.PropertyToID("unity_DeltaTime");
        internal static readonly int _EnvLightSkyEnabled = Shader.PropertyToID("_EnvLightSkyEnabled");
        internal static readonly int _AmbientOcclusionDirectLightStrenght = Shader.PropertyToID("_AmbientOcclusionDirectLightStrenght");
        internal static readonly int _SkyTexture = Shader.PropertyToID("_SkyTexture");

        internal static readonly int _UseDisneySSS = Shader.PropertyToID("_UseDisneySSS");
        internal static readonly int _EnableSSSAndTransmission = Shader.PropertyToID("_EnableSSSAndTransmission");
        internal static readonly int _TexturingModeFlags = Shader.PropertyToID("_TexturingModeFlags");
        internal static readonly int _TransmissionFlags = Shader.PropertyToID("_TransmissionFlags");
        internal static readonly int _ThicknessRemaps = Shader.PropertyToID("_ThicknessRemaps");
        internal static readonly int _ShapeParams = Shader.PropertyToID("_ShapeParams");
        internal static readonly int _HalfRcpVariancesAndWeights = Shader.PropertyToID("_HalfRcpVariancesAndWeights");
        internal static readonly int _TransmissionTints = Shader.PropertyToID("_TransmissionTints");
        internal static readonly int specularLightingUAV = Shader.PropertyToID("specularLightingUAV");
        internal static readonly int diffuseLightingUAV = Shader.PropertyToID("diffuseLightingUAV");

        internal static readonly int g_TileListOffset = Shader.PropertyToID("g_TileListOffset");

        internal static readonly int _LtcData = Shader.PropertyToID("_LtcData");
        internal static readonly int _PreIntegratedFGD = Shader.PropertyToID("_PreIntegratedFGD");
        internal static readonly int _LtcGGXMatrix = Shader.PropertyToID("_LtcGGXMatrix");
        internal static readonly int _LtcDisneyDiffuseMatrix = Shader.PropertyToID("_LtcDisneyDiffuseMatrix");
        internal static readonly int _LtcMultiGGXFresnelDisneyDiffuse = Shader.PropertyToID("_LtcMultiGGXFresnelDisneyDiffuse");

        internal static readonly int _MainDepthTexture = Shader.PropertyToID("_MainDepthTexture");

        internal static readonly int _DeferredShadowTexture = Shader.PropertyToID("_DeferredShadowTexture");
        internal static readonly int _DeferredShadowTextureUAV = Shader.PropertyToID("_DeferredShadowTextureUAV");
        internal static readonly int _DirectionalShadowIndex = Shader.PropertyToID("_DirectionalShadowIndex");

        internal static readonly int unity_OrthoParams = Shader.PropertyToID("unity_OrthoParams");
        internal static readonly int _ZBufferParams = Shader.PropertyToID("_ZBufferParams");
        internal static readonly int _ScreenParams = Shader.PropertyToID("_ScreenParams");
        internal static readonly int _ProjectionParams = Shader.PropertyToID("_ProjectionParams");
        internal static readonly int _WorldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");


        internal static readonly int _StencilRef = Shader.PropertyToID("_StencilRef");
        internal static readonly int _StencilCmp = Shader.PropertyToID("_StencilCmp");

        internal static readonly int _SrcBlend = Shader.PropertyToID("_SrcBlend");
        internal static readonly int _DstBlend = Shader.PropertyToID("_DstBlend");

        internal static readonly int _HTile = Shader.PropertyToID("_HTile");
        internal static readonly int _StencilTexture = Shader.PropertyToID("_StencilTexture");

        internal static readonly int _ViewMatrix = Shader.PropertyToID("_ViewMatrix");
        internal static readonly int _InvViewMatrix = Shader.PropertyToID("_InvViewMatrix");
        internal static readonly int _ProjMatrix = Shader.PropertyToID("_ProjMatrix");
        internal static readonly int _InvProjMatrix = Shader.PropertyToID("_InvProjMatrix");
        internal static readonly int _NonJitteredViewProjMatrix = Shader.PropertyToID("_NonJitteredViewProjMatrix");
        internal static readonly int _ViewProjMatrix = Shader.PropertyToID("_ViewProjMatrix");
        internal static readonly int _InvViewProjMatrix = Shader.PropertyToID("_InvViewProjMatrix");
        internal static readonly int _InvProjParam = Shader.PropertyToID("_InvProjParam");
        internal static readonly int _ScreenSize = Shader.PropertyToID("_ScreenSize");
        internal static readonly int _PrevViewProjMatrix = Shader.PropertyToID("_PrevViewProjMatrix");
        internal static readonly int _FrustumPlanes = Shader.PropertyToID("_FrustumPlanes");

        internal static readonly int _DepthTexture                   = Shader.PropertyToID("_DepthTexture");
        internal static readonly int _CameraColorTexture             = Shader.PropertyToID("_CameraColorTexture");
        internal static readonly int _CameraSssDiffuseLightingBuffer = Shader.PropertyToID("_CameraSssDiffuseLightingTexture");
        internal static readonly int _CameraFilteringBuffer          = Shader.PropertyToID("_CameraFilteringTexture");
        internal static readonly int _IrradianceSource               = Shader.PropertyToID("_IrradianceSource");

        internal static readonly int _VelocityTexture = Shader.PropertyToID("_VelocityTexture");
        internal static readonly int _DistortionTexture = Shader.PropertyToID("_DistortionTexture");
        internal static readonly int _DebugFullScreenTexture = Shader.PropertyToID("_DebugFullScreenTexture");

        internal static readonly int _WorldScales = Shader.PropertyToID("_WorldScales");
        internal static readonly int _FilterKernels = Shader.PropertyToID("_FilterKernels");
        internal static readonly int _FilterKernelsBasic = Shader.PropertyToID("_FilterKernelsBasic");
        internal static readonly int _HalfRcpWeightedVariances = Shader.PropertyToID("_HalfRcpWeightedVariances");

        internal static readonly int _CameraPosDiff = Shader.PropertyToID("_CameraPosDiff");

        internal static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
        internal static readonly int _CameraMotionVectorsTexture = Shader.PropertyToID("_CameraMotionVectorsTexture");
        internal static readonly int _FullScreenDebugMode = Shader.PropertyToID("_FullScreenDebugMode");

        internal static readonly int _InputCubemap = Shader.PropertyToID("_InputCubemap");
        internal static readonly int _Mipmap = Shader.PropertyToID("_Mipmap");


        internal static readonly int _MaxRadius = Shader.PropertyToID("_MaxRadius");
        internal static readonly int _ShapeParam = Shader.PropertyToID("_ShapeParam");
        internal static readonly int _StdDev1 = Shader.PropertyToID("_StdDev1");
        internal static readonly int _StdDev2 = Shader.PropertyToID("_StdDev2");
        internal static readonly int _LerpWeight = Shader.PropertyToID("_LerpWeight");
        internal static readonly int _HalfRcpVarianceAndWeight1 = Shader.PropertyToID("_HalfRcpVarianceAndWeight1");
        internal static readonly int _HalfRcpVarianceAndWeight2 = Shader.PropertyToID("_HalfRcpVarianceAndWeight2");
        internal static readonly int _TransmissionTint = Shader.PropertyToID("_TransmissionTint");
        internal static readonly int _ThicknessRemap = Shader.PropertyToID("_ThicknessRemap");

        internal static readonly int _Cubemap = Shader.PropertyToID("_Cubemap");
        internal static readonly int _SkyParam = Shader.PropertyToID("_SkyParam");
        internal static readonly int _PixelCoordToViewDirWS = Shader.PropertyToID("_PixelCoordToViewDirWS");

        internal static readonly int _GlobalFog_Extinction = Shader.PropertyToID("_GlobalFog_Extinction");
        internal static readonly int _GlobalFog_Asymmetry  = Shader.PropertyToID("_GlobalFog_Asymmetry");
        internal static readonly int _GlobalFog_Scattering = Shader.PropertyToID("_GlobalFog_Scattering");
    }
}
