using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    // HDRP Profile Id
    // - You can use [HideInDebugUI] attribute to hide a given id from the Detailed Stats section of Rendering Debugger.
    internal enum HDProfileId
    {
        CopyDepthBuffer,
        CopyDepthInTargetTexture,
        DuplicateDepthBuffer,
        BuildCoarseStencilAndResolveIfNeeded,
        AmbientOcclusion,
        HorizonSSAO,
        UpSampleSSAO,
        ScreenSpaceShadows,
        ScreenSpaceShadowsDebug,
        BuildLightList,
        GenerateLightAABBs,
        Distortion,
        AccumulateDistortion,
        ApplyDistortion,
        ForwardDepthPrepass,
        DeferredDepthPrepass,
        PreRefractionDepthPrepass,
        TransparentDepthPrepass,
        GBuffer,
        DBufferRender,
        DBufferPrepareDrawData,
        DBufferNormal,
        DisplayDebugDecalsAtlas,
        DisplayDebugViewMaterial,
        DebugViewMaterialGBuffer,
        SubsurfaceScattering,
        SsrTracing,
        SsrReprojection,
        SsrAccumulate,

        // SSGI
        SSGIPass,
        SSGITrace,
        SSGIDenoise,
        SSGIUpscale,
        SSGIConvert,

        ForwardOpaque,
        ForwardOpaqueDebug,
        ForwardTransparent,
        ForwardTransparentDebug,

        ForwardPreRefraction,
        ForwardPreRefractionDebug,
        ForwardTransparentDepthPrepass,
        RenderForwardError,
        TransparentDepthPostpass,
        ObjectsMotionVector,
        CameraMotionVectors,
        ColorPyramid,
        DepthPyramid,
        PostProcessing,
        AfterPostProcessingObjects,
        RenderFullScreenDebug,
        ClearBuffers,
        ClearStencil,
        HDRenderPipelineRenderCamera,
        HDRenderPipelineRenderAOV,
        HDRenderPipelineAllRenderRequest,
        CullResultsCull,
        CustomPassCullResultsCull,
        DisplayCookieAtlas,
        RenderWireFrame,
        ConvolveReflectionProbe,
        ConvertReflectionProbe,
        ConvolvePlanarReflectionProbe,
        UpdateReflectionProbeAtlas,
        BlitTextureToReflectionProbeAtlas,
        DisplayReflectionProbeAtlas,
        PreIntegradeWardCookTorrance,
        FilterCubemapCharlie,
        FilterCubemapGGX,
        AreaLightCookieConvolution,

        UpdateSkyEnvironmentConvolution,
        BackgroundCloudsAmbientProbe,
        RenderSkyToCubemap,
        UpdateSkyAmbientProbe,
        PreRenderSky,
        RenderSky,
        RenderClouds,
        OpaqueAtmosphericScattering,
        InScatteredRadiancePrecomputation,

        VolumeVoxelization,
        VolumetricLighting,
        VolumetricLightingFiltering,
        PrepareVisibleLocalVolumetricFogList,
        UpdateLocalVolumetricFogAtlas,

        // Volumetric clouds
        VolumetricClouds,
        VolumetricCloudsTrace,
        VolumetricCloudsReproject,
        VolumetricCloudsPreUpscale,
        VolumetricCloudsUpscale,
        VolumetricCloudsCombine,
        VolumetricCloudsShadow,
        VolumetricCloudMapGeneration,
        VolumetricCloudsAmbientProbe,

        // Water Decals
        WaterDecalDeformation,
        WaterDecalFoam,
        WaterDecalMask,
        WaterDecalCurrent,

        // Water surface
        WaterSurfaceUpdate,
        WaterSurfaceSimulation,
        WaterSurfaceCaustics,
        WaterExclusion,
        WaterGBuffer,
        WaterMaskDebug,
        WaterPrepareLighting,
        WaterDeferredLighting,
        WaterLineRendering,

        // High Quality Lines
        LinesGeometrySetup,
        LinesVertexSetup,
        LinesSegmentSetup,
        LinesShadingPrepare,
        LinesShading,
        LinesRasterizationSetup,
        LinesBuildClusters,
        LinesBinningStage,
        LinesWorkQueue,
        LinesFineRaster,

        // RT Cluster
        RaytracingBuildCluster,
        RaytracingCullLights,
        RaytracingDebugCluster,
        // RT acceleration structure setup
        RaytracingBuildAccelerationStructure,
        RaytracingBuildAccelerationStructureDebug,
        // RTR
        RaytracingReflectionDirectionGeneration,
        RaytracingReflectionEvaluation,
        RaytracingReflectionAdjustWeight,
        RaytracingReflectionFilter,
        RaytracingReflectionUpscale,

        // ReBlur Denoiser
        ReBlurPreBlur,
        ReBlurTemporalAccumulation,
        ReBlurMipGeneration,
        ReBlurMipHistoryFix,
        ReBlurBlur,
        ReBlurCopyHistory,
        ReBlurTemporalStabilization,
        ReBlurCopyHistoryStab,
        ReBlurPostBlur,

        // RTAO
        RaytracingAmbientOcclusion,
        RaytracingFilterAmbientOcclusion,
        RaytracingComposeAmbientOcclusion,
        RaytracingClearHistoryAmbientOcclusion,
        // RT Shadows
        RaytracingDirectionalLightShadow,
        RaytracingLightShadow,
        RaytracingAreaLightShadow,
        // RTGI
        RaytracingIndirectDiffuseDirectionGeneration,
        RaytracingIndirectDiffuseEvaluation,
        RaytracingIndirectDiffuseUpscale,
        RaytracingFilterIndirectDiffuse,
        RaytracingIndirectDiffuseAdjustWeight,

        // RTSSS
        RaytracingSSS,
        RaytracingSSSTrace,
        RaytracingSSSCompose,
        // RTShadow
        RaytracingWriteShadow,
        // Other ray tracing
        RaytracingDebugOverlay,
        RayTracingRecursiveRendering,
        RayTracingDepthPrepass,
        RayTracingFlagMask,
        // RT Deferred Lighting
        RaytracingDeferredLighting,
        // Denoisers
        HistoryValidity,
        TemporalFilter,
        DiffuseFilter,

        UpdateGlobalConstantBuffers,
        UpdateEnvironment,
        ConfigureKeywords,
        RecordRenderGraph,

        PrepareLightsForGPU,
        PrepareGPULightdata,
        PrepareGPUProbeData,
        ConvertLightsGpuFormat,
        ProcessVisibleLights,
        ProcessDirectionalAndCookies,
        SortVisibleLights,
        BuildVisibleLightEntities,
        ProcessShadows,
        ComputeShadowCullingSplits,
        CalculateLightDataTextureInfo,
        CalculateShadowIndices,
        UpdateDirectionalShadowData,
        EditorOnlyDebugSelectedLightShadow,

        // Profile sampler for shadow
        RenderShadowMaps,
        RenderMomentShadowMaps,
        RenderEVSMShadowMaps,
        RenderEVSMShadowMapsBlur,
        RenderEVSMShadowMapsCopyToAtlas,
        BlitDirectionalMixedCachedShadowMaps,
        BlitPunctualMixedCachedShadowMaps,
        BlitAreaMixedCachedShadowMaps,

        // Profile sampler for tile pass
        TileClusterLightingDebug,
        DisplayShadows,

        RenderDeferredLightingCompute,

        // Misc
        VolumeUpdate,
        CustomPassVolumeUpdate,
        OffscreenUIRendering,
        ComputeThickness,

        // XR
        XRMirrorView,
        XRCustomMirrorView,
        XRDepthCopy,

        // Low res transparency
        DownsampleDepth,
        LowResTransparent,
        CombineAndUpsampleTransparent,
        UpsampleLowResTransparent,
        CombineTransparents,

        // Line Rendering
        LineRenderingSetup,
        LineRenderingComposite,

        // Decal
        UpdateShaderGraphDecalTexture,
        UpdateDecalAtlasMipmaps,

        // Post-processing
        AlphaCopy,
        StopNaNs,
        FixedExposure,
        DynamicExposure,
        ApplyExposure,
        TemporalAntialiasing,
        UpscalerColorMask,
        FSR2,
        DeepLearningSuperSampling,
        DepthOfField,
        DepthOfFieldKernel,
        DepthOfFieldCoC,
        DepthOfFieldPrefilter,
        DepthOfFieldPyramid,
        DepthOfFieldDilate,
        DepthOfFieldTileMax,
        DepthOfFieldGatherFar,
        DepthOfFieldGatherNear,
        DepthOfFieldPreCombine,
        DepthOfFieldCombine,
        DepthOfFieldComputeSlowTiles,
        DepthOfFieldApertureShape,
        LensFlareScreenSpace,
        LensFlareDataDriven,
        LensFlareComputeOcclusionDataDriven,
        LensFlareMergeOcclusionDataDriven,
        MotionBlur,
        MotionBlurMotionVecPrep,
        MotionBlurTileMinMax,
        MotionBlurTileNeighbourhood,
        MotionBlurTileScattering,
        MotionBlurKernel,
        PaniniProjection,
        Bloom,
        ColorGradingLUTBuilder,
        UberPost,
        FXAA,
        SMAA,
        SceneUpsampling,
        SetResolutionGroup,
        FinalPost,
        FinalImageHistogram,
        HDRDebugData,
        CustomPostProcessBeforeTAA,
        CustomPostProcessBeforePP,
        CustomPostProcessAfterPPBlurs,
        CustomPostProcessAfterPP,
        CustomPostProcessAfterOpaqueAndSky,
        Sharpening,
        ContrastAdaptiveSharpen,
        EdgeAdaptiveSpatialUpsampling,
        CustomPassBufferClearDebug,

        // Temp
        APVSamplingDebug,

        AOVExecute,
        AOVOutput,
#if ENABLE_VIRTUALTEXTURES
        VTFeedbackDownsample,
#endif
    }
}
