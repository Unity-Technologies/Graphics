using UnityEngine.Profiling;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum CustomSamplerId
    {
        PushGlobalParameters,
        CopySetDepthBuffer,
        CopyDepthBuffer,
        HTileForSSS,
        Forward,
        RenderSSAO,
        ResolveSSAO,
        RenderShadowMaps,
        ScreenSpaceShadows,
        BuildLightList,
        BlitToFinalRT,
        Distortion,
        ApplyDistortion,
        DepthPrepass,
        TransparentDepthPrepass,
        GBuffer,
        DBufferRender,
        DBufferPrepareDrawData,
        DBufferNormal,
        DecalsForwardEmissive,
        DisplayDebugDecalsAtlas,
        DisplayDebugViewMaterial,
        DebugViewMaterialGBuffer,
        BlitDebugViewMaterialDebug,
        SubsurfaceScattering,
        SsrTracing,
        SsrReprojection,
        ForwardPassName,
        ForwardTransparentDepthPrepass,
        RenderForwardError,
        TransparentDepthPostpass,
        ObjectsMotionVector,
        CameraMotionVectors,
        ColorPyramid,
        DepthPyramid,
        PostProcessing,
        AfterPostProcessing,
        RenderDebug,
        ClearBuffers,
        ClearDepthStencil,
        ClearSssLightingBuffer,
        ClearSSSFilteringTarget,
        ClearAndCopyStencilTexture,
        ClearHTile,
        ClearHDRTarget,
        ClearGBuffer,
        ClearSsrBuffers,
        HDRenderPipelineRender,
        CullResultsCull,
        CopyDepth,
        UpdateStencilCopyForSSRExclusion,
        GizmosPrePostprocess,
        Gizmos,

        RaytracingBuildCluster,
        RaytracingCullLights,
        RaytracingIntegrateReflection,
        RaytracingFilterReflection,
        RaytracingAmbientOcclusion,
        RaytracingFilterAO,
        RaytracingShadowIntegration,
        RaytracingShadowCombination,
        RaytracingDebug,

        // Profile sampler for tile pass
        TPPrepareLightsForGPU,
        TPPushGlobalParameters,
        TPTiledLightingDebug,
        TPScreenSpaceShadows,
        TPTileSettingsEnableTileAndCluster,
        TPForwardPass,
        TPDisplayShadows,
        TPRenderDeferredLighting,

        // Misc
        VolumeUpdate,

        // Low res transparency
        DownsampleDepth,
        LowResTransparent,
        UpsampleLowResTransparent,

        // Post-processing
        StopNaNs,
        Exposure,
        TemporalAntialiasing,
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
        MotionBlur,
        MotionBlurMotionVecPrep,
        MotionBlurTileMinMax,
        MotionBlurTileNeighbourhood,
        MotionBlurKernel,
        PaniniProjection,
        Bloom,
        ColorGradingLUTBuilder,
        UberPost,
        FXAA,
        SMAA,
        FinalPost,

        Max
    }

    public static class HDCustomSamplerExtension
    {
        static CustomSampler[] s_Samplers;

        public static CustomSampler GetSampler(this CustomSamplerId samplerId)
        {
            // Lazy init
            if (s_Samplers == null)
            {
                s_Samplers = new CustomSampler[(int)CustomSamplerId.Max];

                for (int i = 0; i < (int)CustomSamplerId.Max; i++)
                {
                    var id = (CustomSamplerId)i;
                    s_Samplers[i] = CustomSampler.Create("C#_" + id);
                }
            }

            return s_Samplers[(int)samplerId];
        }
    }
}
