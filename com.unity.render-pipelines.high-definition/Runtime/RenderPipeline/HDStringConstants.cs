namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Pass names and shader ids used in HDRP. these names can be used as filters when rendering objects in a custom pass or a DrawRenderers() call.</summary>
    public static class HDShaderPassNames
    {
        // ShaderPass string - use to have consistent name through the code
        /// <summary>Empty pass name.</summary>
        public static readonly string s_EmptyStr = "";
        /// <summary>Forward pass name.</summary>
        public static readonly string s_ForwardStr = "Forward";
        /// <summary>Depth Only pass name.</summary>
        public static readonly string s_DepthOnlyStr = "DepthOnly";
        /// <summary>Depth Forward Only pass name.</summary>
        public static readonly string s_DepthForwardOnlyStr = "DepthForwardOnly";
        /// <summary>Forward Only pass name.</summary>
        public static readonly string s_ForwardOnlyStr = "ForwardOnly";
        /// <summary>GBuffer pass name.</summary>
        public static readonly string s_GBufferStr = "GBuffer";
        /// <summary>GBuffer With Prepass pass name.</summary>
        public static readonly string s_GBufferWithPrepassStr = "GBufferWithPrepass";
        /// <summary>Legacy Unlit cross pipeline pass name.</summary>
        public static readonly string s_SRPDefaultUnlitStr = "SRPDefaultUnlit";
        /// <summary>Motion Vectors pass name.</summary>
        public static readonly string s_MotionVectorsStr = "MotionVectors";
        /// <summary>Distortion Vectors pass name.</summary>
        public static readonly string s_DistortionVectorsStr = "DistortionVectors";
        /// <summary>Transparent Depth Prepass pass name.</summary>
        public static readonly string s_TransparentDepthPrepassStr = "TransparentDepthPrepass";
        /// <summary>Transparent Backface pass name.</summary>
        public static readonly string s_TransparentBackfaceStr = "TransparentBackface";
        /// <summary>Transparent Depth Postpass pass name.</summary>
        public static readonly string s_TransparentDepthPostpassStr = "TransparentDepthPostpass";
        /// <summary>RayTracing Prepass pass name.</summary>
        public static readonly string s_RayTracingPrepassStr = "RayTracingPrepass";
        /// <summary>Visibility DXR pass name.</summary>
        public static readonly string s_RayTracingVisibilityStr = "VisibilityDXR";
        /// <summary>PathTracing DXR pass name.</summary>
        public static readonly string s_PathTracingDXRStr = "PathTracingDXR";
        /// <summary>META pass name.</summary>
        public static readonly string s_MetaStr = "META";
        /// <summary>Shadow Caster pass name.</summary>
        public static readonly string s_ShadowCasterStr = "ShadowCaster";
        /// <summary>FullScreen Debug pass name.</summary>
        public static readonly string s_FullScreenDebugStr = "FullScreenDebug";
        /// <summary>DBuffer Projector pass name.</summary>
        public static readonly string s_DBufferProjectorStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferProjector];
        /// <summary>Decal Projector Forward Emissive pass name.</summary>
        public static readonly string s_DecalProjectorForwardEmissiveStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalProjectorForwardEmissive];
        /// <summary>DBuffer Mesh pass name.</summary>
        public static readonly string s_DBufferMeshStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DBufferMesh];
        /// <summary>Decal Mesh Forward Emissive pass name.</summary>
        public static readonly string s_DecalMeshForwardEmissiveStr = DecalSystem.s_MaterialDecalPassNames[(int)DecalSystem.MaterialDecalPass.DecalMeshForwardEmissive];

        // ShaderPass name
        /// <summary>Empty shader tag id.</summary>
        public static readonly ShaderTagId s_EmptyName = new ShaderTagId(s_EmptyStr);
        /// <summary>Forward shader tag id.</summary>
        public static readonly ShaderTagId s_ForwardName = new ShaderTagId(s_ForwardStr);
        /// <summary>Depth Only shader tag id.</summary>
        public static readonly ShaderTagId s_DepthOnlyName = new ShaderTagId(s_DepthOnlyStr);
        /// <summary>Depth Forward Only shader tag id.</summary>
        public static readonly ShaderTagId s_DepthForwardOnlyName = new ShaderTagId(s_DepthForwardOnlyStr);
        /// <summary>Forward Only shader tag id.</summary>
        public static readonly ShaderTagId s_ForwardOnlyName = new ShaderTagId(s_ForwardOnlyStr);
        /// <summary>GBuffer shader tag id.</summary>
        public static readonly ShaderTagId s_GBufferName = new ShaderTagId(s_GBufferStr);
        /// <summary>GBufferWithPrepass shader tag id.</summary>
        public static readonly ShaderTagId s_GBufferWithPrepassName = new ShaderTagId(s_GBufferWithPrepassStr);
        /// <summary>Legacy Unlit cross pipeline shader tag id.</summary>
        public static readonly ShaderTagId s_SRPDefaultUnlitName = new ShaderTagId(s_SRPDefaultUnlitStr);
        /// <summary>Motion Vectors shader tag id.</summary>
        public static readonly ShaderTagId s_MotionVectorsName = new ShaderTagId(s_MotionVectorsStr);
        /// <summary>Distortion Vectors shader tag id.</summary>
        public static readonly ShaderTagId s_DistortionVectorsName = new ShaderTagId(s_DistortionVectorsStr);
        /// <summary>Transparent Depth Prepass shader tag id.</summary>
        public static readonly ShaderTagId s_TransparentDepthPrepassName = new ShaderTagId(s_TransparentDepthPrepassStr);
        /// <summary>Transparent Backface shader tag id.</summary>
        public static readonly ShaderTagId s_TransparentBackfaceName = new ShaderTagId(s_TransparentBackfaceStr);
        /// <summary>Transparent Depth Postpass shader tag id.</summary>
        public static readonly ShaderTagId s_TransparentDepthPostpassName = new ShaderTagId(s_TransparentDepthPostpassStr);
        /// <summary>RayTracing Prepass shader tag id.</summary>
        public static readonly ShaderTagId s_RayTracingPrepassName = new ShaderTagId(s_RayTracingPrepassStr);
        /// <summary>FullScreen Debug shader tag id.</summary>
        public static readonly ShaderTagId s_FullScreenDebugName = new ShaderTagId(s_FullScreenDebugStr);

        /// <summary>DBuffer Mesh shader tag id.</summary>
        public static readonly ShaderTagId s_DBufferMeshName = new ShaderTagId(s_DBufferMeshStr);
        /// <summary>Decal Mesh Forward Emissive shader tag id.</summary>
        public static readonly ShaderTagId s_DecalMeshForwardEmissiveName = new ShaderTagId(s_DecalMeshForwardEmissiveStr);

        // Legacy name
        internal static readonly ShaderTagId s_AlwaysName = new ShaderTagId("Always");
        internal static readonly ShaderTagId s_ForwardBaseName = new ShaderTagId("ForwardBase");
        internal static readonly ShaderTagId s_DeferredName = new ShaderTagId("Deferred");
        internal static readonly ShaderTagId s_PrepassBaseName = new ShaderTagId("PrepassBase");
        internal static readonly ShaderTagId s_VertexName = new ShaderTagId("Vertex");
        internal static readonly ShaderTagId s_VertexLMRGBMName = new ShaderTagId("VertexLMRGBM");
        internal static readonly ShaderTagId s_VertexLMName = new ShaderTagId("VertexLM");
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
        public static readonly int _ShadowmapAreaAtlas = Shader.PropertyToID("_ShadowmapAreaAtlas");
        public static readonly int _ShadowmapCascadeAtlas = Shader.PropertyToID("_ShadowmapCascadeAtlas");

        public static readonly int _CachedShadowmapAtlas = Shader.PropertyToID("_CachedShadowmapAtlas");
        public static readonly int _CachedAreaLightShadowmapAtlas = Shader.PropertyToID("_CachedAreaLightShadowmapAtlas");
        public static readonly int _CachedShadowAtlasSize = Shader.PropertyToID("_CachedShadowAtlasSize");
        public static readonly int _CachedAreaShadowAtlasSize = Shader.PropertyToID("_CachedAreaShadowAtlasSize");

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

        public static readonly int g_depth_tex = Shader.PropertyToID("g_depth_tex");
        public static readonly int g_vLayeredLightList = Shader.PropertyToID("g_vLayeredLightList");
        public static readonly int g_LayeredOffset = Shader.PropertyToID("g_LayeredOffset");
        public static readonly int g_vBigTileLightList = Shader.PropertyToID("g_vBigTileLightList");
        public static readonly int g_vLightListGlobal = Shader.PropertyToID("g_vLightListGlobal");
        public static readonly int g_logBaseBuffer = Shader.PropertyToID("g_logBaseBuffer");
        public static readonly int g_vBoundsBuffer = Shader.PropertyToID("g_vBoundsBuffer");
        public static readonly int _LightVolumeData = Shader.PropertyToID("_LightVolumeData");
        public static readonly int g_data = Shader.PropertyToID("g_data");
        public static readonly int g_vLightList = Shader.PropertyToID("g_vLightList");

        public static readonly int g_TileFeatureFlags = Shader.PropertyToID("g_TileFeatureFlags");

        public static readonly int g_DispatchIndirectBuffer = Shader.PropertyToID("g_DispatchIndirectBuffer");
        public static readonly int g_TileList = Shader.PropertyToID("g_TileList");
        public static readonly int g_NumTiles = Shader.PropertyToID("g_NumTiles");
        public static readonly int g_NumTilesX = Shader.PropertyToID("g_NumTilesX");
        public static readonly int g_VertexPerTile = Shader.PropertyToID("g_VertexPerTile");

        public static readonly int _NumTiles = Shader.PropertyToID("_NumTiles");

        public static readonly int _CookieAtlas = Shader.PropertyToID("_CookieAtlas");

        public static readonly int _EnvCubemapTextures = Shader.PropertyToID("_EnvCubemapTextures");
        public static readonly int _Env2DTextures = Shader.PropertyToID("_Env2DTextures");
        public static readonly int _DirectionalLightDatas = Shader.PropertyToID("_DirectionalLightDatas");
        public static readonly int _LightDatas = Shader.PropertyToID("_LightDatas");
        public static readonly int _EnvLightDatas = Shader.PropertyToID("_EnvLightDatas");

        public static readonly int _ProbeVolumeBounds = Shader.PropertyToID("_ProbeVolumeBounds");
        public static readonly int _ProbeVolumeDatas = Shader.PropertyToID("_ProbeVolumeDatas");

        public static readonly int g_vLayeredOffsetsBuffer = Shader.PropertyToID("g_vLayeredOffsetsBuffer");

        public static readonly int _LightListToClear = Shader.PropertyToID("_LightListToClear");
        public static readonly int _LightListEntriesAndOffset = Shader.PropertyToID("_LightListEntriesAndOffset");

        public static readonly int _ViewTilesFlags = Shader.PropertyToID("_ViewTilesFlags");
        public static readonly int _ClusterDebugMode = Shader.PropertyToID("_ClusterDebugMode");
        public static readonly int _ClusterDebugDistance = Shader.PropertyToID("_ClusterDebugDistance");
        public static readonly int _MousePixelCoord = Shader.PropertyToID("_MousePixelCoord");
        public static readonly int _MouseClickPixelCoord = Shader.PropertyToID("_MouseClickPixelCoord");
        public static readonly int _DebugFont = Shader.PropertyToID("_DebugFont");
        public static readonly int _SliceIndex = Shader.PropertyToID("_SliceIndex");
        public static readonly int _DebugContactShadowLightIndex = Shader.PropertyToID("_DebugContactShadowLightIndex");

        public static readonly int _AmbientOcclusionTexture = Shader.PropertyToID("_AmbientOcclusionTexture");
        public static readonly int _AmbientOcclusionTextureRW = Shader.PropertyToID("_AmbientOcclusionTextureRW");
        public static readonly int _MultiAmbientOcclusionTexture = Shader.PropertyToID("_MultiAmbientOcclusionTexture");
        public static readonly int _DebugDepthPyramidMip = Shader.PropertyToID("_DebugDepthPyramidMip");
        public static readonly int _DebugDepthPyramidOffsets = Shader.PropertyToID("_DebugDepthPyramidOffsets");

        public static readonly int _UseTileLightList = Shader.PropertyToID("_UseTileLightList");

        public static readonly int _SkyTexture = Shader.PropertyToID("_SkyTexture");

        public static readonly int specularLightingUAV = Shader.PropertyToID("specularLightingUAV");
        public static readonly int diffuseLightingUAV = Shader.PropertyToID("diffuseLightingUAV");
        public static readonly int _DiffusionProfileAsset = Shader.PropertyToID("_DiffusionProfileAsset");
        public static readonly int _SssSampleBudget = Shader.PropertyToID("_SssSampleBudget");
        public static readonly int _MaterialID = Shader.PropertyToID("_MaterialID");

        public static readonly int g_TileListOffset = Shader.PropertyToID("g_TileListOffset");

        public static readonly int _LtcData = Shader.PropertyToID("_LtcData");
        public static readonly int _LtcGGXMatrix = Shader.PropertyToID("_LtcGGXMatrix");
        public static readonly int _LtcDisneyDiffuseMatrix = Shader.PropertyToID("_LtcDisneyDiffuseMatrix");
        public static readonly int _LtcMultiGGXFresnelDisneyDiffuse = Shader.PropertyToID("_LtcMultiGGXFresnelDisneyDiffuse");

        public static readonly int _ScreenSpaceShadowsTexture = Shader.PropertyToID("_ScreenSpaceShadowsTexture");
        public static readonly int _ContactShadowTexture = Shader.PropertyToID("_ContactShadowTexture");
        public static readonly int _ContactShadowTextureUAV = Shader.PropertyToID("_ContactShadowTextureUAV");
        public static readonly int _ContactShadowParamsParameters = Shader.PropertyToID("_ContactShadowParamsParameters");
        public static readonly int _ContactShadowParamsParameters2 = Shader.PropertyToID("_ContactShadowParamsParameters2");
        public static readonly int _ContactShadowParamsParameters3 = Shader.PropertyToID("_ContactShadowParamsParameters3");
        public static readonly int _DirectionalContactShadowSampleCount = Shader.PropertyToID("_SampleCount");
        public static readonly int _ShadowFrustumPlanes = Shader.PropertyToID("_ShadowFrustumPlanes");

        public static readonly int _StencilMask = Shader.PropertyToID("_StencilMask");
        public static readonly int _StencilRef = Shader.PropertyToID("_StencilRef");
        public static readonly int _StencilCmp = Shader.PropertyToID("_StencilCmp");

        public static readonly int _InputDepth = Shader.PropertyToID("_InputDepthTexture");

        public static readonly int _ClearColor = Shader.PropertyToID("_ClearColor");
        public static readonly int _SrcBlend = Shader.PropertyToID("_SrcBlend");
        public static readonly int _DstBlend = Shader.PropertyToID("_DstBlend");

        public static readonly int _ColorMaskTransparentVel = Shader.PropertyToID("_ColorMaskTransparentVel");
        public static readonly int _DecalColorMask0 = Shader.PropertyToID(HDMaterialProperties.kDecalColorMask0);
        public static readonly int _DecalColorMask1 = Shader.PropertyToID(HDMaterialProperties.kDecalColorMask1);
        public static readonly int _DecalColorMask2 = Shader.PropertyToID(HDMaterialProperties.kDecalColorMask2);
        public static readonly int _DecalColorMask3 = Shader.PropertyToID(HDMaterialProperties.kDecalColorMask3);

        public static readonly int _StencilTexture = Shader.PropertyToID("_StencilTexture");

        // Used in the stencil resolve pass
        public static readonly int _OutputStencilBuffer = Shader.PropertyToID("_OutputStencilBuffer");
        public static readonly int _CoarseStencilBuffer = Shader.PropertyToID("_CoarseStencilBuffer");
        public static readonly int _CoarseStencilBufferSize = Shader.PropertyToID("_CoarseStencilBufferSize");


        // all decal properties
        public static readonly int _NormalToWorldID = Shader.PropertyToID("_NormalToWorld");
        public static readonly int _DecalAtlas2DID = Shader.PropertyToID("_DecalAtlas2D");
        public static readonly int _DecalHTileTexture = Shader.PropertyToID("_DecalHTileTexture");
        public static readonly int _DecalDatas = Shader.PropertyToID("_DecalDatas");
        public static readonly int _DecalNormalBufferStencilReadMask = Shader.PropertyToID("_DecalNormalBufferStencilReadMask");
        public static readonly int _DecalNormalBufferStencilRef = Shader.PropertyToID("_DecalNormalBufferStencilRef");
        public static readonly int _DecalPrepassTexture = Shader.PropertyToID("_DecalPrepassTexture");
        public static readonly int _DecalPrepassTextureMS = Shader.PropertyToID("_DecalPrepassTextureMS");
        public static readonly int _DrawOrder = Shader.PropertyToID("_DrawOrder");


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
        public static readonly int _HalfScreenSize = Shader.PropertyToID("_HalfScreenSize");
        public static readonly int _ScreenParams = Shader.PropertyToID("_ScreenParams");
        public static readonly int _RTHandleScale = Shader.PropertyToID("_RTHandleScale");
        public static readonly int _RTHandleScaleHistory = Shader.PropertyToID("_RTHandleScaleHistory");
        public static readonly int _PrevViewProjMatrix = Shader.PropertyToID("_PrevViewProjMatrix");
        public static readonly int _PrevInvViewProjMatrix = Shader.PropertyToID("_PrevInvViewProjMatrix");
        public static readonly int _FrustumPlanes = Shader.PropertyToID("_FrustumPlanes");
        public static readonly int _TaaFrameInfo = Shader.PropertyToID("_TaaFrameInfo");
        public static readonly int _TaaJitterStrength = Shader.PropertyToID("_TaaJitterStrength");

        public static readonly int _TaaPostParameters = Shader.PropertyToID("_TaaPostParameters");
        public static readonly int _TaaHistorySize = Shader.PropertyToID("_TaaHistorySize");
        public static readonly int _TaaFilterWeights = Shader.PropertyToID("_TaaFilterWeights");

        public static readonly int _WorldSpaceCameraPos1 = Shader.PropertyToID("_WorldSpaceCameraPos1");
        public static readonly int _ViewMatrix1 = Shader.PropertyToID("_ViewMatrix1");

        public static readonly int _ColorTexture                   = Shader.PropertyToID("_ColorTexture");
        public static readonly int _DepthTexture                   = Shader.PropertyToID("_DepthTexture");
        public static readonly int _DepthValuesTexture             = Shader.PropertyToID("_DepthValuesTexture");
        public static readonly int _CameraColorTexture             = Shader.PropertyToID("_CameraColorTexture");
        public static readonly int _CameraColorTextureRW           = Shader.PropertyToID("_CameraColorTextureRW");
        public static readonly int _CameraSssDiffuseLightingBuffer = Shader.PropertyToID("_CameraSssDiffuseLightingTexture");
        public static readonly int _CameraFilteringBuffer          = Shader.PropertyToID("_CameraFilteringTexture");
        public static readonly int _IrradianceSource               = Shader.PropertyToID("_IrradianceSource");

        // Planar reflection filtering
        public static readonly int _ReflectionColorMipChain = Shader.PropertyToID("_ReflectionColorMipChain");
        public static readonly int _DepthTextureMipChain = Shader.PropertyToID("_DepthTextureMipChain");
        public static readonly int _ReflectionPlaneNormal = Shader.PropertyToID("_ReflectionPlaneNormal");
        public static readonly int _ReflectionPlanePosition = Shader.PropertyToID("_ReflectionPlanePosition");
        public static readonly int _FilteredPlanarReflectionBuffer = Shader.PropertyToID("_FilteredPlanarReflectionBuffer");
        public static readonly int _HalfResReflectionBuffer = Shader.PropertyToID("_HalfResReflectionBuffer");
        public static readonly int _HalfResDepthBuffer = Shader.PropertyToID("_HalfResDepthBuffer");
        public static readonly int _CaptureBaseScreenSize = Shader.PropertyToID("_CaptureBaseScreenSize");
        public static readonly int _CaptureCurrentScreenSize = Shader.PropertyToID("_CaptureCurrentScreenSize");
        public static readonly int _CaptureCameraIVP = Shader.PropertyToID("_CaptureCameraIVP");
        public static readonly int _CaptureCameraPositon = Shader.PropertyToID("_CaptureCameraPositon");
        public static readonly int _SourceMipIndex = Shader.PropertyToID("_SourceMipIndex");
        public static readonly int _MaxMipLevels = Shader.PropertyToID("_MaxMipLevels");
        public static readonly int _ThetaValuesTexture = Shader.PropertyToID("_ThetaValuesTexture");
        public static readonly int _CaptureCameraFOV = Shader.PropertyToID("_CaptureCameraFOV");
        public static readonly int _RTScaleFactor = Shader.PropertyToID("_RTScaleFactor");
        public static readonly int _CaptureCameraVP_NO = Shader.PropertyToID("_CaptureCameraVP_NO");
        public static readonly int _CaptureCameraFarPlane = Shader.PropertyToID("_CaptureCameraFarPlane");
        public static readonly int _DepthTextureOblique = Shader.PropertyToID("_DepthTextureOblique");
        public static readonly int _DepthTextureNonOblique = Shader.PropertyToID("_DepthTextureNonOblique");
        public static readonly int _CaptureCameraIVP_NO = Shader.PropertyToID("_CaptureCameraIVP_NO");

        public static readonly int _Output                         = Shader.PropertyToID("_Output");
        public static readonly int _Input                          = Shader.PropertyToID("_Input");
        public static readonly int _InputVal                       = Shader.PropertyToID("_InputVal");
        public static readonly int _Sizes                          = Shader.PropertyToID("_Sizes");
        public static readonly int _ScaleBias                      = Shader.PropertyToID("_ScaleBias");

        // MSAA shader properties
        public static readonly int _ColorTextureMS = Shader.PropertyToID("_ColorTextureMS");
        public static readonly int _DepthTextureMS = Shader.PropertyToID("_DepthTextureMS");
        public static readonly int _NormalTextureMS = Shader.PropertyToID("_NormalTextureMS");
        public static readonly int _RaytracePrepassBufferMS = Shader.PropertyToID("_RaytracePrepassBufferMS");
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

        public static readonly int _ShaderVariablesGlobal = Shader.PropertyToID("ShaderVariablesGlobal");
        public static readonly int _ShaderVariablesXR = Shader.PropertyToID("ShaderVariablesXR");
        public static readonly int _ShaderVariablesVolumetric = Shader.PropertyToID("ShaderVariablesVolumetric");
        public static readonly int _ShaderVariablesLightList = Shader.PropertyToID("ShaderVariablesLightList");
        public static readonly int _ShaderVariablesRaytracing = Shader.PropertyToID("ShaderVariablesRaytracing");
        public static readonly int _ShaderVariablesRaytracingLightLoop = Shader.PropertyToID("ShaderVariablesRaytracingLightLoop");
        public static readonly int _ShaderVariablesDebugDisplay = Shader.PropertyToID("ShaderVariablesDebugDisplay");

        public static readonly int _SSSBufferTexture = Shader.PropertyToID("_SSSBufferTexture");
        public static readonly int _NormalBufferTexture = Shader.PropertyToID("_NormalBufferTexture");
        public static readonly int _RaytracePrepassBufferTexture = Shader.PropertyToID("_RaytracePrepassBufferTexture");

        public static readonly int _ShaderVariablesScreenSpaceReflection    = Shader.PropertyToID("ShaderVariablesScreenSpaceReflection");
        public static readonly int _SsrLightingTexture                      = Shader.PropertyToID("_SsrLightingTexture");
        public static readonly int _SsrAccumPrev                            = Shader.PropertyToID("_SsrAccumPrev");
        public static readonly int _SsrLightingTextureRW                    = Shader.PropertyToID("_SsrLightingTextureRW");
        public static readonly int _DirectionPDFTexture                     = Shader.PropertyToID("_DirectionPDFTexture");
        public static readonly int _SSRAccumTexture                         = Shader.PropertyToID("_SSRAccumTexture");
        public static readonly int _SsrHitPointTexture                      = Shader.PropertyToID("_SsrHitPointTexture");
        public static readonly int _SsrClearCoatMaskTexture                 = Shader.PropertyToID("_SsrClearCoatMaskTexture");
        public static readonly int _DepthPyramidMipLevelOffsets             = Shader.PropertyToID("_DepthPyramidMipLevelOffsets");
        public static readonly int _DepthPyramidFirstMipLevelOffset         = Shader.PropertyToID("_DepthPyramidFirstMipLevelOffset");


        // Still used by ray tracing.
        public static readonly int _SsrStencilBit = Shader.PropertyToID("_SsrStencilBit");

        public static readonly int _ShadowMaskTexture = Shader.PropertyToID("_ShadowMaskTexture");
        public static readonly int _LightLayersTexture = Shader.PropertyToID("_LightLayersTexture");
        public static readonly int _DistortionTexture = Shader.PropertyToID("_DistortionTexture");
        public static readonly int _ColorPyramidTexture = Shader.PropertyToID("_ColorPyramidTexture");
        public static readonly int _ColorPyramidUvScaleAndLimitPrevFrame = Shader.PropertyToID("_ColorPyramidUvScaleAndLimitPrevFrame");
        public static readonly int _RoughDistortion = Shader.PropertyToID("_RoughDistortion");

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
        public static readonly int _BlitTextureMSAA = Shader.PropertyToID("_BlitTextureMSAA");
        public static readonly int _BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
        public static readonly int _BlitMipLevel = Shader.PropertyToID("_BlitMipLevel");
        public static readonly int _BlitScaleBiasRt = Shader.PropertyToID("_BlitScaleBiasRt");
        public static readonly int _BlitTextureSize = Shader.PropertyToID("_BlitTextureSize");
        public static readonly int _BlitPaddingSize = Shader.PropertyToID("_BlitPaddingSize");
        public static readonly int _BlitTexArraySlice = Shader.PropertyToID("_BlitTexArraySlice");

        public static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
        public static readonly int _CameraMotionVectorsTexture = Shader.PropertyToID("_CameraMotionVectorsTexture");
        public static readonly int _FullScreenDebugMode = Shader.PropertyToID("_FullScreenDebugMode");
        public static readonly int _FullScreenDebugDepthRemap = Shader.PropertyToID("_FullScreenDebugDepthRemap");
        public static readonly int _TransparencyOverdrawMaxPixelCost = Shader.PropertyToID("_TransparencyOverdrawMaxPixelCost");
        public static readonly int _QuadOverdrawMaxQuadCost = Shader.PropertyToID("_QuadOverdrawMaxQuadCost");
        public static readonly int _VertexDensityMaxPixelCost = Shader.PropertyToID("_VertexDensityMaxPixelCost");
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
        public static readonly int _DistortionParam = Shader.PropertyToID("_DistortionParam");
        public static readonly int _SkyParam = Shader.PropertyToID("_SkyParam");
        public static readonly int _BackplateParameters0 = Shader.PropertyToID("_BackplateParameters0");
        public static readonly int _BackplateParameters1 = Shader.PropertyToID("_BackplateParameters1");
        public static readonly int _BackplateParameters2 = Shader.PropertyToID("_BackplateParameters2");
        public static readonly int _BackplateShadowTint = Shader.PropertyToID("_BackplateShadowTint");
        public static readonly int _BackplateShadowFilter = Shader.PropertyToID("_BackplateShadowFilter");
        public static readonly int _SkyIntensity = Shader.PropertyToID("_SkyIntensity");
        public static readonly int _PixelCoordToViewDirWS = Shader.PropertyToID("_PixelCoordToViewDirWS");

        public static readonly int _Flowmap = Shader.PropertyToID("_Flowmap");
        public static readonly int _FlowmapParam = Shader.PropertyToID("_FlowmapParam");

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

        public static readonly int _VBufferDensity                    = Shader.PropertyToID("_VBufferDensity");
        public static readonly int _VBufferLighting                   = Shader.PropertyToID("_VBufferLighting");
        public static readonly int _VBufferHistory                    = Shader.PropertyToID("_VBufferHistory");
        public static readonly int _VBufferFeedback                   = Shader.PropertyToID("_VBufferFeedback");
        public static readonly int _VolumeBounds                      = Shader.PropertyToID("_VolumeBounds");
        public static readonly int _VolumeData                        = Shader.PropertyToID("_VolumeData");
        public static readonly int _VolumeMaskAtlas                   = Shader.PropertyToID("_VolumeMaskAtlas");

        public static readonly int _MaxZMaskTexture                   = Shader.PropertyToID("_MaxZMaskTexture");
        public static readonly int _DilationWidth                     = Shader.PropertyToID("_DilationWidth");

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
        public static readonly int _RayTracingLayerMask             = Shader.PropertyToID("_RayTracingLayerMask");
        public static readonly int _PixelSpreadAngleTangent         = Shader.PropertyToID("_PixelSpreadAngleTangent");
        public static readonly string _RaytracingAccelerationStructureName          = "_RaytracingAccelerationStructure";

        // Path tracing variables
        public static readonly int _PathTracedDoFConstants          = Shader.PropertyToID("_PathTracedDoFConstants");
        public static readonly int _InvViewportScaleBias            = Shader.PropertyToID("_InvViewportScaleBias");

        // Light Cluster
        public static readonly int _LightDatasRT                    = Shader.PropertyToID("_LightDatasRT");
        public static readonly int _EnvLightDatasRT                 = Shader.PropertyToID("_EnvLightDatasRT");
        public static readonly int _RaytracingLightCluster          = Shader.PropertyToID("_RaytracingLightCluster");
        public static readonly int _RaytracingLightClusterRW        = Shader.PropertyToID("_RaytracingLightClusterRW");

        // Denoising
        public static readonly int _HistoryBuffer                   = Shader.PropertyToID("_HistoryBuffer");
        public static readonly int _HistoryBuffer0                  = Shader.PropertyToID("_HistoryBuffer0");
        public static readonly int _HistoryBuffer1                  = Shader.PropertyToID("_HistoryBuffer1");
        public static readonly int _ValidationBuffer                = Shader.PropertyToID("_ValidationBuffer");
        public static readonly int _ValidationBufferRW              = Shader.PropertyToID("_ValidationBufferRW");
        public static readonly int _HistoryDepthTexture             = Shader.PropertyToID("_HistoryDepthTexture");
        public static readonly int _HistoryNormalTexture            = Shader.PropertyToID("_HistoryNormalTexture");
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
        public static readonly int _ShadowFilterMapping             = Shader.PropertyToID("_ShadowFilterMapping");
        public static readonly int _DistanceTexture                 = Shader.PropertyToID("_DistanceTexture");
        public static readonly int _JitterFramePeriod               = Shader.PropertyToID("_JitterFramePeriod");
        public static readonly int _SingleReflectionBounce          = Shader.PropertyToID("_SingleReflectionBounce");
        public static readonly int _HistoryBufferSize               = Shader.PropertyToID("_HistoryBufferSize");
        public static readonly int _CurrentEffectResolution         = Shader.PropertyToID("_CurrentEffectResolution");
        public static readonly int _SampleCountTextureRW            = Shader.PropertyToID("_SampleCountTextureRW");
        public static readonly int _AffectSmoothSurfaces            = Shader.PropertyToID("_AffectSmoothSurfaces");
        public static readonly int _ObjectMotionStencilBit          = Shader.PropertyToID("_ObjectMotionStencilBit");

        // Reflections
        public static readonly int _ReflectionHistorybufferRW       = Shader.PropertyToID("_ReflectionHistorybufferRW");
        public static readonly int _CurrentFrameTexture             = Shader.PropertyToID("_CurrentFrameTexture");
        public static readonly int _AccumulatedFrameTexture         = Shader.PropertyToID("_AccumulatedFrameTexture");
        public static readonly int _TemporalAccumuationWeight       = Shader.PropertyToID("_TemporalAccumuationWeight");
        public static readonly int _SpatialFilterRadius             = Shader.PropertyToID("_SpatialFilterRadius");
        public static readonly int _RaytracingHitDistanceTexture    = Shader.PropertyToID("_RaytracingHitDistanceTexture");
        public static readonly int _RaytracingVSNormalTexture       = Shader.PropertyToID("_RaytracingVSNormalTexture");
        public static readonly int _RaytracingReflectionTexture     = Shader.PropertyToID("_RaytracingReflectionTexture");

        // Shadows
        public static readonly int _RaytracingTargetAreaLight       = Shader.PropertyToID("_RaytracingTargetAreaLight");
        public static readonly int _RaytracingShadowSlot            = Shader.PropertyToID("_RaytracingShadowSlot");
        public static readonly int _RaytracingChannelMask           = Shader.PropertyToID("_RaytracingChannelMask");
        public static readonly int _RaytracingChannelMask0          = Shader.PropertyToID("_RaytracingChannelMask0");
        public static readonly int _RaytracingChannelMask1          = Shader.PropertyToID("_RaytracingChannelMask1");
        public static readonly int _RaytracingAreaWorldToLocal      = Shader.PropertyToID("_RaytracingAreaWorldToLocal");
        public static readonly int _RaytracedAreaShadowSample       = Shader.PropertyToID("_RaytracedAreaShadowSample");
        public static readonly int _RaytracedAreaShadowIntegration = Shader.PropertyToID("_RaytracedAreaShadowIntegration");
        public static readonly int _RaytracingDirectionBuffer       = Shader.PropertyToID("_RaytracingDirectionBuffer");
        public static readonly int _RayTracingLengthBuffer          = Shader.PropertyToID("_RayTracingLengthBuffer");
        public static readonly int _RaytracingDistanceBufferRW      = Shader.PropertyToID("_RaytracingDistanceBufferRW");
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

        public static readonly int _DirectionalLightAngle           = Shader.PropertyToID("_DirectionalLightAngle");
        public static readonly int _DirectionalMaxRayLength         = Shader.PropertyToID("_DirectionalMaxRayLength");
        public static readonly int _DirectionalLightDirection       = Shader.PropertyToID("_DirectionalLightDirection");
        public static readonly int _SphereLightPosition             = Shader.PropertyToID("_SphereLightPosition");
        public static readonly int _SphereLightRadius               = Shader.PropertyToID("_SphereLightRadius");
        public static readonly int _CameraFOV                       = Shader.PropertyToID("_CameraFOV");

        // Ambient occlusion
        public static readonly int _RaytracingAOIntensity           = Shader.PropertyToID("_RaytracingAOIntensity");

        // Ray count
        public static readonly int _RayCountTexture                 = Shader.PropertyToID("_RayCountTexture");
        public static readonly int _RayCountType                    = Shader.PropertyToID("_RayCountType");
        public static readonly int _InputRayCountTexture            = Shader.PropertyToID("_InputRayCountTexture");
        public static readonly int _InputRayCountBuffer             = Shader.PropertyToID("_InputRayCountBuffer");
        public static readonly int _OutputRayCountBuffer            = Shader.PropertyToID("_OutputRayCountBuffer");
        public static readonly int _InputBufferDimension            = Shader.PropertyToID("_InputBufferDimension");
        public static readonly int _OutputBufferDimension           = Shader.PropertyToID("_OutputBufferDimension");

        // Primary Visibility
        public static readonly int _RaytracingFlagMask              = Shader.PropertyToID("_RaytracingFlagMask");
        public static readonly int _RaytracingPrimaryDebug          = Shader.PropertyToID("_RaytracingPrimaryDebug");
        public static readonly int _RaytracingCameraSkyEnabled      = Shader.PropertyToID("_RaytracingCameraSkyEnabled");
        public static readonly int _RaytracingCameraClearColor      = Shader.PropertyToID("_RaytracingCameraClearColor");

        // Indirect diffuse
        public static readonly int _IndirectDiffuseTexture              = Shader.PropertyToID("_IndirectDiffuseTexture");
        public static readonly int _IndirectDiffuseTextureRW            = Shader.PropertyToID("_IndirectDiffuseTextureRW");
        public static readonly int _IndirectDiffuseTexture0RW           = Shader.PropertyToID("_IndirectDiffuseTexture0RW");
        public static readonly int _IndirectDiffuseTexture1RW           = Shader.PropertyToID("_IndirectDiffuseTexture1RW");
        public static readonly int _IndirectDiffuseTexture0             = Shader.PropertyToID("_IndirectDiffuseTexture0");
        public static readonly int _IndirectDiffuseTexture1             = Shader.PropertyToID("_IndirectDiffuseTexture1");
        public static readonly int _UpscaledIndirectDiffuseTextureRW    = Shader.PropertyToID("_UpscaledIndirectDiffuseTextureRW");
        public static readonly int _IndirectDiffuseHitPointTexture      = Shader.PropertyToID("_IndirectDiffuseHitPointTexture");
        public static readonly int _IndirectDiffuseHitPointTextureRW    = Shader.PropertyToID("_IndirectDiffuseHitPointTextureRW");
        public static readonly int _IndirectDiffuseThicknessScale       = Shader.PropertyToID("_IndirectDiffuseThicknessScale");
        public static readonly int _IndirectDiffuseThicknessBias        = Shader.PropertyToID("_IndirectDiffuseThicknessBias");
        public static readonly int _IndirectDiffuseSteps                = Shader.PropertyToID("_IndirectDiffuseSteps");
        public static readonly int _InputNoisyBuffer                    = Shader.PropertyToID("_InputNoisyBuffer");
        public static readonly int _InputNoisyBuffer0                   = Shader.PropertyToID("_InputNoisyBuffer0");
        public static readonly int _InputNoisyBuffer1                   = Shader.PropertyToID("_InputNoisyBuffer1");
        public static readonly int _OutputFilteredBuffer                = Shader.PropertyToID("_OutputFilteredBuffer");
        public static readonly int _OutputFilteredBuffer0               = Shader.PropertyToID("_OutputFilteredBuffer0");
        public static readonly int _OutputFilteredBuffer1               = Shader.PropertyToID("_OutputFilteredBuffer1");
        public static readonly int _LowResolutionTexture                = Shader.PropertyToID("_LowResolutionTexture");
        public static readonly int _OutputUpscaledTexture               = Shader.PropertyToID("_OutputUpscaledTexture");
        public static readonly int _IndirectDiffuseSpatialFilter        = Shader.PropertyToID("_IndirectDiffuseSpatialFilter");

        // Deferred Lighting
        public static readonly int _RaytracingLitBufferRW           = Shader.PropertyToID("_RaytracingLitBufferRW");
        public static readonly int _RayTracingDiffuseLightingOnly   = Shader.PropertyToID("_RayTracingDiffuseLightingOnly");
        public static readonly int _RaytracingHalfResolution        = Shader.PropertyToID("_RaytracingHalfResolution");

        // Ray binning
        public static readonly int _RayBinResult                    = Shader.PropertyToID("_RayBinResult");
        public static readonly int _RayBinSizeResult                = Shader.PropertyToID("_RayBinSizeResult");
        public static readonly int _RayBinTileCountX                = Shader.PropertyToID("_RayBinTileCountX");
        public static readonly int _BufferSizeX                     = Shader.PropertyToID("_BufferSizeX");

        // Sub Surface
        public static readonly int _ThroughputTextureRW             = Shader.PropertyToID("_ThroughputTextureRW");
        public static readonly int _NormalTextureRW                 = Shader.PropertyToID("_NormalTextureRW");
        public static readonly int _DirectionTextureRW              = Shader.PropertyToID("_DirectionTextureRW");
        public static readonly int _PositionTextureRW               = Shader.PropertyToID("_PositionTextureRW");
        public static readonly int _DiffuseLightingTextureRW        = Shader.PropertyToID("_DiffuseLightingTextureRW");
        public static readonly int _SubSurfaceLightingBuffer        = Shader.PropertyToID("_SubSurfaceLightingBuffer");
        public static readonly int _IndirectDiffuseLightingBuffer   = Shader.PropertyToID("_IndirectDiffuseLightingBuffer");

        // Accumulation
        public static readonly int _AccumulationFrameIndex          = Shader.PropertyToID("_AccumulationFrameIndex");
        public static readonly int _AccumulationNumSamples          = Shader.PropertyToID("_AccumulationNumSamples");
        public static readonly int _AccumulationWeights             = Shader.PropertyToID("_AccumulationWeights");
        public static readonly int _AccumulationNeedsExposure       = Shader.PropertyToID("_AccumulationNeedsExposure");
        public static readonly int _RadianceTexture                 = Shader.PropertyToID("_RadianceTexture");

        // Preintegrated texture name
        public static readonly int _PreIntegratedFGD_GGXDisneyDiffuse = Shader.PropertyToID("_PreIntegratedFGD_GGXDisneyDiffuse");
        public static readonly int _PreIntegratedFGD_CharlieAndFabric = Shader.PropertyToID("_PreIntegratedFGD_CharlieAndFabric");

        public static readonly int _ExposureTexture                = Shader.PropertyToID("_ExposureTexture");
        public static readonly int _PrevExposureTexture            = Shader.PropertyToID("_PrevExposureTexture");
        public static readonly int _PreviousExposureTexture        = Shader.PropertyToID("_PreviousExposureTexture");
        public static readonly int _ExposureDebugTexture           = Shader.PropertyToID("_ExposureDebugTexture");
        public static readonly int _ExposureParams                 = Shader.PropertyToID("_ExposureParams");
        public static readonly int _ExposureParams2                = Shader.PropertyToID("_ExposureParams2");
        public static readonly int _ExposureDebugParams            = Shader.PropertyToID("_ExposureDebugParams");
        public static readonly int _HistogramExposureParams        = Shader.PropertyToID("_HistogramExposureParams");
        public static readonly int _HistogramBuffer                = Shader.PropertyToID("_HistogramBuffer");
        public static readonly int _FullImageHistogram             = Shader.PropertyToID("_FullImageHistogram");
        public static readonly int _AdaptationParams               = Shader.PropertyToID("_AdaptationParams");
        public static readonly int _ExposureCurveTexture           = Shader.PropertyToID("_ExposureCurveTexture");
        public static readonly int _ExposureWeightMask             = Shader.PropertyToID("_ExposureWeightMask");
        public static readonly int _ProceduralMaskParams           = Shader.PropertyToID("_ProceduralMaskParams");
        public static readonly int _ProceduralMaskParams2          = Shader.PropertyToID("_ProceduralMaskParams2");
        public static readonly int _Variants                       = Shader.PropertyToID("_Variants");
        public static readonly int _InputTexture                   = Shader.PropertyToID("_InputTexture");
        public static readonly int _InputTextureMSAA               = Shader.PropertyToID("_InputTextureMSAA");
        public static readonly int _OutputTexture                  = Shader.PropertyToID("_OutputTexture");
        public static readonly int _SourceTexture                  = Shader.PropertyToID("_SourceTexture");
        public static readonly int _InputHistoryTexture            = Shader.PropertyToID("_InputHistoryTexture");
        public static readonly int _OutputHistoryTexture           = Shader.PropertyToID("_OutputHistoryTexture");
        public static readonly int _InputVelocityMagnitudeHistory  = Shader.PropertyToID("_InputVelocityMagnitudeHistory");
        public static readonly int _OutputVelocityMagnitudeHistory = Shader.PropertyToID("_OutputVelocityMagnitudeHistory");

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
        public static readonly int _OutputMip5                     = Shader.PropertyToID("_OutputMip5");
        public static readonly int _OutputMip6                     = Shader.PropertyToID("_OutputMip6");
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

        public static readonly int _AlphaScaleBias                 = Shader.PropertyToID("_AlphaScaleBias");

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
        public static readonly int _MotionBlurParams3              = Shader.PropertyToID("_MotionBlurParams3");
        public static readonly int _PrevVPMatrixNoTranslation      = Shader.PropertyToID("_PrevVPMatrixNoTranslation");
        public static readonly int _CurrVPMatrixNoTranslation      = Shader.PropertyToID("_CurrVPMatrixNoTranslation");

        public static readonly int _SMAAAreaTex                    = Shader.PropertyToID("_AreaTex");
        public static readonly int _SMAASearchTex                  = Shader.PropertyToID("_SearchTex");
        public static readonly int _SMAABlendTex                   = Shader.PropertyToID("_BlendTex");
        public static readonly int _SMAARTMetrics                  = Shader.PropertyToID("_SMAARTMetrics");

        public static readonly int _LowResDepthTexture             = Shader.PropertyToID("_LowResDepthTexture");
        public static readonly int _LowResTransparent              = Shader.PropertyToID("_LowResTransparent");

        public static readonly int _ShaderVariablesAmbientOcclusion = Shader.PropertyToID("ShaderVariablesAmbientOcclusion");
        public static readonly int _OcclusionTexture               = Shader.PropertyToID("_OcclusionTexture");
        public static readonly int _BentNormalsTexture             = Shader.PropertyToID("_BentNormalsTexture");
        public static readonly int _AOPackedData                   = Shader.PropertyToID("_AOPackedData");
        public static readonly int _AOPackedHistory                = Shader.PropertyToID("_AOPackedHistory");
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

        // Custom Pass Utils API
        public static readonly int _SourceScaleBias                = Shader.PropertyToID("_SourceScaleBias");
        public static readonly int _GaussianWeights                = Shader.PropertyToID("_GaussianWeights");
        public static readonly int _SampleCount                    = Shader.PropertyToID("_SampleCount");
        public static readonly int _Radius                         = Shader.PropertyToID("_Radius");
        public static readonly int _ViewPortSize                   = Shader.PropertyToID("_ViewPortSize");
        public static readonly int _ViewportScaleBias              = Shader.PropertyToID("_ViewportScaleBias");
        public static readonly int _SourceSize                     = Shader.PropertyToID("_SourceSize");
        public static readonly int _SourceScaleFactor              = Shader.PropertyToID("_SourceScaleFactor");

        // Probe Volumes
        public static readonly int _ProbeVolumeAtlasSH             = Shader.PropertyToID("_ProbeVolumeAtlasSH");
        public static readonly int _ProbeVolumeAtlasResolutionAndSliceCount = Shader.PropertyToID("_ProbeVolumeAtlasResolutionAndSliceCount");
        public static readonly int _ProbeVolumeAtlasResolutionAndSliceCountInverse = Shader.PropertyToID("_ProbeVolumeAtlasResolutionAndSliceCountInverse");
        public static readonly int _ProbeVolumeAtlasOctahedralDepth = Shader.PropertyToID("_ProbeVolumeAtlasOctahedralDepth");
        public static readonly int _ProbeVolumeResolution          = Shader.PropertyToID("_ProbeVolumeResolution");
        public static readonly int _ProbeVolumeResolutionInverse   = Shader.PropertyToID("_ProbeVolumeResolutionInverse");
        public static readonly int _ProbeVolumeAtlasScale          = Shader.PropertyToID("_ProbeVolumeAtlasScale");
        public static readonly int _ProbeVolumeAtlasBias           = Shader.PropertyToID("_ProbeVolumeAtlasBias");
        public static readonly int _ProbeVolumeAtlasReadBufferCount = Shader.PropertyToID("_ProbeVolumeAtlasReadBufferCount");
        public static readonly int _ProbeVolumeAtlasReadSHL01Buffer = Shader.PropertyToID("_ProbeVolumeAtlasReadSHL01Buffer");
        public static readonly int _ProbeVolumeAtlasReadSHL2Buffer = Shader.PropertyToID("_ProbeVolumeAtlasReadSHL2Buffer");
        public static readonly int _ProbeVolumeAtlasReadValidityBuffer = Shader.PropertyToID("_ProbeVolumeAtlasReadValidityBuffer");
        public static readonly int _ProbeVolumeAtlasWriteTextureSH = Shader.PropertyToID("_ProbeVolumeAtlasWriteTextureSH");
        public static readonly int _ProbeVolumeAtlasOctahedralDepthScaleBias = Shader.PropertyToID("_ProbeVolumeAtlasOctahedralDepthScaleBias");
        public static readonly int _ProbeVolumeAtlasOctahedralDepthResolutionAndInverse = Shader.PropertyToID("_ProbeVolumeAtlasOctahedralDepthResolutionAndInverse");
        public static readonly int _ProbeVolumeAtlasOctahedralDepthReadBufferCount = Shader.PropertyToID("_ProbeVolumeAtlasOctahedralDepthReadBufferCount");
        public static readonly int _ProbeVolumeAtlasOctahedralDepthReadBuffer = Shader.PropertyToID("_ProbeVolumeAtlasOctahedralDepthReadBuffer");
        public static readonly int _ProbeVolumeAtlasOctahedralDepthWriteTexture = Shader.PropertyToID("_ProbeVolumeAtlasOctahedralDepthWriteTexture");
        public static readonly int _ProbeVolumeAtlasOctahedralDepthScaleBiasTexels = Shader.PropertyToID("_ProbeVolumeAtlasOctahedralDepthScaleBiasTexels");
        public static readonly int _ProbeVolumeAtlasOctahedralDepthRWTexture = Shader.PropertyToID("_ProbeVolumeAtlasOctahedralDepthRWTexture");
        public static readonly int _FilterSampleCount              = Shader.PropertyToID("_FilterSampleCount");
        public static readonly int _FilterSharpness                = Shader.PropertyToID("_FilterSharpness");
        public static readonly int _AtlasTextureSH                 = Shader.PropertyToID("_AtlasTextureSH");
        public static readonly int _TextureViewScale               = Shader.PropertyToID("_TextureViewScale");
        public static readonly int _TextureViewBias                = Shader.PropertyToID("_TextureViewBias");
        public static readonly int _TextureViewResolution          = Shader.PropertyToID("_TextureViewResolution");
        public static readonly int _AtlasTextureOctahedralDepth    = Shader.PropertyToID("_AtlasTextureOctahedralDepth");
        public static readonly int _AtlasTextureOctahedralDepthScaleBias = Shader.PropertyToID("_AtlasTextureOctahedralDepthScaleBias");
        public static readonly int _ValidRange                     = Shader.PropertyToID("_ValidRange");
        public static readonly int _ProbeVolumeAtlasSliceMode      = Shader.PropertyToID("_ProbeVolumeAtlasSliceMode");
    }

    /// <summary>
    /// Material property names used in HDRP Shaders.
    /// </summary>
    public static class HDMaterialProperties
    {
        /// <summary>Depth Write.</summary>
        public const string kZWrite = "_ZWrite";
        /// <summary>Depth Write for Transparent Materials.</summary>
        public const string kTransparentZWrite = "_TransparentZWrite";
        /// <summary>Cull Mode for Transparent Materials.</summary>
        public const string kTransparentCullMode = "_TransparentCullMode";
        /// <summary>Cull Mode for Opaque Materials.</summary>
        public const string kOpaqueCullMode = "_OpaqueCullMode";
        /// <summary>Depth Test for Transparent Materials.</summary>
        public const string kZTestTransparent = "_ZTestTransparent";
        /// <summary>Is Raytracing supported.</summary>
        public const string kRayTracing = "_RayTracing";

        /// <summary>Surface Type.</summary>
        public const string kSurfaceType = "_SurfaceType";
        /// <summary>Receive Decals.</summary>
        public const string kSupportDecals = kEnableDecals;

        /// <summary>Enable Alpha Cutoff.</summary>
        public const string kAlphaCutoffEnabled = "_AlphaCutoffEnable";
        /// <summary>Blend Mode.</summary>
        public const string kBlendMode = "_BlendMode";
        /// <summary>Enable Alpha to Mask.</summary>
        public const string kAlphaToMask = "_AlphaToMask";
        /// <summary>Enable Fog on Transparent Materials.</summary>
        public const string kEnableFogOnTransparent = "_EnableFogOnTransparent";
        /// <summary>Enable Depth Test for distortion.</summary>
        internal const string kDistortionDepthTest = "_DistortionDepthTest";
        /// <summary>Enable distortion.</summary>
        public const string kDistortionEnable = "_DistortionEnable";
        /// <summary>Depth Test for distortion.</summary>
        public const string kZTestModeDistortion = "_ZTestModeDistortion";
        /// <summary>Blend Mode for distortion.</summary>
        public const string kDistortionBlendMode = "_DistortionBlendMode";
        /// <summary>Transparent Material Writes Motion Vectors.</summary>
        public const string kTransparentWritingMotionVec = "_TransparentWritingMotionVec";
        /// <summary>Enable Preserve Specular Lighting.</summary>
        public const string kEnableBlendModePreserveSpecularLighting = "_EnableBlendModePreserveSpecularLighting";
        /// <summary>Enable Back then Front rendering.</summary>
        public const string kTransparentBackfaceEnable = "_TransparentBackfaceEnable";
        /// <summary>Enable double sided.</summary>
        public const string kDoubleSidedEnable = "_DoubleSidedEnable";
        /// <summary>Double sided normal mode.</summary>
        public const string kDoubleSidedNormalMode = "_DoubleSidedNormalMode";
        /// <summary>Enable distortion only (for Unlit).</summary>
        public const string kDistortionOnly = "_DistortionOnly";
        /// <summary>Enable Depth Prepass.</summary>
        public const string kTransparentDepthPrepassEnable = "_TransparentDepthPrepassEnable";
        /// <summary>Enable Depth PostPass.</summary>
        public const string kTransparentDepthPostpassEnable = "_TransparentDepthPostpassEnable";
        /// <summary>Transparent material sorting priority.</summary>
        public const string kTransparentSortPriority = "_TransparentSortPriority";

        /// <summary>Receive SSR.</summary>
        public const string kReceivesSSR = "_ReceivesSSR";
        /// <summary>Receive SSR for Transparent materials.</summary>
        public const string kReceivesSSRTransparent = "_ReceivesSSRTransparent";
        /// <summary>Enable Depth Offset.</summary>
        public const string kDepthOffsetEnable = "_DepthOffsetEnable";

        /// <summary>Enable affect Albedo (decal only).</summary>
        public const string kAffectAlbedo   = "_AffectAlbedo";
        /// <summary>Enable affect Normal (decal only.</summary>
        public const string kAffectNormal   = "_AffectNormal";
        /// <summary>Enable affect AO (decal only.</summary>
        public const string kAffectAO = "_AffectAO";
        /// <summary>Enable affect Metal (decal only.</summary>
        public const string kAffectMetal = "_AffectMetal";
        /// <summary>Enable affect Smoothness (decal only.</summary>
        public const string kAffectSmoothness = "_AffectSmoothness";
        /// <summary>Enable affect Emission (decal only.</summary>
        public const string kAffectEmission = "_AffectEmission";

        // Internal properties

        internal const string kStencilRef = "_StencilRef";
        internal const string kStencilWriteMask = "_StencilWriteMask";
        internal const string kStencilRefDepth = "_StencilRefDepth";
        internal const string kStencilWriteMaskDepth = "_StencilWriteMaskDepth";
        internal const string kStencilRefGBuffer = "_StencilRefGBuffer";
        internal const string kStencilWriteMaskGBuffer = "_StencilWriteMaskGBuffer";
        internal const string kStencilRefMV = "_StencilRefMV";
        internal const string kStencilWriteMaskMV = "_StencilWriteMaskMV";
        internal const string kStencilRefDistortionVec = "_StencilRefDistortionVec";
        internal const string kStencilWriteMaskDistortionVec = "_StencilWriteMaskDistortionVec";
        internal const string kDecalStencilWriteMask = "_DecalStencilWriteMask";
        internal const string kDecalStencilRef = "_DecalStencilRef";

        internal const string kUseSplitLighting = "_RequireSplitLighting";

        internal static readonly Color[] kLayerColors =
        {
            Color.white,
            Color.red,
            Color.green,
            Color.blue
        };

        internal const string kDecalColorMask0 = "_DecalColorMask0";
        internal const string kDecalColorMask1 = "_DecalColorMask1";
        internal const string kDecalColorMask2 = "_DecalColorMask2";
        internal const string kDecalColorMask3 = "_DecalColorMask3";
        internal const string kEnableDecals = "_SupportDecals";

        internal const int kMaxLayerCount = 4;
        internal const string kLayerCount = "_LayerCount";

        internal const string kUVBase = "_UVBase";
        internal const string kTexWorldScale = "_TexWorldScale";
        internal const string kUVMappingMask = "_UVMappingMask";
        internal const string kUVDetail = "_UVDetail";
        internal const string kUVDetailsMappingMask = "_UVDetailsMappingMask";
        internal const string kDecalLayerMaskFromDecal = "_DecalLayerMaskFromDecal";

        internal const string kDisplacementMode = "_DisplacementMode";
        internal const string kMaterialID = "_MaterialID";
        internal const string kEmissiveColorMap = "_EmissiveColorMap";
        internal const string kTransmissionEnable = "_TransmissionEnable";
        internal const string kZTestGBuffer = "_ZTestGBuffer";
        internal const string kZTestDepthEqualForOpaque = "_ZTestDepthEqualForOpaque";
        internal const string kAlphaToMaskInspector = "_AlphaToMaskInspectorValue";
        internal const string kEmissionColor = "_EmissionColor";
        internal const string kEnableSSR = kReceivesSSR;
        internal const string kAddPrecomputedVelocity = "_AddPrecomputedVelocity";
        internal const string kShadowMatteFilter = "_ShadowMatteFilter";
        internal const string kRefractionModel = "_RefractionModel";
    }
}
