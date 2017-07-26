namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // Pre-hashed shader ids - naming conventions are a bit off in this file as we use the same
    // fields names as in the shaders for ease of use... Would be nice to clean this up at some
    // point.
    static class HDShaderIDs
    {
        internal static readonly int ShadowDatasExp = Shader.PropertyToID("_ShadowDatasExp");
        internal static readonly int ShadowPayloads = Shader.PropertyToID("_ShadowPayloads");
        internal static readonly int ShadowmapExp_VSM_0 = Shader.PropertyToID("_ShadowmapExp_VSM_0");
        internal static readonly int ShadowmapExp_VSM_1 = Shader.PropertyToID("_ShadowmapExp_VSM_1");
        internal static readonly int ShadowmapExp_VSM_2 = Shader.PropertyToID("_ShadowmapExp_VSM_2");
        internal static readonly int ShadowmapExp_PCF = Shader.PropertyToID("_ShadowmapExp_PCF");

        internal static readonly int LayeredSingleIdxBuffer = Shader.PropertyToID("g_LayeredSingleIdxBuffer");
        internal static readonly int EnvLightIndexShift = Shader.PropertyToID("_EnvLightIndexShift");
        internal static readonly int iNrVisibLights = Shader.PropertyToID("g_iNrVisibLights");
        internal static readonly int mScrProjection = Shader.PropertyToID("g_mScrProjection");
        internal static readonly int mInvScrProjection = Shader.PropertyToID("g_mInvScrProjection");
        internal static readonly int iLog2NumClusters = Shader.PropertyToID("g_iLog2NumClusters");
        internal static readonly int fNearPlane = Shader.PropertyToID("g_fNearPlane");
        internal static readonly int fFarPlane = Shader.PropertyToID("g_fFarPlane");
        internal static readonly int fClustScale = Shader.PropertyToID("g_fClustScale");
        internal static readonly int fClustBase = Shader.PropertyToID("g_fClustBase");
        internal static readonly int depth_tex = Shader.PropertyToID("g_depth_tex");
        internal static readonly int vLayeredLightList = Shader.PropertyToID("g_vLayeredLightList");
        internal static readonly int LayeredOffset = Shader.PropertyToID("g_LayeredOffset");
        internal static readonly int vBigTileLightList = Shader.PropertyToID("g_vBigTileLightList");
        internal static readonly int logBaseBuffer = Shader.PropertyToID("g_logBaseBuffer");
        internal static readonly int vBoundsBuffer = Shader.PropertyToID("g_vBoundsBuffer");
        internal static readonly int LightVolumeData = Shader.PropertyToID("_LightVolumeData");
        internal static readonly int data = Shader.PropertyToID("g_data");
        internal static readonly int mProjection = Shader.PropertyToID("g_mProjection");
        internal static readonly int mInvProjection = Shader.PropertyToID("g_mInvProjection");
        internal static readonly int viDimensions = Shader.PropertyToID("g_viDimensions");
        internal static readonly int vLightList = Shader.PropertyToID("g_vLightList");

        internal static readonly int BaseFeatureFlags = Shader.PropertyToID("g_BaseFeatureFlags");
        internal static readonly int TileFeatureFlags = Shader.PropertyToID("g_TileFeatureFlags");

        internal static readonly int GBufferTexture0 = Shader.PropertyToID("_GBufferTexture0");
        internal static readonly int GBufferTexture1 = Shader.PropertyToID("_GBufferTexture1");
        internal static readonly int GBufferTexture2 = Shader.PropertyToID("_GBufferTexture2");
        internal static readonly int GBufferTexture3 = Shader.PropertyToID("_GBufferTexture3");

        internal static readonly int DispatchIndirectBuffer = Shader.PropertyToID("g_DispatchIndirectBuffer");
        internal static readonly int TileList = Shader.PropertyToID("g_TileList");
        internal static readonly int NumTiles = Shader.PropertyToID("g_NumTiles");
        internal static readonly int NumTilesX = Shader.PropertyToID("g_NumTilesX");


        internal static readonly int BloomTex = Shader.PropertyToID("vLightListGlobal");


        internal static readonly int CookieTextures = Shader.PropertyToID("_CookieTextures");
        internal static readonly int CookieCubeTextures = Shader.PropertyToID("_CookieCubeTextures");
        internal static readonly int EnvTextures = Shader.PropertyToID("_EnvTextures");
        internal static readonly int DirectionalLightDatas = Shader.PropertyToID("_DirectionalLightDatas");
        internal static readonly int DirectionalLightCount = Shader.PropertyToID("_DirectionalLightCount");
        internal static readonly int LightDatas = Shader.PropertyToID("_LightDatas");
        internal static readonly int PunctualLightCount = Shader.PropertyToID("_PunctualLightCount");
        internal static readonly int AreaLightCount = Shader.PropertyToID("_AreaLightCount");
        internal static readonly int vLightListGlobal = Shader.PropertyToID("g_vLightListGlobal");
        internal static readonly int EnvLightDatas = Shader.PropertyToID("_EnvLightDatas");
        internal static readonly int EnvLightCount = Shader.PropertyToID("_EnvLightCount");
        internal static readonly int ShadowDatas = Shader.PropertyToID("_ShadowDatas");
        internal static readonly int DirShadowSplitSpheres = Shader.PropertyToID("_DirShadowSplitSpheres");
        internal static readonly int NumTileFtplX = Shader.PropertyToID("_NumTileFtplX");
        internal static readonly int NumTileFtplY = Shader.PropertyToID("_NumTileFtplY");
        internal static readonly int NumTileClusteredX = Shader.PropertyToID("_NumTileClusteredX");
        internal static readonly int NumTileClusteredY = Shader.PropertyToID("_NumTileClusteredY");

        internal static readonly int isLogBaseBufferEnabled = Shader.PropertyToID("g_isLogBaseBufferEnabled");
        internal static readonly int vLayeredOffsetsBuffer = Shader.PropertyToID("g_vLayeredOffsetsBuffer");

        internal static readonly int ViewTilesFlags = Shader.PropertyToID("_ViewTilesFlags");
        internal static readonly int MousePixelCoord = Shader.PropertyToID("_MousePixelCoord");

        internal static readonly int DebugViewMaterial = Shader.PropertyToID("_DebugViewMaterial");
        internal static readonly int DebugLightingMode = Shader.PropertyToID("_DebugLightingMode");
        internal static readonly int DebugLightingAlbedo = Shader.PropertyToID("_DebugLightingAlbedo");
        internal static readonly int DebugLightingSmoothness = Shader.PropertyToID("_DebugLightingSmoothness");
        internal static readonly int AmbientOcclusionTexture = Shader.PropertyToID("_AmbientOcclusionTexture");

        internal static readonly int UseTileLightList = Shader.PropertyToID("_UseTileLightList");
        internal static readonly int Time = Shader.PropertyToID("_Time");
        internal static readonly int SinTime = Shader.PropertyToID("_SinTime");
        internal static readonly int CosTime = Shader.PropertyToID("_CosTime");
        internal static readonly int unity_DeltaTime = Shader.PropertyToID("unity_DeltaTime");
        internal static readonly int EnvLightSkyEnabled = Shader.PropertyToID("_EnvLightSkyEnabled");
        internal static readonly int AmbientOcclusionDirectLightStrenght = Shader.PropertyToID("_AmbientOcclusionDirectLightStrenght");
        internal static readonly int SkyTexture = Shader.PropertyToID("_SkyTexture");

        internal static readonly int EnableSSSAndTransmission = Shader.PropertyToID("_EnableSSSAndTransmission");
        internal static readonly int TexturingModeFlags = Shader.PropertyToID("_TexturingModeFlags");
        internal static readonly int TransmissionFlags = Shader.PropertyToID("_TransmissionFlags");
        internal static readonly int ThicknessRemaps = Shader.PropertyToID("_ThicknessRemaps");
        internal static readonly int ShapeParams = Shader.PropertyToID("_ShapeParams");
        internal static readonly int TransmissionTints = Shader.PropertyToID("_TransmissionTints");
        internal static readonly int specularLightingUAV = Shader.PropertyToID("specularLightingUAV");
        internal static readonly int diffuseLightingUAV = Shader.PropertyToID("diffuseLightingUAV");

        internal static readonly int TileListOffset = Shader.PropertyToID("g_TileListOffset");

        internal static readonly int LtcData = Shader.PropertyToID("_LtcData");
        internal static readonly int PreIntegratedFGD = Shader.PropertyToID("_PreIntegratedFGD");
        internal static readonly int LtcGGXMatrix = Shader.PropertyToID("_LtcGGXMatrix");
        internal static readonly int LtcDisneyDiffuseMatrix = Shader.PropertyToID("_LtcDisneyDiffuseMatrix");
        internal static readonly int LtcMultiGGXFresnelDisneyDiffuse = Shader.PropertyToID("_LtcMultiGGXFresnelDisneyDiffuse");

        internal static readonly int MainDepthTexture = Shader.PropertyToID("_MainDepthTexture");

        internal static readonly int unity_OrthoParams = Shader.PropertyToID("unity_OrthoParams");
        internal static readonly int ZBufferParams = Shader.PropertyToID("_ZBufferParams");
        internal static readonly int ScreenParams = Shader.PropertyToID("_ScreenParams");
        internal static readonly int ProjectionParams = Shader.PropertyToID("_ProjectionParams");
        internal static readonly int WorldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");


        internal static readonly int StencilRef = Shader.PropertyToID("_StencilRef");
        internal static readonly int StencilCmp = Shader.PropertyToID("_StencilCmp");

        internal static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        internal static readonly int DstBlend = Shader.PropertyToID("_DstBlend");

        internal static readonly int HTile = Shader.PropertyToID("_HTile");
        internal static readonly int StencilTexture = Shader.PropertyToID("_StencilTexture");
    }
}
