using System;
using System.Reflection;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Runtime Shaders", Order = 1000), HideInInspector]
    class HDRenderPipelineRuntimeShaders : IRenderPipelineResources
    {
        public int version => 0;

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        #region General
        [Header("General")]
        [SerializeField, ResourcePath("Runtime/Material/Lit/Lit.shader")]
        private Shader m_DefaultShader;

        public Shader defaultShader
        {
            get => m_DefaultShader;
            set => this.SetValueAndNotify(ref m_DefaultShader, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/RenderPass/MotionVectors/CameraMotionVectors.shader")]
        private Shader m_CameraMotionVectorsPS;

        public Shader cameraMotionVectorsPS
        {
            get => m_CameraMotionVectorsPS;
            set => this.SetValueAndNotify(ref m_CameraMotionVectorsPS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/RenderPass/ColorPyramidPS.Shader")]
        private Shader m_ColorPyramidPS;

        public Shader colorPyramidPS
        {
            get => m_ColorPyramidPS;
            set => this.SetValueAndNotify(ref m_ColorPyramidPS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/RenderPass/ColorPyramid.compute")]
        public ComputeShader m_ColorPyramidCS;
        public ComputeShader colorPyramidCS
        {
            get => m_ColorPyramidCS;
            set => this.SetValueAndNotify(ref m_ColorPyramidCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/RenderPass/DepthPyramid.compute")]
        private ComputeShader m_DepthPyramidCS;

        public ComputeShader depthPyramidCS
        {
            get => m_DepthPyramidCS;
            set => this.SetValueAndNotify(ref m_DepthPyramidCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/RenderPass/GenerateMaxZ.compute")]
        private ComputeShader m_MaxZCS;

        public ComputeShader maxZCS
        {
            get => m_MaxZCS;
            set => this.SetValueAndNotify(ref m_MaxZCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/RenderPass/Distortion/ApplyDistortion.shader")]
        private Shader m_ApplyDistortionPS;

        public Shader applyDistortionPS
        {
            get => m_ApplyDistortionPS;
            set => this.SetValueAndNotify(ref m_ApplyDistortionPS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassUtils.shader")]
        private Shader m_CustomPassUtils;

        public Shader customPassUtils
        {
            get => m_CustomPassUtils;
            set => this.SetValueAndNotify(ref m_CustomPassUtils, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderersUtils.shader")]
        private Shader m_CustomPassRenderersUtils;

        public Shader customPassRenderersUtils
        {
            get => m_CustomPassRenderersUtils;
            set => this.SetValueAndNotify(ref m_CustomPassRenderersUtils, value);
        }

        [SerializeField, ResourcePath("Runtime/ShaderLibrary/ClearStencilBuffer.shader")]
        private Shader m_ClearStencilBufferPS;

        public Shader clearStencilBufferPS
        {
            get => m_ClearStencilBufferPS;
            set => this.SetValueAndNotify(ref m_ClearStencilBufferPS, value);
        }

        [SerializeField, ResourcePath("Runtime/ShaderLibrary/CopyStencilBuffer.shader")]
        private Shader m_CopyStencilBufferPS;

        public Shader copyStencilBufferPS
        {
            get => m_CopyStencilBufferPS;
            set => this.SetValueAndNotify(ref m_CopyStencilBufferPS, value);
        }

        [SerializeField, ResourcePath("Runtime/ShaderLibrary/CopyDepthBuffer.shader")]
        private Shader m_CopyDepthBufferPS;

        public Shader copyDepthBufferPS
        {
            get => m_CopyDepthBufferPS;
            set => this.SetValueAndNotify(ref m_CopyDepthBufferPS, value);
        }

        [SerializeField, ResourcePath("Runtime/ShaderLibrary/Blit.shader")]
        private Shader m_BlitPS;

        public Shader blitPS
        {
            get => m_BlitPS;
            set => this.SetValueAndNotify(ref m_BlitPS, value);
        }

        [SerializeField, ResourcePath("Runtime/ShaderLibrary/BlitColorAndDepth.shader")]
        private Shader m_BlitColorAndDepthPS;

        public Shader blitColorAndDepthPS
        {
            get => m_BlitColorAndDepthPS;
            set => this.SetValueAndNotify(ref m_BlitColorAndDepthPS, value);
        }

        [SerializeField, ResourcePath("Runtime/ShaderLibrary/DownsampleDepth.shader")]
        private Shader m_DownsampleDepthPS;

        public Shader downsampleDepthPS
        {
            get => m_DownsampleDepthPS;
            set => this.SetValueAndNotify(ref m_DownsampleDepthPS, value);
        }

        [SerializeField, ResourcePath("Runtime/ShaderLibrary/UpsampleTransparent.shader")]
        private Shader m_UpsampleTransparentPS;

        public Shader upsampleTransparentPS
        {
            get => m_UpsampleTransparentPS;
            set => this.SetValueAndNotify(ref m_UpsampleTransparentPS, value);
        }

        [SerializeField, ResourcePath("Runtime/ShaderLibrary/ResolveStencilBuffer.compute")]
        private ComputeShader m_ResolveStencilCS;

        public ComputeShader resolveStencilCS
        {
            get => m_ResolveStencilCS;
            set => this.SetValueAndNotify(ref m_ResolveStencilCS, value);
        }
        #endregion

        #region Debug
        [Header("Debug")]
        [SerializeField, ResourcePath("Runtime/Debug/DebugDisplayLatlong.Shader")]
        private Shader m_DebugDisplayLatlongPS;

        public Shader debugDisplayLatlongPS
        {
            get => m_DebugDisplayLatlongPS;
            set => this.SetValueAndNotify(ref m_DebugDisplayLatlongPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/DebugViewMaterialGBuffer.Shader")]
        private Shader m_DebugViewMaterialGBufferPS;

        public Shader debugViewMaterialGBufferPS
        {
            get => m_DebugViewMaterialGBufferPS;
            set => this.SetValueAndNotify(ref m_DebugViewMaterialGBufferPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/DebugViewTiles.Shader")]
        private Shader m_DebugViewTilesPS;

        public Shader debugViewTilesPS
        {
            get => m_DebugViewTilesPS;
            set => this.SetValueAndNotify(ref m_DebugViewTilesPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/DebugFullScreen.Shader")]
        private Shader m_DebugFullScreenPS;

        public Shader debugFullScreenPS
        {
            get => m_DebugFullScreenPS;
            set => this.SetValueAndNotify(ref m_DebugFullScreenPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/DebugColorPicker.Shader")]
        private Shader m_DebugColorPickerPS;

        public Shader debugColorPickerPS
        {
            get => m_DebugColorPickerPS;
            set => this.SetValueAndNotify(ref m_DebugColorPickerPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/DebugExposure.Shader")]
        private Shader m_DebugExposurePS;

        public Shader debugExposurePS
        {
            get => m_DebugExposurePS;
            set => this.SetValueAndNotify(ref m_DebugExposurePS, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/DebugHDR.Shader")]
        private Shader m_DebugHDRPS;

        public Shader debugHDRPS
        {
            get => m_DebugHDRPS;
            set => this.SetValueAndNotify(ref m_DebugHDRPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/DebugLightVolumes.Shader")]
        private Shader m_DebugLightVolumePS;

        public Shader debugLightVolumePS
        {
            get => m_DebugLightVolumePS;
            set => this.SetValueAndNotify(ref m_DebugLightVolumePS, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/DebugLightVolumes.compute")]
        private ComputeShader m_DebugLightVolumeCS;

        public ComputeShader debugLightVolumeCS
        {
            get => m_DebugLightVolumeCS;
            set => this.SetValueAndNotify(ref m_DebugLightVolumeCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/DebugBlitQuad.Shader")]
        private Shader m_DebugBlitQuad;

        public Shader debugBlitQuad
        {
            get => m_DebugBlitQuad;
            set => this.SetValueAndNotify(ref m_DebugBlitQuad, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/DebugVTBlit.Shader")]
        private Shader m_DebugViewVirtualTexturingBlit;

        public Shader debugViewVirtualTexturingBlit
        {
            get => m_DebugViewVirtualTexturingBlit;
            set => this.SetValueAndNotify(ref m_DebugViewVirtualTexturingBlit, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/MaterialError.Shader")]
        private Shader m_MaterialError;

        public Shader materialError
        {
            get => m_MaterialError;
            set => this.SetValueAndNotify(ref m_MaterialError, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/MaterialLoading.shader")]
        private Shader m_MaterialLoading;

        public Shader materialLoading
        {
            get => m_MaterialLoading;
            set => this.SetValueAndNotify(ref m_MaterialLoading, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/ClearDebugBuffer.compute")]
        private ComputeShader m_ClearDebugBufferCS;

        public ComputeShader clearDebugBufferCS
        {
            get => m_ClearDebugBufferCS;
            set => this.SetValueAndNotify(ref m_ClearDebugBufferCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/DebugWaveform.shader")]
        private Shader m_DebugWaveformPS;

        public Shader debugWaveformPS
        {
            get => m_DebugWaveformPS;
            set => this.SetValueAndNotify(ref m_DebugWaveformPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/DebugWaveform.compute")]
        private ComputeShader m_DebugWaveformCS;

        public ComputeShader debugWaveformCS
        {
            get => m_DebugWaveformCS;
            set => this.SetValueAndNotify(ref m_DebugWaveformCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/DebugVectorscope.shader")]
        private Shader m_DebugVectorscopePS;

        public Shader debugVectorscopePS
        {
            get => m_DebugVectorscopePS;
            set => this.SetValueAndNotify(ref m_DebugVectorscopePS, value);
        }

        [SerializeField, ResourcePath("Runtime/Debug/DebugVectorscope.compute")]
        private ComputeShader m_DebugVectorscopeCS;

        public ComputeShader debugVectorscopeCS
        {
            get => m_DebugVectorscopeCS;
            set => this.SetValueAndNotify(ref m_DebugVectorscopeCS, value);
        }
        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DebugHistogramImage.compute")]
        private ComputeShader m_DebugImageHistogramCS;
        public ComputeShader debugImageHistogramCS
        {
            get => m_DebugImageHistogramCS;
            set => this.SetValueAndNotify(ref m_DebugImageHistogramCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DebugHDRxyMapping.compute")]
        private ComputeShader m_DebugHDRxyMappingCS;
        public ComputeShader debugHDRxyMappingCS
        {
            get => m_DebugHDRxyMappingCS;
            set => this.SetValueAndNotify(ref m_DebugHDRxyMappingCS, value);
        }
        #endregion

        #region APV
        [Header("APV")]
        [SerializeField, ResourcePath("Runtime/Debug/ProbeVolumeSamplingDebugPositionNormal.compute")]
        private ComputeShader m_ProbeVolumeSamplingDebugComputeShader;

        public ComputeShader probeVolumeSamplingDebugComputeShader
        {
            get => m_ProbeVolumeSamplingDebugComputeShader;
            set => this.SetValueAndNotify(ref m_ProbeVolumeSamplingDebugComputeShader, value);
        }
        #endregion

        #region Lighting
        [Header("Lighting")]
        [SerializeField, ResourcePath("Runtime/Lighting/PlanarReflectionFiltering.compute")]
        private ComputeShader m_PlanarReflectionFilteringCS;

        public ComputeShader planarReflectionFilteringCS
        {
            get => m_PlanarReflectionFilteringCS;
            set => this.SetValueAndNotify(ref m_PlanarReflectionFilteringCS, value);
        }

        [Header("Lighting - Screen Space")]
        [SerializeField, ResourcePath("Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceGlobalIllumination.compute")]
        private ComputeShader m_ScreenSpaceGlobalIlluminationCS;

        public ComputeShader screenSpaceGlobalIlluminationCS
        {
            get => m_ScreenSpaceGlobalIlluminationCS;
            set => this.SetValueAndNotify(ref m_ScreenSpaceGlobalIlluminationCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceReflections.compute")]
        private ComputeShader m_ScreenSpaceReflectionsCS;

        public ComputeShader screenSpaceReflectionsCS
        {
            get => m_ScreenSpaceReflectionsCS;
            set => this.SetValueAndNotify(ref m_ScreenSpaceReflectionsCS, value);
        }

        [Header("Lighting - Tile Pass")]
        [SerializeField, ResourcePath("Runtime/Lighting/LightLoop/cleardispatchindirect.compute")]
        private ComputeShader m_ClearDispatchIndirectCS;

        public ComputeShader clearDispatchIndirectCS
        {
            get => m_ClearDispatchIndirectCS;
            set => this.SetValueAndNotify(ref m_ClearDispatchIndirectCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/LightLoop/ClearLightLists.compute")]
        private ComputeShader m_ClearLightListsCS;

        public ComputeShader clearLightListsCS
        {
            get => m_ClearLightListsCS;
            set => this.SetValueAndNotify(ref m_ClearLightListsCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/LightLoop/builddispatchindirect.compute")]
        private ComputeShader m_BuildDispatchIndirectCS;

        public ComputeShader buildDispatchIndirectCS
        {
            get => m_BuildDispatchIndirectCS;
            set => this.SetValueAndNotify(ref m_BuildDispatchIndirectCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/LightLoop/scrbound.compute")]
        private ComputeShader m_BuildScreenAABBCS;

        public ComputeShader buildScreenAABBCS
        {
            get => m_BuildScreenAABBCS;
            set => this.SetValueAndNotify(ref m_BuildScreenAABBCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/LightLoop/lightlistbuild.compute")]
        private ComputeShader m_BuildPerTileLightListCS; // FPTL

        public ComputeShader buildPerTileLightListCS
        {
            get => m_BuildPerTileLightListCS;
            set => this.SetValueAndNotify(ref m_BuildPerTileLightListCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/LightLoop/lightlistbuild-bigtile.compute")]
        private ComputeShader m_BuildPerBigTileLightListCS;

        public ComputeShader buildPerBigTileLightListCS
        {
            get => m_BuildPerBigTileLightListCS;
            set => this.SetValueAndNotify(ref m_BuildPerBigTileLightListCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/LightLoop/lightlistbuild-clustered.compute")]
        private ComputeShader m_BuildPerVoxelLightListCS; // clustered

        public ComputeShader buildPerVoxelLightListCS
        {
            get => m_BuildPerVoxelLightListCS;
            set => this.SetValueAndNotify(ref m_BuildPerVoxelLightListCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/LightLoop/lightlistbuild-clearatomic.compute")]
        private ComputeShader m_LightListClusterClearAtomicIndexCS;

        public ComputeShader lightListClusterClearAtomicIndexCS
        {
            get => m_LightListClusterClearAtomicIndexCS;
            set => this.SetValueAndNotify(ref m_LightListClusterClearAtomicIndexCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/LightLoop/materialflags.compute")]
        private ComputeShader m_BuildMaterialFlagsCS;

        public ComputeShader buildMaterialFlagsCS
        {
            get => m_BuildMaterialFlagsCS;
            set => this.SetValueAndNotify(ref m_BuildMaterialFlagsCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/LightLoop/Deferred.compute")]
        private ComputeShader m_DeferredCS;

        public ComputeShader deferredCS
        {
            get => m_DeferredCS;
            set => this.SetValueAndNotify(ref m_DeferredCS, value);
        }
        #endregion

        #region Volumetric Fog
        [Header("Fog")]
        [SerializeField, ResourcePath("Runtime/Lighting/VolumetricLighting/VolumeVoxelization.compute")]
        private ComputeShader m_VolumeVoxelizationCS;

        public ComputeShader volumeVoxelizationCS
        {
            get => m_VolumeVoxelizationCS;
            set => this.SetValueAndNotify(ref m_VolumeVoxelizationCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/VolumetricLighting/VolumetricLighting.compute")]
        private ComputeShader m_VolumetricLightingCS;

        public ComputeShader volumetricLightingCS
        {
            get => m_VolumetricLightingCS;
            set => this.SetValueAndNotify(ref m_VolumetricLightingCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/VolumetricLighting/VolumetricLightingFiltering.compute")]
        private ComputeShader m_VolumetricLightingFilteringCS;

        public ComputeShader volumetricLightingFilteringCS
        {
            get => m_VolumetricLightingFilteringCS;
            set => this.SetValueAndNotify(ref m_VolumetricLightingFilteringCS, value);
        }

        // Default Fog Volume Shader
        [SerializeField, ResourcePath("Runtime/RenderPipelineResources/ShaderGraph/DefaultFogVolume.shadergraph")]
        private Shader m_DefaultFogVolumeShader;
        public Shader defaultFogVolumeShader
        {
            get => m_DefaultFogVolumeShader;
            set => this.SetValueAndNotify(ref m_DefaultFogVolumeShader, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/AtmosphericScattering/ScreenSpaceMultipleScattering.compute")]
        private ComputeShader m_ScreenSpaceMultipleScatteringCS;
        public ComputeShader screenSpaceMultipleScatteringCS
        {
            get => m_ScreenSpaceMultipleScatteringCS;
            set => this.SetValueAndNotify(ref m_ScreenSpaceMultipleScatteringCS, value);
        }

        #endregion

        #region SSS
        [Header("SubsurfaceScattering")]
        [SerializeField, ResourcePath("Runtime/Material/SubsurfaceScattering/SubsurfaceScattering.compute")]
        private ComputeShader m_SubsurfaceScatteringCS; // Disney SSS

        public ComputeShader subsurfaceScatteringCS
        {
            get => m_SubsurfaceScatteringCS;
            set => this.SetValueAndNotify(ref m_SubsurfaceScatteringCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Material/SubsurfaceScattering/RandomDownsample.compute")]
        private ComputeShader m_SubsurfaceScatteringDownsampleCS;

        public ComputeShader subsurfaceScatteringDownsampleCS
        {
            get => m_SubsurfaceScatteringDownsampleCS;
            set => this.SetValueAndNotify(ref m_SubsurfaceScatteringDownsampleCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Material/SubsurfaceScattering/CombineLighting.shader")]
        private Shader m_CombineLightingPS;

        public Shader combineLightingPS
        {
            get => m_CombineLightingPS;
            set => this.SetValueAndNotify(ref m_CombineLightingPS, value);
        }
        #endregion

        #region Sky
        [Header("Sky")]
        [SerializeField, ResourcePath("Runtime/Sky/BlitCubemap.shader")]
        private Shader m_BlitCubemapPS;

        public Shader blitCubemapPS
        {
            get => m_BlitCubemapPS;
            set => this.SetValueAndNotify(ref m_BlitCubemapPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/AtmosphericScattering/OpaqueAtmosphericScattering.shader")]
        private Shader m_OpaqueAtmosphericScatteringPS;

        public Shader opaqueAtmosphericScatteringPS
        {
            get => m_OpaqueAtmosphericScatteringPS;
            set => this.SetValueAndNotify(ref m_OpaqueAtmosphericScatteringPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Sky/HDRISky/HDRISky.shader")]
        private Shader m_HdriSkyPS;

        public Shader hdriSkyPS
        {
            get => m_HdriSkyPS;
            set => this.SetValueAndNotify(ref m_HdriSkyPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Sky/HDRISky/IntegrateHDRISky.shader")]
        private Shader m_IntegrateHdriSkyPS;

        public Shader integrateHdriSkyPS
        {
            get => m_IntegrateHdriSkyPS;
            set => this.SetValueAndNotify(ref m_IntegrateHdriSkyPS, value);
        }

        [SerializeField, ResourcePath("Skybox/Cubemap", SearchType.BuiltinPath)]
        private Shader m_SkyboxCubemapPS;

        public Shader skyboxCubemapPS
        {
            get => m_SkyboxCubemapPS;
            set => this.SetValueAndNotify(ref m_SkyboxCubemapPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Sky/GradientSky/GradientSky.shader")]
        private Shader m_GradientSkyPS;

        public Shader gradientSkyPS
        {
            get => m_GradientSkyPS;
            set => this.SetValueAndNotify(ref m_GradientSkyPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Sky/AmbientProbeConvolution.compute")]
        private ComputeShader m_AmbientProbeConvolutionCS;

        public ComputeShader ambientProbeConvolutionCS
        {
            get => m_AmbientProbeConvolutionCS;
            set => this.SetValueAndNotify(ref m_AmbientProbeConvolutionCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Sky/PhysicallyBasedSky/GroundIrradiancePrecomputation.compute")]
        private ComputeShader m_GroundIrradiancePrecomputationCS;

        public ComputeShader groundIrradiancePrecomputationCS
        {
            get => m_GroundIrradiancePrecomputationCS;
            set => this.SetValueAndNotify(ref m_GroundIrradiancePrecomputationCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Sky/PhysicallyBasedSky/InScatteredRadiancePrecomputation.compute")]
        private ComputeShader m_InScatteredRadiancePrecomputationCS;

        public ComputeShader inScatteredRadiancePrecomputationCS
        {
            get => m_InScatteredRadiancePrecomputationCS;
            set => this.SetValueAndNotify(ref m_InScatteredRadiancePrecomputationCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSky.shader")]
        private Shader m_PhysicallyBasedSkyPS;

        public Shader physicallyBasedSkyPS
        {
            get => m_PhysicallyBasedSkyPS;
            set => this.SetValueAndNotify(ref m_PhysicallyBasedSkyPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Sky/CloudSystem/CloudLayer/CloudLayer.shader")]
        private Shader m_CloudLayerPS;

        public Shader cloudLayerPS
        {
            get => m_CloudLayerPS;
            set => this.SetValueAndNotify(ref m_CloudLayerPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Sky/CloudSystem/CloudLayer/BakeCloudTexture.compute")]
        private ComputeShader m_BakeCloudTextureCS;

        public ComputeShader bakeCloudTextureCS
        {
            get => m_BakeCloudTextureCS;
            set => this.SetValueAndNotify(ref m_BakeCloudTextureCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Sky/CloudSystem/CloudLayer/BakeCloudShadows.compute")]
        private ComputeShader m_BakeCloudShadowsCS;

        public ComputeShader bakeCloudShadowsCS
        {
            get => m_BakeCloudShadowsCS;
            set => this.SetValueAndNotify(ref m_BakeCloudShadowsCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Sky/PhysicallyBasedSky/SkyLUTGenerator.compute")]
        private ComputeShader m_SkyLUTGenerator;

        public ComputeShader skyLUTGenerator
        {
            get => m_SkyLUTGenerator;
            set => this.SetValueAndNotify(ref m_SkyLUTGenerator, value);
        }

        #endregion

        // NOTE: Move this to Core SRP once a 'core resource' concept exists.
        #region Line Rendering

        [Header("Line Rendering")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/LineRendering/Kernels/StagePrepare.compute")]
        private ComputeShader m_LineStagePrepareCS;

        public ComputeShader lineStagePrepareCS
        {
            get => m_LineStagePrepareCS;
            set => this.SetValueAndNotify(ref m_LineStagePrepareCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/LineRendering/Kernels/StageSetupSegment.compute")]
        private ComputeShader m_LineStageSetupSegmentCS;

        public ComputeShader lineStageSetupSegmentCS
        {
            get => m_LineStageSetupSegmentCS;
            set => this.SetValueAndNotify(ref m_LineStageSetupSegmentCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/LineRendering/Kernels/StageShadingSetup.compute")]
        private ComputeShader m_LineStageShadingSetupCS;

        public ComputeShader lineStageShadingSetupCS
        {
            get => m_LineStageShadingSetupCS;
            set => this.SetValueAndNotify(ref m_LineStageShadingSetupCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/LineRendering/Kernels/StageRasterBin.compute")]
        private ComputeShader m_LineStageRasterBinCS;

        public ComputeShader lineStageRasterBinCS
        {
            get => m_LineStageRasterBinCS;
            set => this.SetValueAndNotify(ref m_LineStageRasterBinCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/LineRendering/Kernels/StageWorkQueue.compute")]
        private ComputeShader m_LineStageWorkQueueCS;

        public ComputeShader lineStageWorkQueueCS
        {
            get => m_LineStageWorkQueueCS;
            set => this.SetValueAndNotify(ref m_LineStageWorkQueueCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/LineRendering/Kernels/StageRasterFine.compute")]
        private ComputeShader m_LineStageRasterFineCS;

        public ComputeShader lineStageRasterFineCS
        {
            get => m_LineStageRasterFineCS;
            set => this.SetValueAndNotify(ref m_LineStageRasterFineCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/LineRendering/CompositeLines.shader")]
        private Shader m_LineCompositePS;

        public Shader lineCompositePS
        {
            get => m_LineCompositePS;
            set => this.SetValueAndNotify(ref m_LineCompositePS, value);
        }
        #endregion

        #region Material
        [Header("Material")]
        [SerializeField, ResourcePath("Runtime/Material/PreIntegratedFGD/PreIntegratedFGD_GGXDisneyDiffuse.shader")]
        private Shader m_PreIntegratedFGD_GGXDisneyDiffusePS;

        public Shader preIntegratedFGD_GGXDisneyDiffusePS
        {
            get => m_PreIntegratedFGD_GGXDisneyDiffusePS;
            set => this.SetValueAndNotify(ref m_PreIntegratedFGD_GGXDisneyDiffusePS, value);
        }

        [SerializeField, ResourcePath("Runtime/Material/PreIntegratedFGD/PreIntegratedFGD_CharlieFabricLambert.shader")]
        private Shader m_PreIntegratedFGD_CharlieFabricLambertPS;

        public Shader preIntegratedFGD_CharlieFabricLambertPS
        {
            get => m_PreIntegratedFGD_CharlieFabricLambertPS;
            set => this.SetValueAndNotify(ref m_PreIntegratedFGD_CharlieFabricLambertPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Material/AxF/PreIntegratedFGD_Ward.shader")]
        private Shader m_PreIntegratedFGD_WardPS;

        public Shader preIntegratedFGD_WardPS
        {
            get => m_PreIntegratedFGD_WardPS;
            set => this.SetValueAndNotify(ref m_PreIntegratedFGD_WardPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Material/AxF/PreIntegratedFGD_CookTorrance.shader")]
        private Shader m_PreIntegratedFGD_CookTorrancePS;

        public Shader preIntegratedFGD_CookTorrancePS
        {
            get => m_PreIntegratedFGD_CookTorrancePS;
            set => this.SetValueAndNotify(ref m_PreIntegratedFGD_CookTorrancePS, value);
        }

        [SerializeField, ResourcePath("Runtime/Material/PreIntegratedFGD/PreIntegratedFGD_Marschner.shader")]
        private Shader m_PreIntegratedFGD_MarschnerPS;

        public Shader preIntegratedFGD_MarschnerPS
        {
            get => m_PreIntegratedFGD_MarschnerPS;
            set => this.SetValueAndNotify(ref m_PreIntegratedFGD_MarschnerPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Material/Hair/MultipleScattering/HairMultipleScatteringPreIntegration.compute")]
        private ComputeShader m_PreIntegratedFiberScatteringCS;

        public ComputeShader preIntegratedFiberScatteringCS
        {
            get => m_PreIntegratedFiberScatteringCS;
            set => this.SetValueAndNotify(ref m_PreIntegratedFiberScatteringCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Material/VolumetricMaterial/VolumetricMaterial.compute")]
        private ComputeShader m_VolumetricMaterialCS;

        public ComputeShader volumetricMaterialCS
        {
            get => m_VolumetricMaterialCS;
            set => this.SetValueAndNotify(ref m_VolumetricMaterialCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Material/Eye/EyeCausticLUTGen.compute")]
        private ComputeShader m_EyeMaterialCS;

        public ComputeShader eyeMaterialCS
        {
            get => m_EyeMaterialCS;
            set => this.SetValueAndNotify(ref m_EyeMaterialCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Material/LTCAreaLight/FilterAreaLightCookies.shader")]
        private Shader m_FilterAreaLightCookiesPS;

        public Shader filterAreaLightCookiesPS
        {
            get => m_FilterAreaLightCookiesPS;
            set => this.SetValueAndNotify(ref m_FilterAreaLightCookiesPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Material/GGXConvolution/BuildProbabilityTables.compute")]
        private ComputeShader m_BuildProbabilityTablesCS;

        public ComputeShader buildProbabilityTablesCS
        {
            get => m_BuildProbabilityTablesCS;
            set => this.SetValueAndNotify(ref m_BuildProbabilityTablesCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Material/GGXConvolution/ComputeGgxIblSampleData.compute")]
        private ComputeShader m_ComputeGgxIblSampleDataCS;

        public ComputeShader computeGgxIblSampleDataCS
        {
            get => m_ComputeGgxIblSampleDataCS;
            set => this.SetValueAndNotify(ref m_ComputeGgxIblSampleDataCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Material/GGXConvolution/GGXConvolve.shader")]
        private Shader m_GGXConvolvePS;

        public Shader GGXConvolvePS
        {
            get => m_GGXConvolvePS;
            set => this.SetValueAndNotify(ref m_GGXConvolvePS, value);
        }

        [SerializeField, ResourcePath("Runtime/Material/Fabric/CharlieConvolve.shader")]
        private Shader m_CharlieConvolvePS;

        public Shader charlieConvolvePS
        {
            get => m_CharlieConvolvePS;
            set => this.SetValueAndNotify(ref m_CharlieConvolvePS, value);
        }
        #endregion

        #region Utilities
        [Header("Utilities / Core")]
        // Prefix Sum
        [SerializeField, ResourcePath("Runtime/Utilities/GPUPrefixSum/GPUPrefixSum.compute")]
        private ComputeShader m_GpuPrefixSumCS;

        public ComputeShader gpuPrefixSumCS
        {
            get => m_GpuPrefixSumCS;
            set => this.SetValueAndNotify(ref m_GpuPrefixSumCS, value);
        }

        // Copy
        [SerializeField, ResourcePath("Runtime/Utilities/GPUSort/GPUSort.compute")]
        private ComputeShader m_GpuSortCS;

        public ComputeShader gpuSortCS
        {
            get => m_GpuSortCS;
            set => this.SetValueAndNotify(ref m_GpuSortCS, value);
        }

        // Denoising
        [SerializeField, ResourcePath("Runtime/Lighting/ScreenSpaceLighting/BilateralUpsample.compute")]
        private ComputeShader m_BilateralUpsampleCS;
        public ComputeShader bilateralUpsampleCS
        {
            get => m_BilateralUpsampleCS;
            set => this.SetValueAndNotify(ref m_BilateralUpsampleCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/TemporalFilter.compute")]
        private ComputeShader m_TemporalFilterCS;
        public ComputeShader temporalFilterCS
        {
            get => m_TemporalFilterCS;
            set => this.SetValueAndNotify(ref m_TemporalFilterCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/DiffuseDenoiser.compute")]
        private ComputeShader m_DiffuseDenoiserCS;
        public ComputeShader diffuseDenoiserCS
        {
            get => m_DiffuseDenoiserCS;
            set => this.SetValueAndNotify(ref m_DiffuseDenoiserCS, value);
        }

#if UNITY_EDITOR
        // Furnace Testing (BSDF Energy Conservation)
        [SerializeField, ResourcePath("Tests/Editor/Utilities/FurnaceTests.compute")]
        private ComputeShader m_FurnaceTestCS;
        public ComputeShader furnaceTestCS
        {
            get => m_FurnaceTestCS;
            set => this.SetValueAndNotify(ref m_FurnaceTestCS, value);
        }
#endif

        // Object ID Shader
        [SerializeField, ResourcePath("Runtime/ShaderLibrary/SolidColor.shadergraph")]
        private Shader m_ObjectIDPS;
        public Shader objectIDPS
        {
            get => m_ObjectIDPS;
            set => this.SetValueAndNotify(ref m_ObjectIDPS, value);
        }

        // Compute Thickness
        [SerializeField, ResourcePath("Runtime/RenderPipeline/ShaderPass/ComputeThickness.shader")]
        private Shader m_ComputeThicknessPS;

        public Shader ComputeThicknessPS
        {
            get => m_ComputeThicknessPS;
            set => this.SetValueAndNotify(ref m_ComputeThicknessPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Core/CoreResources/GPUCopy.compute")]
        private ComputeShader m_CopyChannelCS;

        public ComputeShader copyChannelCS
        {
            get => m_CopyChannelCS;
            set => this.SetValueAndNotify(ref m_CopyChannelCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Core/CoreResources/ClearBuffer2D.compute")]
        private ComputeShader m_ClearBuffer2D;

        public ComputeShader clearBuffer2D
        {
            get => m_ClearBuffer2D;
            set => this.SetValueAndNotify(ref m_ClearBuffer2D, value);
        }

        [SerializeField, ResourcePath("Runtime/Core/CoreResources/EncodeBC6H.compute")]
        private ComputeShader m_EncodeBC6HCS;

        public ComputeShader encodeBC6HCS
        {
            get => m_EncodeBC6HCS;
            set => this.SetValueAndNotify(ref m_EncodeBC6HCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Core/CoreResources/CubeToPano.shader")]
        private Shader m_CubeToPanoPS;

        public Shader cubeToPanoPS
        {
            get => m_CubeToPanoPS;
            set => this.SetValueAndNotify(ref m_CubeToPanoPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Core/CoreResources/BlitCubeTextureFace.shader")]
        private Shader m_BlitCubeTextureFacePS;

        public Shader blitCubeTextureFacePS
        {
            get => m_BlitCubeTextureFacePS;
            set => this.SetValueAndNotify(ref m_BlitCubeTextureFacePS, value);
        }

        [SerializeField, ResourcePath("Runtime/Core/CoreResources/ClearUIntTextureArray.compute")]
        private ComputeShader m_ClearUIntTextureCS;

        public ComputeShader clearUIntTextureCS
        {
            get => m_ClearUIntTextureCS;
            set => this.SetValueAndNotify(ref m_ClearUIntTextureCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Utility/Texture3DAtlas.compute")]
        private ComputeShader m_Texture3DAtlasCS;

        public ComputeShader texture3DAtlasCS
        {
            get => m_Texture3DAtlasCS;
            set => this.SetValueAndNotify(ref m_Texture3DAtlasCS, value);
        }
        #endregion

        #region XR
        [Header("XR")]
        [SerializeField, ResourcePath("Runtime/ShaderLibrary/XRMirrorView.shader")]
        private Shader m_XrMirrorViewPS;

        public Shader xrMirrorViewPS
        {
            get => m_XrMirrorViewPS;
            set => this.SetValueAndNotify(ref m_XrMirrorViewPS, value);
        }

        [SerializeField, ResourcePath("Runtime/ShaderLibrary/XROcclusionMesh.shader")]
        private Shader m_XrOcclusionMeshPS;

        public Shader xrOcclusionMeshPS
        {
            get => m_XrOcclusionMeshPS;
            set => this.SetValueAndNotify(ref m_XrOcclusionMeshPS, value);
        }
        #endregion

        #region Shadows

        [Header("Shadow")]
        [SerializeField, ResourcePath("Runtime/Lighting/Shadow/ContactShadows.compute")]
        private ComputeShader m_ContactShadowCS;

        public ComputeShader contactShadowCS
        {
            get => m_ContactShadowCS;
            set => this.SetValueAndNotify(ref m_ContactShadowCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/Shadow/ScreenSpaceShadows.shader")]
        private Shader m_ScreenSpaceShadowPS;

        public Shader screenSpaceShadowPS
        {
            get => m_ScreenSpaceShadowPS;
            set => this.SetValueAndNotify(ref m_ScreenSpaceShadowPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/Shadow/ShadowClear.shader")]
        private Shader m_ShadowClearPS;

        public Shader shadowClearPS
        {
            get => m_ShadowClearPS;
            set => this.SetValueAndNotify(ref m_ShadowClearPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/Shadow/EVSMBlur.compute")]
        private ComputeShader m_EvsmBlurCS;

        public ComputeShader evsmBlurCS
        {
            get => m_EvsmBlurCS;
            set => this.SetValueAndNotify(ref m_EvsmBlurCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/Shadow/DebugDisplayHDShadowMap.shader")]
        private Shader m_DebugHDShadowMapPS;

        public Shader debugHDShadowMapPS
        {
            get => m_DebugHDShadowMapPS;
            set => this.SetValueAndNotify(ref m_DebugHDShadowMapPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/VolumetricLighting/DebugLocalVolumetricFogAtlas.shader")]
        private Shader m_DebugLocalVolumetricFogAtlasPS;
        public Shader debugLocalVolumetricFogAtlasPS
        {
            get => m_DebugLocalVolumetricFogAtlasPS;
            set => this.SetValueAndNotify(ref m_DebugLocalVolumetricFogAtlasPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/Shadow/MomentShadows.compute")]
        private ComputeShader m_MomentShadowsCS;

        public ComputeShader momentShadowsCS
        {
            get => m_MomentShadowsCS;
            set => this.SetValueAndNotify(ref m_MomentShadowsCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/Shadow/ShadowBlit.shader")]
        private Shader m_ShadowBlitPS;

        public Shader shadowBlitPS
        {
            get => m_ShadowBlitPS;
            set => this.SetValueAndNotify(ref m_ShadowBlitPS, value);
        }
        #endregion

        #region Decals
        [Header("Decal")]
        [SerializeField, ResourcePath("Runtime/Material/Decal/DecalNormalBuffer.shader")]
        private Shader m_DecalNormalBufferPS;

        public Shader decalNormalBufferPS
        {
            get => m_DecalNormalBufferPS;
            set => this.SetValueAndNotify(ref m_DecalNormalBufferPS, value);
        }
        #endregion

        #region Ambient occlusion
        [Header("Ambient occlusion")]
        [SerializeField, ResourcePath("Runtime/Lighting/ScreenSpaceLighting/GTAO.compute")]
        private ComputeShader m_GTAOCS;

        public ComputeShader GTAOCS
        {
            get => m_GTAOCS;
            set => this.SetValueAndNotify(ref m_GTAOCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/ScreenSpaceLighting/GTAOSpatialDenoise.compute")]
        private ComputeShader m_GTAOSpatialDenoiseCS;

        public ComputeShader GTAOSpatialDenoiseCS
        {
            get => m_GTAOSpatialDenoiseCS;
            set => this.SetValueAndNotify(ref m_GTAOSpatialDenoiseCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/ScreenSpaceLighting/GTAOTemporalDenoise.compute")]
        private ComputeShader m_GTAOTemporalDenoiseCS;

        public ComputeShader GTAOTemporalDenoiseCS
        {
            get => m_GTAOTemporalDenoiseCS;
            set => this.SetValueAndNotify(ref m_GTAOTemporalDenoiseCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/ScreenSpaceLighting/GTAOCopyHistory.compute")]
        private ComputeShader m_GTAOCopyHistoryCS;

        public ComputeShader GTAOCopyHistoryCS
        {
            get => m_GTAOCopyHistoryCS;
            set => this.SetValueAndNotify(ref m_GTAOCopyHistoryCS, value);
        }

        [SerializeField, ResourcePath("Runtime/Lighting/ScreenSpaceLighting/GTAOBlurAndUpsample.compute")]
        private ComputeShader m_GTAOBlurAndUpsample;

        public ComputeShader GTAOBlurAndUpsample
        {
            get => m_GTAOBlurAndUpsample;
            set => this.SetValueAndNotify(ref m_GTAOBlurAndUpsample, value);
        }
        #endregion

        #region Post-processing
        [Header("Post-processing")]

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/AlphaCopy.compute")]
        private ComputeShader m_CopyAlphaCS;
        public ComputeShader copyAlphaCS
        {
            get => m_CopyAlphaCS;
            set => this.SetValueAndNotify(ref m_CopyAlphaCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/NaNKiller.compute")]
        private ComputeShader m_NanKillerCS;
        public ComputeShader nanKillerCS
        {
            get => m_NanKillerCS;
            set => this.SetValueAndNotify(ref m_NanKillerCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/Exposure.compute")]
        private ComputeShader m_ExposureCS;
        public ComputeShader exposureCS
        {
            get => m_ExposureCS;
            set => this.SetValueAndNotify(ref m_ExposureCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/HistogramExposure.compute")]
        private ComputeShader m_HistogramExposureCS;
        public ComputeShader histogramExposureCS
        {
            get => m_HistogramExposureCS;
            set => this.SetValueAndNotify(ref m_HistogramExposureCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/ApplyExposure.compute")]
        private ComputeShader m_ApplyExposureCS;
        public ComputeShader applyExposureCS
        {
            get => m_ApplyExposureCS;
            set => this.SetValueAndNotify(ref m_ApplyExposureCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/UberPost.compute")]
        private ComputeShader m_UberPostCS;
        public ComputeShader uberPostCS
        {
            get => m_UberPostCS;
            set => this.SetValueAndNotify(ref m_UberPostCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/LutBuilder3D.compute")]
        private ComputeShader m_LutBuilder3DCS;
        public ComputeShader lutBuilder3DCS
        {
            get => m_LutBuilder3DCS;
            set => this.SetValueAndNotify(ref m_LutBuilder3DCS, value);
        }

        [Header("Post-processing - Depth Of Field")]
        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DepthOfFieldKernel.compute")]
        private ComputeShader m_DepthOfFieldKernelCS;
        public ComputeShader depthOfFieldKernelCS
        {
            get => m_DepthOfFieldKernelCS;
            set => this.SetValueAndNotify(ref m_DepthOfFieldKernelCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DepthOfFieldCoC.compute")]
        private ComputeShader m_DepthOfFieldCoCCS;
        public ComputeShader depthOfFieldCoCCS
        {
            get => m_DepthOfFieldCoCCS;
            set => this.SetValueAndNotify(ref m_DepthOfFieldCoCCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DepthOfFieldCoCReproject.compute")]
        private ComputeShader m_DepthOfFieldCoCReprojectCS;
        public ComputeShader depthOfFieldCoCReprojectCS
        {
            get => m_DepthOfFieldCoCReprojectCS;
            set => this.SetValueAndNotify(ref m_DepthOfFieldCoCReprojectCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DepthOfFieldCoCDilate.compute")]
        private ComputeShader m_DepthOfFieldDilateCS;
        public ComputeShader depthOfFieldDilateCS
        {
            get => m_DepthOfFieldDilateCS;
            set => this.SetValueAndNotify(ref m_DepthOfFieldDilateCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DepthOfFieldMip.compute")]
        private ComputeShader m_DepthOfFieldMipCS;
        public ComputeShader depthOfFieldMipCS
        {
            get => m_DepthOfFieldMipCS;
            set => this.SetValueAndNotify(ref m_DepthOfFieldMipCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DepthOfFieldMipSafe.compute")]
        private ComputeShader m_DepthOfFieldMipSafeCS;
        public ComputeShader depthOfFieldMipSafeCS
        {
            get => m_DepthOfFieldMipSafeCS;
            set => this.SetValueAndNotify(ref m_DepthOfFieldMipSafeCS, value);
        }
        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DepthOfFieldPrefilter.compute")]
        private ComputeShader m_DepthOfFieldPrefilterCS;
        public ComputeShader depthOfFieldPrefilterCS
        {
            get => m_DepthOfFieldPrefilterCS;
            set => this.SetValueAndNotify(ref m_DepthOfFieldPrefilterCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DepthOfFieldTileMax.compute")]
        private ComputeShader m_DepthOfFieldTileMaxCS;
        public ComputeShader depthOfFieldTileMaxCS
        {
            get => m_DepthOfFieldTileMaxCS;
            set => this.SetValueAndNotify(ref m_DepthOfFieldTileMaxCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DepthOfFieldGather.compute")]
        private ComputeShader m_DepthOfFieldGatherCS;
        public ComputeShader depthOfFieldGatherCS
        {
            get => m_DepthOfFieldGatherCS;
            set => this.SetValueAndNotify(ref m_DepthOfFieldGatherCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DepthOfFieldCombine.compute")]
        private ComputeShader m_DepthOfFieldCombineCS;
        public ComputeShader depthOfFieldCombineCS
        {
            get => m_DepthOfFieldCombineCS;
            set => this.SetValueAndNotify(ref m_DepthOfFieldCombineCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DepthOfFieldPreCombineFar.compute")]
        private ComputeShader m_DepthOfFieldPreCombineFarCS;
        public ComputeShader depthOfFieldPreCombineFarCS
        {
            get => m_DepthOfFieldPreCombineFarCS;
            set => this.SetValueAndNotify(ref m_DepthOfFieldPreCombineFarCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DepthOfFieldClearIndirectArgs.compute")]
        private ComputeShader m_DepthOfFieldClearIndirectArgsCS;
        public ComputeShader depthOfFieldClearIndirectArgsCS
        {
            get => m_DepthOfFieldClearIndirectArgsCS;
            set => this.SetValueAndNotify(ref m_DepthOfFieldClearIndirectArgsCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/PaniniProjection.compute")]
        private ComputeShader m_PaniniProjectionCS;
        public ComputeShader paniniProjectionCS
        {
            get => m_PaniniProjectionCS;
            set => this.SetValueAndNotify(ref m_PaniniProjectionCS, value);
        }

        // Physically based DoF
        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DoFCircleOfConfusion.compute")]
        private ComputeShader m_DofCircleOfConfusion;
        public ComputeShader dofCircleOfConfusion
        {
            get => m_DofCircleOfConfusion;
            set => this.SetValueAndNotify(ref m_DofCircleOfConfusion, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DoFGather.compute")]
        private ComputeShader m_DofGatherCS;
        public ComputeShader dofGatherCS
        {
            get => m_DofGatherCS;
            set => this.SetValueAndNotify(ref m_DofGatherCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DoFCoCMinMax.compute")]
        private ComputeShader m_DofCoCMinMaxCS;
        public ComputeShader dofCoCMinMaxCS
        {
            get => m_DofCoCMinMaxCS;
            set => this.SetValueAndNotify(ref m_DofCoCMinMaxCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DoFMinMaxDilate.compute")]
        private ComputeShader m_DofMinMaxDilateCS;
        public ComputeShader dofMinMaxDilateCS
        {
            get => m_DofMinMaxDilateCS;
            set => this.SetValueAndNotify(ref m_DofMinMaxDilateCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DoFCombine.compute")]
        private ComputeShader m_DofCombineCS;
        public ComputeShader dofCombineCS
        {
            get => m_DofCombineCS;
            set => this.SetValueAndNotify(ref m_DofCombineCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DoFComputeSlowTiles.compute")]
        private ComputeShader m_DofComputeSlowTilesCS;
        public ComputeShader dofComputeSlowTilesCS
        {
            get => m_DofComputeSlowTilesCS;
            set => this.SetValueAndNotify(ref m_DofComputeSlowTilesCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DoFApertureShape.compute")]
        private ComputeShader m_DofComputeApertureShapeCS;
        public ComputeShader dofComputeApertureShapeCS
        {
            get => m_DofComputeApertureShapeCS;
            set => this.SetValueAndNotify(ref m_DofComputeApertureShapeCS, value);
        }

        [Header("Post-processing - Motion Blur")]
        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/MotionBlurMotionVecPrep.compute")]
        private ComputeShader m_MotionBlurMotionVecPrepCS;
        public ComputeShader motionBlurMotionVecPrepCS
        {
            get => m_MotionBlurMotionVecPrepCS;
            set => this.SetValueAndNotify(ref m_MotionBlurMotionVecPrepCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/MotionBlurGenTilePass.compute")]
        private ComputeShader m_MotionBlurGenTileCS;
        public ComputeShader motionBlurGenTileCS
        {
            get => m_MotionBlurGenTileCS;
            set => this.SetValueAndNotify(ref m_MotionBlurGenTileCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/MotionBlurMergeTilePass.compute")]
        private ComputeShader m_MotionBlurMergeTileCS;
        public ComputeShader motionBlurMergeTileCS
        {
            get => m_MotionBlurMergeTileCS;
            set => this.SetValueAndNotify(ref m_MotionBlurMergeTileCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/MotionBlurNeighborhoodTilePass.compute")]
        private ComputeShader m_MotionBlurNeighborhoodTileCS;
        public ComputeShader motionBlurNeighborhoodTileCS
        {
            get => m_MotionBlurNeighborhoodTileCS;
            set => this.SetValueAndNotify(ref m_MotionBlurNeighborhoodTileCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/MotionBlur.compute")]
        private ComputeShader m_MotionBlurCS;
        public ComputeShader motionBlurCS
        {
            get => m_MotionBlurCS;
            set => this.SetValueAndNotify(ref m_MotionBlurCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/BloomPrefilter.compute")]
        private ComputeShader m_BloomPrefilterCS;
        public ComputeShader bloomPrefilterCS
        {
            get => m_BloomPrefilterCS;
            set => this.SetValueAndNotify(ref m_BloomPrefilterCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/BloomBlur.compute")]
        private ComputeShader m_BloomBlurCS;
        public ComputeShader bloomBlurCS
        {
            get => m_BloomBlurCS;
            set => this.SetValueAndNotify(ref m_BloomBlurCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/BloomUpsample.compute")]
        private ComputeShader m_BloomUpsampleCS;
        public ComputeShader bloomUpsampleCS
        {
            get => m_BloomUpsampleCS;
            set => this.SetValueAndNotify(ref m_BloomUpsampleCS, value);
        }

        [Header("Post-processing - AA")]
        [SerializeField, ResourcePath("Runtime/RenderPipeline/RenderPass/MSAA/DepthValues.shader")]
        private Shader m_DepthValuesPS;

        public Shader depthValuesPS
        {
            get => m_DepthValuesPS;
            set => this.SetValueAndNotify(ref m_DepthValuesPS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/RenderPass/MSAA/ColorResolve.shader")]
        private Shader m_ColorResolvePS;

        public Shader colorResolvePS
        {
            get => m_ColorResolvePS;
            set => this.SetValueAndNotify(ref m_ColorResolvePS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/RenderPass/MSAA/MotionVecResolve.shader")]
        private Shader m_ResolveMotionVecPS;

        public Shader resolveMotionVecPS
        {
            get => m_ResolveMotionVecPS;
            set => this.SetValueAndNotify(ref m_ResolveMotionVecPS, value);
        }
        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/FXAA.compute")]
        private ComputeShader m_FXAACS;
        public ComputeShader FXAACS
        {
            get => m_FXAACS;
            set => this.SetValueAndNotify(ref m_FXAACS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/FinalPass.shader")]
        private Shader m_FinalPassPS;
        public Shader finalPassPS
        {
            get => m_FinalPassPS;
            set => this.SetValueAndNotify(ref m_FinalPassPS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/ClearBlack.shader")]
        private Shader m_ClearBlackPS;
        public Shader clearBlackPS
        {
            get => m_ClearBlackPS;
            set => this.SetValueAndNotify(ref m_ClearBlackPS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/SubpixelMorphologicalAntialiasing.shader")]
        private Shader m_SMAAPS;
        public Shader SMAAPS
        {
            get => m_SMAAPS;
            set => this.SetValueAndNotify(ref m_SMAAPS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/TemporalAntialiasing.shader")]
        private Shader m_TemporalAntialiasingPS;
        public Shader temporalAntialiasingPS
        {
            get => m_TemporalAntialiasingPS;
            set => this.SetValueAndNotify(ref m_TemporalAntialiasingPS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/PostSharpenPass.compute")]
        private ComputeShader m_SharpeningCS;
        public ComputeShader sharpeningCS
        {
            get => m_SharpeningCS;
            set => this.SetValueAndNotify(ref m_SharpeningCS, value);
        }

        [Header("Post-processing - Lens Flares")]
        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/LensFlareDataDriven.shader")]
        private Shader m_LensFlareDataDrivenPS;
        public Shader lensFlareDataDrivenPS
        {
            get => m_LensFlareDataDrivenPS;
            set => this.SetValueAndNotify(ref m_LensFlareDataDrivenPS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/LensFlareScreenSpace.shader")]
        private Shader m_LensFlareScreenSpacePS;
        public Shader lensFlareScreenSpacePS
        {
            get => m_LensFlareScreenSpacePS;
            set => this.SetValueAndNotify(ref m_LensFlareScreenSpacePS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/LensFlareMergeOcclusionDataDriven.compute")]
        private ComputeShader m_LensFlareMergeOcclusionCS;
        public ComputeShader lensFlareMergeOcclusionCS
        {
            get => m_LensFlareMergeOcclusionCS;
            set => this.SetValueAndNotify(ref m_LensFlareMergeOcclusionCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/DLSSBiasColorMask.shader")]
        private Shader m_DLSSBiasColorMaskPS;
        public Shader DLSSBiasColorMaskPS
        {
            get => m_DLSSBiasColorMaskPS;
            set => this.SetValueAndNotify(ref m_DLSSBiasColorMaskPS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/CompositeWithUIAndOETF.shader")]
        private Shader m_CompositeUIAndOETFApplyPS;
        public Shader compositeUIAndOETFApplyPS
        {
            get => m_CompositeUIAndOETFApplyPS;
            set => this.SetValueAndNotify(ref m_CompositeUIAndOETFApplyPS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/ContrastAdaptiveSharpen.compute")]
        private ComputeShader m_ContrastAdaptiveSharpenCS;
        public ComputeShader contrastAdaptiveSharpenCS
        {
            get => m_ContrastAdaptiveSharpenCS;
            set => this.SetValueAndNotify(ref m_ContrastAdaptiveSharpenCS, value);
        }

        [SerializeField, ResourcePath("Runtime/PostProcessing/Shaders/EdgeAdaptiveSpatialUpsampling.compute")]
        private ComputeShader m_EdgeAdaptiveSpatialUpsamplingCS;
        public ComputeShader edgeAdaptiveSpatialUpsamplingCS
        {
            get => m_EdgeAdaptiveSpatialUpsamplingCS;
            set => this.SetValueAndNotify(ref m_EdgeAdaptiveSpatialUpsamplingCS, value);
        }

        [SerializeField, ResourcePath("Runtime/VirtualTexturing/Shaders/DownsampleVTFeedback.compute")]
        private ComputeShader m_VTFeedbackDownsample;
        public ComputeShader VTFeedbackDownsample
        {
            get => m_VTFeedbackDownsample;
            set => this.SetValueAndNotify(ref m_VTFeedbackDownsample, value);
        }
        // Accumulation
        [SerializeField, ResourcePath("Runtime/RenderPipeline/Accumulation/Shaders/Accumulation.compute")]
        private ComputeShader m_AccumulationCS;
        public ComputeShader accumulationCS
        {
            get => m_AccumulationCS;
            set => this.SetValueAndNotify(ref m_AccumulationCS, value);
        }

        [SerializeField, ResourcePath("Runtime/RenderPipeline/Accumulation/Shaders/BlitAndExpose.compute")]
        private ComputeShader m_BlitAndExposeCS;
        public ComputeShader blitAndExposeCS
        {
            get => m_BlitAndExposeCS;
            set => this.SetValueAndNotify(ref m_BlitAndExposeCS, value);
        }

        // Compositor
        [SerializeField, ResourcePath("Runtime/Compositor/Shaders/AlphaInjection.shader")]
        private Shader m_AlphaInjectionPS;
        public Shader alphaInjectionPS
        {
            get => m_AlphaInjectionPS;
            set => this.SetValueAndNotify(ref m_AlphaInjectionPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Compositor/Shaders/ChromaKeying.shader")]
        private Shader m_ChromaKeyingPS;
        public Shader chromaKeyingPS
        {
            get => m_ChromaKeyingPS;
            set => this.SetValueAndNotify(ref m_ChromaKeyingPS, value);
        }

        [SerializeField, ResourcePath("Runtime/Compositor/Shaders/CustomClear.shader")]
        private Shader m_CustomClearPS;
        public Shader customClearPS
        {
            get => m_CustomClearPS;
            set => this.SetValueAndNotify(ref m_CustomClearPS, value);
        }

        #endregion

#if UNITY_EDITOR
        public void EnsureShadersCompiled()
        {
            void CheckComputeShaderMessages(ComputeShader computeShader)
            {
                foreach (var message in UnityEditor.ShaderUtil.GetComputeShaderMessages(computeShader))
                {
                    if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                    {
                        // Will be catched by the try in HDRenderPipelineAsset.CreatePipeline()
                        throw new System.Exception(System.String.Format(
                            "Compute Shader compilation error on platform {0} in file {1}:{2}: {3}{4}\n" +
                            "HDRP will not run until the error is fixed.\n",
                            message.platform, message.file, message.line, message.message, message.messageDetails
                        ));
                    }
                }
            }

            // We iterate over all compute shader to verify if they are all compiled, if it's not the case then
            // we throw an exception to avoid allocating resources and crashing later on by using a null compute kernel.
            this.ForEachFieldOfType<ComputeShader>(CheckComputeShaderMessages, BindingFlags.Public | BindingFlags.Instance);
        }
#endif
        public bool IsDefaultShaderValid(out string message)
        {
            message = string.Empty;
            if (defaultShader == null)
            {
                message = "Unable to find default Shader";
            }
            else if (defaultShader.isSupported == false)
            {
                message = $"Unable to compile {defaultShader.name}. Either there is a compile error in Lit.shader or the current platform / API isn't compatible.";
            }

            return string.IsNullOrEmpty(message);
        }

    }
}
