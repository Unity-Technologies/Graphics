namespace UnityEngine.Rendering.Universal
{
    // Pre-hashed shader ids - naming conventions are a bit off in this file as we use the same
    // fields names as in the shaders for ease of use...
    internal static class URPShaderIDs
    {
        public static readonly int[] _ShapeLightBlendFactors =
        {
            Shader.PropertyToID("_ShapeLightBlendFactors0"),
            Shader.PropertyToID("_ShapeLightBlendFactors1"),
            Shader.PropertyToID("_ShapeLightBlendFactors2"),
            Shader.PropertyToID("_ShapeLightBlendFactors3")
        };

        public static readonly int[] _ShapeLightMaskFilter =
        {
            Shader.PropertyToID("_ShapeLightMaskFilter0"),
            Shader.PropertyToID("_ShapeLightMaskFilter1"),
            Shader.PropertyToID("_ShapeLightMaskFilter2"),
            Shader.PropertyToID("_ShapeLightMaskFilter3")
        };

        public static readonly int[] _ShapeLightInvertedFilter =
        {
            Shader.PropertyToID("_ShapeLightInvertedFilter0"),
            Shader.PropertyToID("_ShapeLightInvertedFilter1"),
            Shader.PropertyToID("_ShapeLightInvertedFilter2"),
            Shader.PropertyToID("_ShapeLightInvertedFilter3")
        };

        public static readonly int _SrcBlend = Shader.PropertyToID("_SrcBlend");
        public static readonly int _DstBlend = Shader.PropertyToID("_DstBlend");
        public static readonly int _FalloffIntensity = Shader.PropertyToID("_FalloffIntensity");
        public static readonly int _FalloffDistance = Shader.PropertyToID("_FalloffDistance");
        public static readonly int _LightColor = Shader.PropertyToID("_LightColor");
        public static readonly int _VolumeOpacity = Shader.PropertyToID("_VolumeOpacity");
        public static readonly int _CookieTex = Shader.PropertyToID("_CookieTex");
        public static readonly int _FalloffLookup = Shader.PropertyToID("_FalloffLookup");
        public static readonly int _LightPosition = Shader.PropertyToID("_LightPosition");
        public static readonly int _LightInvMatrix = Shader.PropertyToID("_LightInvMatrix");
        public static readonly int _InnerRadiusMult = Shader.PropertyToID("_InnerRadiusMult");
        public static readonly int _OuterAngle = Shader.PropertyToID("_OuterAngle");
        public static readonly int _InnerAngleMult = Shader.PropertyToID("_InnerAngleMult");
        public static readonly int _LightLookup = Shader.PropertyToID("_LightLookup");
        public static readonly int _IsFullSpotlight = Shader.PropertyToID("_IsFullSpotlight");
        public static readonly int _LightZDistance = Shader.PropertyToID("_LightZDistance");
        public static readonly int _PointLightCookieTex = Shader.PropertyToID("_PointLightCookieTex");

        public static readonly int _HDREmulationScale = Shader.PropertyToID("_HDREmulationScale");
        public static readonly int _InverseHDREmulationScale = Shader.PropertyToID("_InverseHDREmulationScale");
        public static readonly int _UseSceneLighting = Shader.PropertyToID("_UseSceneLighting");
        public static readonly int _CameraSortingLayerTexture = Shader.PropertyToID("_CameraSortingLayerTexture");

        public static readonly int[] _ShapeLightTexture =
        {
            Shader.PropertyToID("_ShapeLightTexture0"),
            Shader.PropertyToID("_ShapeLightTexture1"),
            Shader.PropertyToID("_ShapeLightTexture2"),
            Shader.PropertyToID("_ShapeLightTexture3")
        };

        public static readonly int _LightPos = Shader.PropertyToID("_LightPos");
        public static readonly int _ShadowStencilGroup = Shader.PropertyToID("_ShadowStencilGroup");
        public static readonly int _ShadowIntensity = Shader.PropertyToID("_ShadowIntensity");
        public static readonly int _ShadowVolumeIntensity = Shader.PropertyToID("_ShadowVolumeIntensity");
        public static readonly int _ShadowRadius = Shader.PropertyToID("_ShadowRadius");


        public static readonly int _ColorBalance = Shader.PropertyToID("_ColorBalance");
        public static readonly int _ColorFilter = Shader.PropertyToID("_ColorFilter");
        public static readonly int _ChannelMixerRed = Shader.PropertyToID("_ChannelMixerRed");
        public static readonly int _ChannelMixerGreen = Shader.PropertyToID("_ChannelMixerGreen");
        public static readonly int _ChannelMixerBlue = Shader.PropertyToID("_ChannelMixerBlue");
        public static readonly int _HueSatCon = Shader.PropertyToID("_HueSatCon");
        public static readonly int _Lift = Shader.PropertyToID("_Lift");
        public static readonly int _Gamma = Shader.PropertyToID("_Gamma");
        public static readonly int _Gain = Shader.PropertyToID("_Gain");
        public static readonly int _Shadows = Shader.PropertyToID("_Shadows");
        public static readonly int _Midtones = Shader.PropertyToID("_Midtones");
        public static readonly int _Highlights = Shader.PropertyToID("_Highlights");
        public static readonly int _ShaHiLimits = Shader.PropertyToID("_ShaHiLimits");
        public static readonly int _SplitShadows = Shader.PropertyToID("_SplitShadows");
        public static readonly int _SplitHighlights = Shader.PropertyToID("_SplitHighlights");
        public static readonly int _CurveMaster = Shader.PropertyToID("_CurveMaster");
        public static readonly int _CurveRed = Shader.PropertyToID("_CurveRed");
        public static readonly int _CurveGreen = Shader.PropertyToID("_CurveGreen");
        public static readonly int _CurveBlue = Shader.PropertyToID("_CurveBlue");
        public static readonly int _CurveHueVsHue = Shader.PropertyToID("_CurveHueVsHue");
        public static readonly int _CurveHueVsSat = Shader.PropertyToID("_CurveHueVsSat");
        public static readonly int _CurveLumVsSat = Shader.PropertyToID("_CurveLumVsSat");
        public static readonly int _CurveSatVsSat = Shader.PropertyToID("_CurveSatVsSat");

        public static readonly int _SampleOffset = Shader.PropertyToID("_SampleOffset");

        public static readonly int _DrawObjectPassData = Shader.PropertyToID("_DrawObjectPassData");

        public static int[] _BloomMipUp =
        {
            Shader.PropertyToID("_BloomMipUp0"),
            Shader.PropertyToID("_BloomMipUp1"),
            Shader.PropertyToID("_BloomMipUp2"),
            Shader.PropertyToID("_BloomMipUp3"),
            Shader.PropertyToID("_BloomMipUp4"),
            Shader.PropertyToID("_BloomMipUp5"),
            Shader.PropertyToID("_BloomMipUp6"),
            Shader.PropertyToID("_BloomMipUp7"),
            Shader.PropertyToID("_BloomMipUp8"),
            Shader.PropertyToID("_BloomMipUp9"),
            Shader.PropertyToID("_BloomMipUp10"),
            Shader.PropertyToID("_BloomMipUp11"),
            Shader.PropertyToID("_BloomMipUp12"),
            Shader.PropertyToID("_BloomMipUp13"),
            Shader.PropertyToID("_BloomMipUp14"),
            Shader.PropertyToID("_BloomMipUp15"),
        };
        public static int[] _BloomMipDown =
        {
            Shader.PropertyToID("_BloomMipDown0"),
            Shader.PropertyToID("_BloomMipDown1"),
            Shader.PropertyToID("_BloomMipDown2"),
            Shader.PropertyToID("_BloomMipDown3"),
            Shader.PropertyToID("_BloomMipDown4"),
            Shader.PropertyToID("_BloomMipDown5"),
            Shader.PropertyToID("_BloomMipDown6"),
            Shader.PropertyToID("_BloomMipDown7"),
            Shader.PropertyToID("_BloomMipDown8"),
            Shader.PropertyToID("_BloomMipDown9"),
            Shader.PropertyToID("_BloomMipDown10"),
            Shader.PropertyToID("_BloomMipDown11"),
            Shader.PropertyToID("_BloomMipDown12"),
            Shader.PropertyToID("_BloomMipDown13"),
            Shader.PropertyToID("_BloomMipDown14"),
            Shader.PropertyToID("_BloomMipDown15"),
        };

        public static readonly int _StencilRef         = Shader.PropertyToID("_StencilRef");
        public static readonly int _StencilMask        = Shader.PropertyToID("_StencilMask");

        public static readonly int _FullCoCTexture     = Shader.PropertyToID("_FullCoCTexture");
        public static readonly int _HalfCoCTexture     = Shader.PropertyToID("_HalfCoCTexture");
        public static readonly int _DofTexture         = Shader.PropertyToID("_DofTexture");
        public static readonly int _CoCParams          = Shader.PropertyToID("_CoCParams");
        public static readonly int _BokehKernel        = Shader.PropertyToID("_BokehKernel");
        public static readonly int _BokehConstants     = Shader.PropertyToID("_BokehConstants");
        public static readonly int _PongTexture        = Shader.PropertyToID("_PongTexture");
        public static readonly int _PingTexture        = Shader.PropertyToID("_PingTexture");

        public static readonly int _Metrics            = Shader.PropertyToID("_Metrics");
        public static readonly int _AreaTexture        = Shader.PropertyToID("_AreaTexture");
        public static readonly int _SearchTexture      = Shader.PropertyToID("_SearchTexture");
        public static readonly int _EdgeTexture        = Shader.PropertyToID("_EdgeTexture");
        public static readonly int _BlendTexture       = Shader.PropertyToID("_BlendTexture");

        public static readonly int _ColorTexture       = Shader.PropertyToID("_ColorTexture");
        public static readonly int _Params             = Shader.PropertyToID("_Params");
        public static readonly int _SourceTexLowMip    = Shader.PropertyToID("_SourceTexLowMip");
        public static readonly int _Bloom_Params       = Shader.PropertyToID("_Bloom_Params");
        public static readonly int _Bloom_RGBM         = Shader.PropertyToID("_Bloom_RGBM");
        public static readonly int _Bloom_Texture      = Shader.PropertyToID("_Bloom_Texture");
        public static readonly int _LensDirt_Texture   = Shader.PropertyToID("_LensDirt_Texture");
        public static readonly int _LensDirt_Params    = Shader.PropertyToID("_LensDirt_Params");
        public static readonly int _LensDirt_Intensity = Shader.PropertyToID("_LensDirt_Intensity");
        public static readonly int _Distortion_Params1 = Shader.PropertyToID("_Distortion_Params1");
        public static readonly int _Distortion_Params2 = Shader.PropertyToID("_Distortion_Params2");
        public static readonly int _Chroma_Params      = Shader.PropertyToID("_Chroma_Params");
        public static readonly int _Vignette_Params1   = Shader.PropertyToID("_Vignette_Params1");
        public static readonly int _Vignette_Params2   = Shader.PropertyToID("_Vignette_Params2");
        public static readonly int _Lut_Params         = Shader.PropertyToID("_Lut_Params");
        public static readonly int _UserLut_Params     = Shader.PropertyToID("_UserLut_Params");
        public static readonly int _InternalLut        = Shader.PropertyToID("_InternalLut");
        public static readonly int _UserLut            = Shader.PropertyToID("_UserLut");
        public static readonly int _DownSampleScaleFactor = Shader.PropertyToID("_DownSampleScaleFactor");

        public static readonly int _FullscreenProjMat = Shader.PropertyToID("_FullscreenProjMat");

        public static readonly int _BaseMapID = Shader.PropertyToID("_BaseMap");
        public static readonly int _SSAOParamsID = Shader.PropertyToID("_SSAOParams");
        public static readonly int _ProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
        public static readonly int _CameraViewProjectionsID = Shader.PropertyToID("_CameraViewProjections");
        public static readonly int _CameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner");
        public static readonly int _CameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
        public static readonly int _CameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");
        public static readonly int _CameraViewZExtentID = Shader.PropertyToID("_CameraViewZExtent");
        public static readonly int[] _SSAO_OcclusionTexture =
        {
            0,
            Shader.PropertyToID("_SSAO_OcclusionTexture1"),
            Shader.PropertyToID("_SSAO_OcclusionTexture2"),
            Shader.PropertyToID("_SSAO_OcclusionTexture3"),
        };

        public static readonly int _SourceTexArraySlice = Shader.PropertyToID("_SourceTexArraySlice");
        public static readonly int _SRGBRead = Shader.PropertyToID("_SRGBRead");
        public static readonly int _SRGBWrite = Shader.PropertyToID("_SRGBWrite");


        public static readonly int _LitStencilRef = Shader.PropertyToID("_LitStencilRef");
        public static readonly int _LitStencilReadMask = Shader.PropertyToID("_LitStencilReadMask");
        public static readonly int _LitStencilWriteMask = Shader.PropertyToID("_LitStencilWriteMask");
        public static readonly int _SimpleLitStencilRef = Shader.PropertyToID("_SimpleLitStencilRef");
        public static readonly int _SimpleLitStencilReadMask = Shader.PropertyToID("_SimpleLitStencilReadMask");
        public static readonly int _SimpleLitStencilWriteMask = Shader.PropertyToID("_SimpleLitStencilWriteMask");
        public static readonly int _StencilReadMask = Shader.PropertyToID("_StencilReadMask");
        public static readonly int _StencilWriteMask = Shader.PropertyToID("_StencilWriteMask");
        public static readonly int _LitPunctualStencilRef = Shader.PropertyToID("_LitPunctualStencilRef");
        public static readonly int _LitPunctualStencilReadMask = Shader.PropertyToID("_LitPunctualStencilReadMask");
        public static readonly int _LitPunctualStencilWriteMask = Shader.PropertyToID("_LitPunctualStencilWriteMask");
        public static readonly int _SimpleLitPunctualStencilRef = Shader.PropertyToID("_SimpleLitPunctualStencilRef");

        public static readonly int _SimpleLitPunctualStencilReadMask =
            Shader.PropertyToID("_SimpleLitPunctualStencilReadMask");

        public static readonly int _SimpleLitPunctualStencilWriteMask =
            Shader.PropertyToID("_SimpleLitPunctualStencilWriteMask");

        public static readonly int _LitDirStencilRef = Shader.PropertyToID("_LitDirStencilRef");
        public static readonly int _LitDirStencilReadMask = Shader.PropertyToID("_LitDirStencilReadMask");
        public static readonly int _LitDirStencilWriteMask = Shader.PropertyToID("_LitDirStencilWriteMask");
        public static readonly int _SimpleLitDirStencilRef = Shader.PropertyToID("_SimpleLitDirStencilRef");
        public static readonly int _SimpleLitDirStencilReadMask = Shader.PropertyToID("_SimpleLitDirStencilReadMask");
        public static readonly int _SimpleLitDirStencilWriteMask = Shader.PropertyToID("_SimpleLitDirStencilWriteMask");
        public static readonly int _ClearStencilRef = Shader.PropertyToID("_ClearStencilRef");
        public static readonly int _ClearStencilReadMask = Shader.PropertyToID("_ClearStencilReadMask");
        public static readonly int _ClearStencilWriteMask = Shader.PropertyToID("_ClearStencilWriteMask");

        public static readonly int UDepthRanges = Shader.PropertyToID("UDepthRanges");
        public static readonly int _DepthRanges = Shader.PropertyToID("_DepthRanges");
        public static readonly int _DownsamplingWidth = Shader.PropertyToID("_DownsamplingWidth");
        public static readonly int _DownsamplingHeight = Shader.PropertyToID("_DownsamplingHeight");
        public static readonly int _SourceShiftX = Shader.PropertyToID("_SourceShiftX");
        public static readonly int _SourceShiftY = Shader.PropertyToID("_SourceShiftY");
        public static readonly int _TileShiftX = Shader.PropertyToID("_TileShiftX");
        public static readonly int _TileShiftY = Shader.PropertyToID("_TileShiftY");
        public static readonly int _tileXCount = Shader.PropertyToID("_tileXCount");
        public static readonly int _DepthRangeOffset = Shader.PropertyToID("_DepthRangeOffset");
        public static readonly int _BitmaskTex = Shader.PropertyToID("_BitmaskTex");
        public static readonly int UTileList = Shader.PropertyToID("UTileList");
        public static readonly int _TileList = Shader.PropertyToID("_TileList");
        public static readonly int UPunctualLightBuffer = Shader.PropertyToID("UPunctualLightBuffer");
        public static readonly int _PunctualLightBuffer = Shader.PropertyToID("_PunctualLightBuffer");
        public static readonly int URelLightList = Shader.PropertyToID("URelLightList");
        public static readonly int _RelLightList = Shader.PropertyToID("_RelLightList");
        public static readonly int _TilePixelWidth = Shader.PropertyToID("_TilePixelWidth");
        public static readonly int _TilePixelHeight = Shader.PropertyToID("_TilePixelHeight");
        public static readonly int _InstanceOffset = Shader.PropertyToID("_InstanceOffset");
        public static readonly int _DepthTex = Shader.PropertyToID("_DepthTex");
        public static readonly int _DepthTexSize = Shader.PropertyToID("_DepthTexSize");
        public static readonly int _ScreenSize = Shader.PropertyToID("_ScreenSize");

        public static readonly int _ScreenToWorld = Shader.PropertyToID("_ScreenToWorld");

        public static readonly int[] _unproject =
        {
            Shader.PropertyToID("_unproject0"),
            Shader.PropertyToID("_unproject1"),
        };

        public static readonly int _MainLightPosition = Shader.PropertyToID("_MainLightPosition");
        public static readonly int _MainLightColor = Shader.PropertyToID("_MainLightColor");
        public static readonly int _MainLightOcclusionProbes = Shader.PropertyToID("_MainLightOcclusionProbes");
        public static readonly int _AdditionalLightsCount = Shader.PropertyToID("_AdditionalLightsCount");
        public static readonly int _AdditionalLightsBuffer = Shader.PropertyToID("_AdditionalLightsBuffer");
        public static readonly int _AdditionalLightsIndices = Shader.PropertyToID("_AdditionalLightsIndices");
        public static readonly int _AdditionalLightsPosition = Shader.PropertyToID("_AdditionalLightsPosition");
        public static readonly int _AdditionalLightsColor = Shader.PropertyToID("_AdditionalLightsColor");
        public static readonly int _AdditionalLightsAttenuation = Shader.PropertyToID("_AdditionalLightsAttenuation");
        public static readonly int _AdditionalLightsSpotDir = Shader.PropertyToID("_AdditionalLightsSpotDir");

        public static readonly int _AdditionalLightsOcclusionProbes =
            Shader.PropertyToID("_AdditionalLightsOcclusionProbes");

        public static int _SpotLightScale = Shader.PropertyToID("_SpotLightScale");
        public static int _SpotLightBias = Shader.PropertyToID("_SpotLightBias");
        public static int _SpotLightGuard = Shader.PropertyToID("_SpotLightGuard");
        public static int _LightPosWS = Shader.PropertyToID("_LightPosWS");
        public static int _LightAttenuation = Shader.PropertyToID("_LightAttenuation");
        public static int _LightOcclusionProbInfo = Shader.PropertyToID("_LightOcclusionProbInfo");
        public static int _LightDirection = Shader.PropertyToID("_LightDirection");
        public static int _LightFlags = Shader.PropertyToID("_LightFlags");
        public static int _ShadowLightIndex = Shader.PropertyToID("_ShadowLightIndex");

        public static readonly int _Grain_Texture = Shader.PropertyToID("_Grain_Texture");
        public static readonly int _Grain_Params = Shader.PropertyToID("_Grain_Params");
        public static readonly int _Grain_TilingParams = Shader.PropertyToID("_Grain_TilingParams");

        public static readonly int _BlueNoise_Texture = Shader.PropertyToID("_BlueNoise_Texture");
        public static readonly int _Dithering_Params = Shader.PropertyToID("_Dithering_Params");

        public static readonly int _SourceSize = Shader.PropertyToID("_SourceSize");

        public static readonly int unity_StereoMatrixV = Shader.PropertyToID("unity_StereoMatrixV");
        public static readonly int unity_StereoMatrixInvV = Shader.PropertyToID("unity_StereoMatrixInvV");
        public static readonly int unity_StereoMatrixP = Shader.PropertyToID("unity_StereoMatrixP");
        public static readonly int unity_StereoMatrixInvP = Shader.PropertyToID("unity_StereoMatrixInvP");
        public static readonly int unity_StereoMatrixVP = Shader.PropertyToID("unity_StereoMatrixVP");
        public static readonly int unity_StereoMatrixInvVP = Shader.PropertyToID("unity_StereoMatrixInvVP");
        public static readonly int unity_StereoCameraProjection = Shader.PropertyToID("unity_StereoCameraProjection");

        public static readonly int unity_StereoCameraInvProjection =
            Shader.PropertyToID("unity_StereoCameraInvProjection");

        public static readonly int unity_StereoWorldSpaceCameraPos =
            Shader.PropertyToID("unity_StereoWorldSpaceCameraPos");


        public static readonly int _GlossyEnvironmentColor = Shader.PropertyToID("_GlossyEnvironmentColor");
        public static readonly int _SubtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

        public static readonly int unity_AmbientSky = Shader.PropertyToID("unity_AmbientSky");
        public static readonly int unity_AmbientEquator = Shader.PropertyToID("unity_AmbientEquator");
        public static readonly int unity_AmbientGround = Shader.PropertyToID("unity_AmbientGround");

        public static readonly int _Time = Shader.PropertyToID("_Time");
        public static readonly int _SinTime = Shader.PropertyToID("_SinTime");
        public static readonly int _CosTime = Shader.PropertyToID("_CosTime");
        public static readonly int unity_DeltaTime = Shader.PropertyToID("unity_DeltaTime");
        public static readonly int _TimeParameters = Shader.PropertyToID("_TimeParameters");

        public static readonly int _ScaledScreenParams = Shader.PropertyToID("_ScaledScreenParams");
        public static readonly int _WorldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");
        public static readonly int _ScreenParams = Shader.PropertyToID("_ScreenParams");
        public static readonly int _ProjectionParams = Shader.PropertyToID("_ProjectionParams");
        public static readonly int _ZBufferParams = Shader.PropertyToID("_ZBufferParams");
        public static readonly int unity_OrthoParams = Shader.PropertyToID("unity_OrthoParams");

        public static readonly int unity_MatrixV = Shader.PropertyToID("unity_MatrixV");
        public static readonly int glstate_matrix_projection = Shader.PropertyToID("glstate_matrix_projection");
        public static readonly int unity_MatrixVP = Shader.PropertyToID("unity_MatrixVP");

        public static readonly int unity_MatrixInvV = Shader.PropertyToID("unity_MatrixInvV");
        public static readonly int unity_MatrixInvP = Shader.PropertyToID("unity_MatrixInvP");
        public static readonly int unity_MatrixInvVP = Shader.PropertyToID("unity_MatrixInvVP");

        public static readonly int unity_CameraProjection = Shader.PropertyToID("unity_CameraProjection");
        public static readonly int unity_CameraInvProjection = Shader.PropertyToID("unity_CameraInvProjection");
        public static readonly int unity_WorldToCamera = Shader.PropertyToID("unity_WorldToCamera");
        public static readonly int unity_CameraToWorld = Shader.PropertyToID("unity_CameraToWorld");

        public static readonly int unity_CameraWorldClipPlanes = Shader.PropertyToID("unity_CameraWorldClipPlanes");

        public static readonly int unity_BillboardNormal = Shader.PropertyToID("unity_BillboardNormal");
        public static readonly int unity_BillboardTangent = Shader.PropertyToID("unity_BillboardTangent");
        public static readonly int unity_BillboardCameraParams = Shader.PropertyToID("unity_BillboardCameraParams");

        public static readonly int _SourceTex = Shader.PropertyToID("_SourceTex");
        public static readonly int _ScaleBias = Shader.PropertyToID("_ScaleBias");
        public static readonly int _ScaleBiasRt = Shader.PropertyToID("_ScaleBiasRt");

        public static readonly int _RTHandleScale = Shader.PropertyToID("_RTHandleScale");

        // Required for 2D Unlit Shadergraph master node as it doesn't currently support hidden properties.
        public static readonly int _RendererColor = Shader.PropertyToID("_RendererColor");

        public static readonly int _NormalMap = Shader.PropertyToID("_NormalMap");
        public static readonly int _ShadowTex = Shader.PropertyToID("_ShadowTex");

        public static readonly int _AfterPostProcessTexture = Shader.PropertyToID("_AfterPostProcessTexture");
        public static readonly int _InternalGradingLut = Shader.PropertyToID("_InternalGradingLut");

        public static readonly int _AdditionalLightsWorldToShadow =
            Shader.PropertyToID("_AdditionalLightsWorldToShadow");

        public static readonly int _AdditionalShadowParams = Shader.PropertyToID("_AdditionalShadowParams");

        public static readonly int[] _AdditionalShadowOffset =
        {
            Shader.PropertyToID("_AdditionalShadowOffset0"),
            Shader.PropertyToID("_AdditionalShadowOffset1"),
            Shader.PropertyToID("_AdditionalShadowOffset2"),
            Shader.PropertyToID("_AdditionalShadowOffset3"),
        };

        public static readonly int _AdditionalShadowFadeParams = Shader.PropertyToID("_AdditionalShadowFadeParams");
        public static readonly int _AdditionalShadowmapSize = Shader.PropertyToID("_AdditionalShadowmapSize");

        public static readonly int _AdditionalLightsShadowmapTexture =
            Shader.PropertyToID("_AdditionalLightsShadowmapTexture");

        public static readonly int _AdditionalLightsWorldToShadow_SSBO =
            Shader.PropertyToID("_AdditionalLightsWorldToShadow_SSBO");

        public static readonly int _AdditionalShadowParams_SSBO = Shader.PropertyToID("_AdditionalShadowParams_SSBO");

        public static readonly int _MainLightWorldToShadow = Shader.PropertyToID("_MainLightWorldToShadow");

        public static readonly int[] _CascadeShadowSplitSpheres =
        {
            Shader.PropertyToID("_CascadeShadowSplitSpheres0"),
            Shader.PropertyToID("_CascadeShadowSplitSpheres1"),
            Shader.PropertyToID("_CascadeShadowSplitSpheres2"),
            Shader.PropertyToID("_CascadeShadowSplitSpheres3"),
        };

        public static readonly int _CascadeShadowSplitSphereRadii =
            Shader.PropertyToID("_CascadeShadowSplitSphereRadii");

        public static readonly int[] _MainLightShadowOffset =
        {
            Shader.PropertyToID("_MainLightShadowOffset0"),
            Shader.PropertyToID("_MainLightShadowOffset1"),
            Shader.PropertyToID("_MainLightShadowOffset2"),
            Shader.PropertyToID("_MainLightShadowOffset3"),
        };

        public static readonly int _MainLightShadowParams = Shader.PropertyToID("_MainLightShadowParams");
        public static readonly int _MainLightShadowmapSize = Shader.PropertyToID("_MainLightShadowmapSize");
        public static readonly int _MainLightShadowmapTexture = Shader.PropertyToID("_MainLightShadowmapTexture");

        public static readonly int _ScreenSpaceShadowmapTexture = Shader.PropertyToID("_ScreenSpaceShadowmapTexture");

        public static readonly int _CameraColorTexture = Shader.PropertyToID("_CameraColorTexture");
        public static readonly int _CameraDepthAttachment = Shader.PropertyToID("_CameraDepthAttachment");
        public static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
        public static readonly int _CameraNormalsTexture = Shader.PropertyToID("_CameraNormalsTexture");

        public static readonly int _GBufferDepthAsColor = Shader.PropertyToID("_GBufferDepthAsColor");
        public static readonly int[] _GBuffer =
        {
            Shader.PropertyToID("_GBuffer0"),
            Shader.PropertyToID("_GBuffer1"),
            Shader.PropertyToID("_GBuffer2"),
            0,
            Shader.PropertyToID("_GBuffer4"),
        };

        public static readonly int _CameraOpaqueTexture = Shader.PropertyToID("_CameraOpaqueTexture");
        public static readonly int _DepthInfoTexture = Shader.PropertyToID("_DepthInfoTexture");
        public static readonly int _TileDepthInfoTexture = Shader.PropertyToID("_TileDepthInfoTexture");
    }
}
