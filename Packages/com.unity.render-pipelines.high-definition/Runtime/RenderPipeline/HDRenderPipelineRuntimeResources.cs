using System;
using System.Reflection;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    [HDRPHelpURL("Default-Settings-Window")]
    partial class HDRenderPipelineRuntimeResources : HDRenderPipelineResources
    {
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            // Defaults
            [Reload("Runtime/Material/Lit/Lit.shader")]
            public Shader defaultPS;

            // Debug
            [Reload("Runtime/Debug/DebugDisplayLatlong.Shader")]
            public Shader debugDisplayLatlongPS;
            [Reload("Runtime/Debug/DebugViewMaterialGBuffer.Shader")]
            public Shader debugViewMaterialGBufferPS;
            [Reload("Runtime/Debug/DebugViewTiles.Shader")]
            public Shader debugViewTilesPS;
            [Reload("Runtime/Debug/DebugFullScreen.Shader")]
            public Shader debugFullScreenPS;
            [Reload("Runtime/Debug/DebugColorPicker.Shader")]
            public Shader debugColorPickerPS;
            [Reload("Runtime/Debug/DebugExposure.Shader")]
            public Shader debugExposurePS;
            [Reload("Runtime/Debug/DebugHDR.Shader")]
            public Shader debugHDRPS;
            [Reload("Runtime/Debug/DebugLightVolumes.Shader")]
            public Shader debugLightVolumePS;
            [Reload("Runtime/Debug/DebugLightVolumes.compute")]
            public ComputeShader debugLightVolumeCS;
            [Reload("Runtime/Debug/DebugBlitQuad.Shader")]
            public Shader debugBlitQuad;
            [Reload("Runtime/Debug/DebugVTBlit.Shader")]
            public Shader debugViewVirtualTexturingBlit;
            [Reload("Runtime/Debug/MaterialError.Shader")]
            public Shader materialError;
            [Reload("Runtime/Debug/MaterialLoading.shader")]
            public Shader materialLoading;
            [Reload("Runtime/Debug/ClearDebugBuffer.compute")]
            public ComputeShader clearDebugBufferCS;

            [Reload("Runtime/Debug/DebugWaveform.shader")]
            public Shader debugWaveformPS;
            [Reload("Runtime/Debug/DebugWaveform.compute")]
            public ComputeShader debugWaveformCS;
            [Reload("Runtime/Debug/DebugVectorscope.shader")]
            public Shader debugVectorscopePS;
            [Reload("Runtime/Debug/DebugVectorscope.compute")]
            public ComputeShader debugVectorscopeCS;

            [Reload("Runtime/Lighting/VolumetricLighting/DebugLocalVolumetricFogAtlas.shader")]
            public Shader debugLocalVolumetricFogAtlasPS;

            // APV
            [Reload("Runtime/Lighting/ProbeVolume/ProbeVolumeBlendStates.compute")]
            public ComputeShader probeVolumeBlendStatesCS;
            [Reload("Runtime/Lighting/ProbeVolume/ProbeVolumeUploadData.compute")]
            public ComputeShader probeVolumeUploadDataCS;
            [Reload("Runtime/Lighting/ProbeVolume/ProbeVolumeUploadDataL2.compute")]
            public ComputeShader probeVolumeUploadDataL2CS;

            // APV Debug
            [Reload("Runtime/Debug/ProbeVolumeDebug.shader")]
            public Shader probeVolumeDebugShader;
            [Reload("Runtime/Debug/ProbeVolumeFragmentationDebug.shader")]
            public Shader probeVolumeFragmentationDebugShader;
            [Reload("Runtime/Debug/ProbeVolumeSamplingDebug.shader")]
            public Shader probeVolumeSamplingDebugShader;
            [Reload("Runtime/Debug/ProbeVolumeSamplingDebugPositionNormal.compute")]
            public ComputeShader probeVolumeSamplingDebugComputeShader;
            [Reload("Runtime/Debug/ProbeVolumeOffsetDebug.shader")]
            public Shader probeVolumeOffsetDebugShader;

            // Lighting
            [Reload("Runtime/Lighting/Deferred.Shader")]
            public Shader deferredPS;
            [Reload("Runtime/Lighting/PlanarReflectionFiltering.compute")]
            public ComputeShader planarReflectionFilteringCS;

            // Lighting screen space
            [Reload("Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceGlobalIllumination.compute")]
            public ComputeShader screenSpaceGlobalIlluminationCS;
            [Reload("Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceReflections.compute")]
            public ComputeShader screenSpaceReflectionsCS;

            // Lighting tile pass
            [Reload("Runtime/Lighting/LightLoop/cleardispatchindirect.compute")]
            public ComputeShader clearDispatchIndirectCS;
            [Reload("Runtime/Lighting/LightLoop/ClearLightLists.compute")]
            public ComputeShader clearLightListsCS;
            [Reload("Runtime/Lighting/LightLoop/builddispatchindirect.compute")]
            public ComputeShader buildDispatchIndirectCS;
            [Reload("Runtime/Lighting/LightLoop/scrbound.compute")]
            public ComputeShader buildScreenAABBCS;
            [Reload("Runtime/Lighting/LightLoop/lightlistbuild.compute")]
            public ComputeShader buildPerTileLightListCS;               // FPTL
            [Reload("Runtime/Lighting/LightLoop/lightlistbuild-bigtile.compute")]
            public ComputeShader buildPerBigTileLightListCS;
            [Reload("Runtime/Lighting/LightLoop/lightlistbuild-clustered.compute")]
            public ComputeShader buildPerVoxelLightListCS;              // clustered
            [Reload("Runtime/Lighting/LightLoop/lightlistbuild-clearatomic.compute")]
            public ComputeShader lightListClusterClearAtomicIndexCS;
            [Reload("Runtime/Lighting/LightLoop/materialflags.compute")]
            public ComputeShader buildMaterialFlagsCS;
            [Reload("Runtime/Lighting/LightLoop/Deferred.compute")]
            public ComputeShader deferredCS;
            [Reload("Runtime/Lighting/LightLoop/DeferredTile.shader")]
            public Shader deferredTilePS;

            // Volumetric Fog
            [Reload("Runtime/Lighting/VolumetricLighting/VolumeVoxelization.compute")]
            public ComputeShader volumeVoxelizationCS;
            [Reload("Runtime/Lighting/VolumetricLighting/VolumetricLighting.compute")]
            public ComputeShader volumetricLightingCS;
            [Reload("Runtime/Lighting/VolumetricLighting/VolumetricLightingFiltering.compute")]
            public ComputeShader volumetricLightingFilteringCS;

            // SSS
            [Reload("Runtime/Material/SubsurfaceScattering/SubsurfaceScattering.compute")]
            public ComputeShader subsurfaceScatteringCS;                // Disney SSS
            [Reload("Runtime/Material/SubsurfaceScattering/RandomDownsample.compute")]
            public ComputeShader subsurfaceScatteringDownsampleCS;
            [Reload("Runtime/Material/SubsurfaceScattering/CombineLighting.shader")]
            public Shader combineLightingPS;

            // General
            [Reload("Runtime/RenderPipeline/RenderPass/MotionVectors/CameraMotionVectors.shader")]
            public Shader cameraMotionVectorsPS;
            [Reload("Runtime/RenderPipeline/RenderPass/ColorPyramidPS.Shader")]
            public Shader colorPyramidPS;
            [Reload("Runtime/RenderPipeline/RenderPass/DepthPyramid.compute")]
            public ComputeShader depthPyramidCS;
            [Reload("Runtime/RenderPipeline/RenderPass/GenerateMaxZ.compute")]
            public ComputeShader maxZCS;
            [Reload("Runtime/RenderPipeline/RenderPass/Distortion/ApplyDistortion.shader")]
            public Shader applyDistortionPS;
            [Reload("Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassUtils.shader")]
            public Shader customPassUtils;
            [Reload("Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderersUtils.shader")]
            public Shader customPassRenderersUtils;

            [Reload("Runtime/ShaderLibrary/ClearStencilBuffer.shader")]
            public Shader clearStencilBufferPS;
            [Reload("Runtime/ShaderLibrary/CopyStencilBuffer.shader")]
            public Shader copyStencilBufferPS;
            [Reload("Runtime/ShaderLibrary/CopyDepthBuffer.shader")]
            public Shader copyDepthBufferPS;
            [Reload("Runtime/ShaderLibrary/Blit.shader")]
            public Shader blitPS;
            [Reload("Runtime/ShaderLibrary/BlitColorAndDepth.shader")]
            public Shader blitColorAndDepthPS;
            [Reload("Runtime/ShaderLibrary/DownsampleDepth.shader")]
            public Shader downsampleDepthPS;
            [Reload("Runtime/ShaderLibrary/UpsampleTransparent.shader")]
            public Shader upsampleTransparentPS;
            [Reload("Runtime/ShaderLibrary/ResolveStencilBuffer.compute")]
            public ComputeShader resolveStencilCS;

            // Sky
            [Reload("Runtime/Sky/BlitCubemap.shader")]
            public Shader blitCubemapPS;
            [Reload("Runtime/Lighting/AtmosphericScattering/OpaqueAtmosphericScattering.shader")]
            public Shader opaqueAtmosphericScatteringPS;
            [Reload("Runtime/Sky/HDRISky/HDRISky.shader")]
            public Shader hdriSkyPS;
            [Reload("Runtime/Sky/HDRISky/IntegrateHDRISky.shader")]
            public Shader integrateHdriSkyPS;
            [Reload("Skybox/Cubemap", ReloadAttribute.Package.Builtin)]
            public Shader skyboxCubemapPS;
            [Reload("Runtime/Sky/GradientSky/GradientSky.shader")]
            public Shader gradientSkyPS;
            [Reload("Runtime/Sky/AmbientProbeConvolution.compute")]
            public ComputeShader ambientProbeConvolutionCS;
            [Reload("Runtime/Sky/PhysicallyBasedSky/GroundIrradiancePrecomputation.compute")]
            public ComputeShader groundIrradiancePrecomputationCS;
            [Reload("Runtime/Sky/PhysicallyBasedSky/InScatteredRadiancePrecomputation.compute")]
            public ComputeShader inScatteredRadiancePrecomputationCS;
            [Reload("Runtime/Sky/PhysicallyBasedSky/SkyLUTGenerator.compute")]
            public ComputeShader skyLUTGenerator;
            [Reload("Runtime/Sky/PhysicallyBasedSky/PhysicallyBasedSky.shader")]
            public Shader physicallyBasedSkyPS;
            [Reload("Runtime/Sky/CloudSystem/CloudLayer/CloudLayer.shader")]
            public Shader cloudLayerPS;
            [Reload("Runtime/Sky/CloudSystem/CloudLayer/BakeCloudTexture.compute")]
            public ComputeShader bakeCloudTextureCS;
            [Reload("Runtime/Sky/CloudSystem/CloudLayer/BakeCloudShadows.compute")]
            public ComputeShader bakeCloudShadowsCS;

            // Volumetric Clouds
            [Reload("Runtime/Lighting/VolumetricLighting/VolumetricClouds.compute")]
            public ComputeShader volumetricCloudsCS;
            [Reload("Runtime/Lighting/VolumetricClouds/VolumetricCloudsTrace.compute")]
            public ComputeShader volumetricCloudsTraceCS;
            [Reload("Runtime/Lighting/VolumetricClouds/VolumetricCloudsTraceShadows.compute")]
            public ComputeShader volumetricCloudsTraceShadowsCS;
            [Reload("Runtime/Lighting/VolumetricClouds/VolumetricCloudsShadowFilter.compute")]
            public ComputeShader volumetricCloudsShadowFilterCS;
            [Reload("Editor/Lighting/VolumetricClouds/CloudMapGenerator.compute")]
            public ComputeShader volumetricCloudMapGeneratorCS;
            [Reload("Runtime/Lighting/VolumetricLighting/VolumetricCloudsCombine.shader")]
            public Shader volumetricCloudsCombinePS;

            // Water
            [Reload("Runtime/Water/Shaders/WaterSimulation.compute")]
            public ComputeShader waterSimulationCS;
            [Reload("Runtime/Water/Shaders/FourierTransform.compute")]
            public ComputeShader fourierTransformCS;
            [Reload("Runtime/Water/Shaders/WaterEvaluation.compute")]
            public ComputeShader waterEvaluationCS;
            [Reload("Runtime/RenderPipelineResources/ShaderGraph/Water.shadergraph")]
            public Shader waterPS;
            [Reload("Runtime/Water/Shaders/WaterLighting.compute")]
            public ComputeShader waterLightingCS;
            [Reload("Runtime/Water/Shaders/WaterLine.compute")]
            public ComputeShader waterLineCS;
            [Reload("Runtime/Water/Shaders/WaterCaustics.shader")]
            public Shader waterCausticsPS;
            [Reload("Runtime/Water/Shaders/WaterDeformation.shader")]
            public Shader waterDeformationPS;
            [Reload("Runtime/Water/Shaders/WaterDeformation.compute")]
            public ComputeShader waterDeformationCS;
            [Reload("Runtime/Water/Shaders/WaterFoam.shader")]
            public Shader waterFoamPS;
            [Reload("Runtime/Water/Shaders/WaterFoam.compute")]
            public ComputeShader waterFoamCS;

            // Line Rendering
            // NOTE: Move this to Core SRP once a 'core resource' concept exists.
            [Reload("Runtime/RenderPipeline/LineRendering/Kernels/StagePrepare.compute")]
            public ComputeShader lineStagePrepareCS;
            [Reload("Runtime/RenderPipeline/LineRendering/Kernels/StageSetupSegment.compute")]
            public ComputeShader lineStageSetupSegmentCS;
            [Reload("Runtime/RenderPipeline/LineRendering/Kernels/StageShadingSetup.compute")]
            public ComputeShader lineStageShadingSetupCS;
            [Reload("Runtime/RenderPipeline/LineRendering/Kernels/StageRasterBin.compute")]
            public ComputeShader lineStageRasterBinCS;
            [Reload("Runtime/RenderPipeline/LineRendering/Kernels/StageWorkQueue.compute")]
            public ComputeShader lineStageWorkQueueCS;
            [Reload("Runtime/RenderPipeline/LineRendering/Kernels/StageRasterFine.compute")]
            public ComputeShader lineStageRasterFineCS;
            [Reload("Runtime/RenderPipeline/LineRendering/CompositeLines.shader")]
            public Shader lineCompositePS;

            // Prefix Sum
            [Reload("Runtime/Utilities/GPUPrefixSum/GPUPrefixSum.compute")]
            public ComputeShader gpuPrefixSumCS;

            // Copy
            [Reload("Runtime/Utilities/GPUSort/GPUSort.compute")]
            public ComputeShader gpuSortCS;

            // Material
            [Reload("Runtime/Material/PreIntegratedFGD/PreIntegratedFGD_GGXDisneyDiffuse.shader")]
            public Shader preIntegratedFGD_GGXDisneyDiffusePS;
            [Reload("Runtime/Material/PreIntegratedFGD/PreIntegratedFGD_CharlieFabricLambert.shader")]
            public Shader preIntegratedFGD_CharlieFabricLambertPS;
            [Reload("Runtime/Material/AxF/PreIntegratedFGD_Ward.shader")]
            public Shader preIntegratedFGD_WardPS;
            [Reload("Runtime/Material/AxF/PreIntegratedFGD_CookTorrance.shader")]
            public Shader preIntegratedFGD_CookTorrancePS;
            [Reload("Runtime/Material/PreIntegratedFGD/PreIntegratedFGD_Marschner.shader")]
            public Shader preIntegratedFGD_MarschnerPS;
            [Reload("Runtime/Material/Hair/MultipleScattering/HairMultipleScatteringPreIntegration.compute")]
            public ComputeShader preIntegratedFiberScatteringCS;
            [Reload("Runtime/Material/VolumetricMaterial/VolumetricMaterial.compute")]
            public ComputeShader volumetricMaterialCS;
            [Reload("Runtime/Material/Eye/EyeCausticLUTGen.compute")]
            public ComputeShader eyeMaterialCS;
            [Reload("Runtime/Material/LTCAreaLight/FilterAreaLightCookies.shader")]
            public Shader filterAreaLightCookiesPS;
            [Reload("Runtime/Material/GGXConvolution/BuildProbabilityTables.compute")]
            public ComputeShader buildProbabilityTablesCS;
            [Reload("Runtime/Material/GGXConvolution/ComputeGgxIblSampleData.compute")]
            public ComputeShader computeGgxIblSampleDataCS;
            [Reload("Runtime/Material/GGXConvolution/GGXConvolve.shader")]
            public Shader GGXConvolvePS;
            [Reload("Runtime/Material/Fabric/CharlieConvolve.shader")]
            public Shader charlieConvolvePS;

            // Utilities / Core
            [Reload("Runtime/Core/CoreResources/GPUCopy.compute")]
            public ComputeShader copyChannelCS;
            [Reload("Runtime/Core/CoreResources/ClearBuffer2D.compute")]
            public ComputeShader clearBuffer2D;
            [Reload("Runtime/Core/CoreResources/EncodeBC6H.compute")]
            public ComputeShader encodeBC6HCS;
            [Reload("Runtime/Core/CoreResources/CubeToPano.shader")]
            public Shader cubeToPanoPS;
            [Reload("Runtime/Core/CoreResources/BlitCubeTextureFace.shader")]
            public Shader blitCubeTextureFacePS;
            [Reload("Runtime/Core/CoreResources/ClearUIntTextureArray.compute")]
            public ComputeShader clearUIntTextureCS;
            [Reload("Runtime/RenderPipeline/Utility/Texture3DAtlas.compute")]
            public ComputeShader texture3DAtlasCS;

            // XR
            [Reload("Runtime/ShaderLibrary/XRMirrorView.shader")]
            public Shader xrMirrorViewPS;
            [Reload("Runtime/ShaderLibrary/XROcclusionMesh.shader")]
            public Shader xrOcclusionMeshPS;

            // Shadow
            [Reload("Runtime/Lighting/Shadow/ContactShadows.compute")]
            public ComputeShader contactShadowCS;
            [Reload("Runtime/Lighting/Shadow/ScreenSpaceShadows.shader")]
            public Shader screenSpaceShadowPS;
            [Reload("Runtime/Lighting/Shadow/ShadowClear.shader")]
            public Shader shadowClearPS;
            [Reload("Runtime/Lighting/Shadow/EVSMBlur.compute")]
            public ComputeShader evsmBlurCS;
            [Reload("Runtime/Lighting/Shadow/DebugDisplayHDShadowMap.shader")]
            public Shader debugHDShadowMapPS;
            [Reload("Runtime/Lighting/Shadow/MomentShadows.compute")]
            public ComputeShader momentShadowsCS;
            [Reload("Runtime/Lighting/Shadow/ShadowBlit.shader")]
            public Shader shadowBlitPS;

            // Decal
            [Reload("Runtime/Material/Decal/DecalNormalBuffer.shader")]
            public Shader decalNormalBufferPS;

            // Compute Thickness
            [Reload("Runtime/RenderPipeline/ShaderPass/ComputeThickness.shader")]
            public Shader ComputeThicknessPS;

            // Ambient occlusion
            [Reload("Runtime/Lighting/ScreenSpaceLighting/GTAO.compute")]
            public ComputeShader GTAOCS;
            [Reload("Runtime/Lighting/ScreenSpaceLighting/GTAOSpatialDenoise.compute")]
            public ComputeShader GTAOSpatialDenoiseCS;
            [Reload("Runtime/Lighting/ScreenSpaceLighting/GTAOTemporalDenoise.compute")]
            public ComputeShader GTAOTemporalDenoiseCS;
            [Reload("Runtime/Lighting/ScreenSpaceLighting/GTAOCopyHistory.compute")]
            public ComputeShader GTAOCopyHistoryCS;
            [Reload("Runtime/Lighting/ScreenSpaceLighting/GTAOBlurAndUpsample.compute")]
            public ComputeShader GTAOBlurAndUpsample;

            // MSAA Shaders
            [Reload("Runtime/RenderPipeline/RenderPass/MSAA/DepthValues.shader")]
            public Shader depthValuesPS;
            [Reload("Runtime/RenderPipeline/RenderPass/MSAA/ColorResolve.shader")]
            public Shader colorResolvePS;
            [Reload("Runtime/RenderPipeline/RenderPass/MSAA/MotionVecResolve.shader")]
            public Shader resolveMotionVecPS;

            // Post-processing
            [Reload("Runtime/PostProcessing/Shaders/AlphaCopy.compute")]
            public ComputeShader copyAlphaCS;
            [Reload("Runtime/PostProcessing/Shaders/NaNKiller.compute")]
            public ComputeShader nanKillerCS;
            [Reload("Runtime/PostProcessing/Shaders/Exposure.compute")]
            public ComputeShader exposureCS;
            [Reload("Runtime/PostProcessing/Shaders/HistogramExposure.compute")]
            public ComputeShader histogramExposureCS;
            [Reload("Runtime/PostProcessing/Shaders/ApplyExposure.compute")]
            public ComputeShader applyExposureCS;
            [Reload("Runtime/PostProcessing/Shaders/DebugHistogramImage.compute")]
            public ComputeShader debugImageHistogramCS;
            [Reload("Runtime/PostProcessing/Shaders/DebugHDRxyMapping.compute")]
            public ComputeShader debugHDRxyMappingCS;
            [Reload("Runtime/PostProcessing/Shaders/UberPost.compute")]
            public ComputeShader uberPostCS;
            [Reload("Runtime/PostProcessing/Shaders/LutBuilder3D.compute")]
            public ComputeShader lutBuilder3DCS;
            [Reload("Runtime/PostProcessing/Shaders/DepthOfFieldKernel.compute")]
            public ComputeShader depthOfFieldKernelCS;
            [Reload("Runtime/PostProcessing/Shaders/DepthOfFieldCoC.compute")]
            public ComputeShader depthOfFieldCoCCS;
            [Reload("Runtime/PostProcessing/Shaders/DepthOfFieldCoCReproject.compute")]
            public ComputeShader depthOfFieldCoCReprojectCS;
            [Reload("Runtime/PostProcessing/Shaders/DepthOfFieldCoCDilate.compute")]
            public ComputeShader depthOfFieldDilateCS;
            [Reload("Runtime/PostProcessing/Shaders/DepthOfFieldMip.compute")]
            public ComputeShader depthOfFieldMipCS;
            [Reload("Runtime/PostProcessing/Shaders/DepthOfFieldMipSafe.compute")]
            public ComputeShader depthOfFieldMipSafeCS;
            [Reload("Runtime/PostProcessing/Shaders/DepthOfFieldPrefilter.compute")]
            public ComputeShader depthOfFieldPrefilterCS;
            [Reload("Runtime/PostProcessing/Shaders/DepthOfFieldTileMax.compute")]
            public ComputeShader depthOfFieldTileMaxCS;
            [Reload("Runtime/PostProcessing/Shaders/DepthOfFieldGather.compute")]
            public ComputeShader depthOfFieldGatherCS;
            [Reload("Runtime/PostProcessing/Shaders/DepthOfFieldCombine.compute")]
            public ComputeShader depthOfFieldCombineCS;
            [Reload("Runtime/PostProcessing/Shaders/DepthOfFieldPreCombineFar.compute")]
            public ComputeShader depthOfFieldPreCombineFarCS;
            [Reload("Runtime/PostProcessing/Shaders/DepthOfFieldClearIndirectArgs.compute")]
            public ComputeShader depthOfFieldClearIndirectArgsCS;
            [Reload("Runtime/PostProcessing/Shaders/PaniniProjection.compute")]
            public ComputeShader paniniProjectionCS;
            [Reload("Runtime/PostProcessing/Shaders/MotionBlurMotionVecPrep.compute")]
            public ComputeShader motionBlurMotionVecPrepCS;
            [Reload("Runtime/PostProcessing/Shaders/MotionBlurGenTilePass.compute")]
            public ComputeShader motionBlurGenTileCS;
            [Reload("Runtime/PostProcessing/Shaders/MotionBlurMergeTilePass.compute")]
            public ComputeShader motionBlurMergeTileCS;
            [Reload("Runtime/PostProcessing/Shaders/MotionBlurNeighborhoodTilePass.compute")]
            public ComputeShader motionBlurNeighborhoodTileCS;
            [Reload("Runtime/PostProcessing/Shaders/MotionBlur.compute")]
            public ComputeShader motionBlurCS;
            [Reload("Runtime/PostProcessing/Shaders/BloomPrefilter.compute")]
            public ComputeShader bloomPrefilterCS;
            [Reload("Runtime/PostProcessing/Shaders/BloomBlur.compute")]
            public ComputeShader bloomBlurCS;
            [Reload("Runtime/PostProcessing/Shaders/BloomUpsample.compute")]
            public ComputeShader bloomUpsampleCS;
            [Reload("Runtime/PostProcessing/Shaders/FXAA.compute")]
            public ComputeShader FXAACS;
            [Reload("Runtime/PostProcessing/Shaders/FinalPass.shader")]
            public Shader finalPassPS;
            [Reload("Runtime/PostProcessing/Shaders/ClearBlack.shader")]
            public Shader clearBlackPS;
            [Reload("Runtime/PostProcessing/Shaders/SubpixelMorphologicalAntialiasing.shader")]
            public Shader SMAAPS;
            [Reload("Runtime/PostProcessing/Shaders/TemporalAntialiasing.shader")]
            public Shader temporalAntialiasingPS;
            [Reload("Runtime/PostProcessing/Shaders/PostSharpenPass.compute")]
            public ComputeShader sharpeningCS;
            [Reload("Runtime/PostProcessing/Shaders/LensFlareDataDriven.shader")]
            public Shader lensFlareDataDrivenPS;
            [Reload("Runtime/PostProcessing/Shaders/LensFlareScreenSpace.shader")]
            public Shader lensFlareScreenSpacePS;
            [Reload("Runtime/PostProcessing/Shaders/LensFlareMergeOcclusionDataDriven.compute")]
            public ComputeShader lensFlareMergeOcclusionCS;
            [Reload("Runtime/PostProcessing/Shaders/DLSSBiasColorMask.shader")]
            public Shader DLSSBiasColorMaskPS;
            [Reload("Runtime/PostProcessing/Shaders/CompositeWithUIAndOETF.shader")]
            public Shader compositeUIAndOETFApplyPS;

            // Physically based DoF
            [Reload("Runtime/PostProcessing/Shaders/DoFCircleOfConfusion.compute")]
            public ComputeShader dofCircleOfConfusion;
            [Reload("Runtime/PostProcessing/Shaders/DoFGather.compute")]
            public ComputeShader dofGatherCS;
            [Reload("Runtime/PostProcessing/Shaders/DoFCoCMinMax.compute")]
            public ComputeShader dofCoCMinMaxCS;
            [Reload("Runtime/PostProcessing/Shaders/DoFMinMaxDilate.compute")]
            public ComputeShader dofMinMaxDilateCS;
            [Reload("Runtime/PostProcessing/Shaders/DoFCombine.compute")]
            public ComputeShader dofCombineCS;

            [Reload("Runtime/PostProcessing/Shaders/ContrastAdaptiveSharpen.compute")]
            public ComputeShader contrastAdaptiveSharpenCS;
            [Reload("Runtime/PostProcessing/Shaders/EdgeAdaptiveSpatialUpsampling.compute")]
            public ComputeShader edgeAdaptiveSpatialUpsamplingCS;
            [Reload("Runtime/VirtualTexturing/Shaders/DownsampleVTFeedback.compute")]
            public ComputeShader VTFeedbackDownsample;

            // Accumulation
            [Reload("Runtime/RenderPipeline/Accumulation/Shaders/Accumulation.compute")]
            public ComputeShader accumulationCS;

            [Reload("Runtime/RenderPipeline/Accumulation/Shaders/BlitAndExpose.compute")]
            public ComputeShader blitAndExposeCS;

            // Compositor
            [Reload("Runtime/Compositor/Shaders/AlphaInjection.shader")]
            public Shader alphaInjectionPS;
            [Reload("Runtime/Compositor/Shaders/ChromaKeying.shader")]
            public Shader chromaKeyingPS;
            [Reload("Runtime/Compositor/Shaders/CustomClear.shader")]
            public Shader customClearPS;

            // Denoising
            [Reload("Runtime/Lighting/ScreenSpaceLighting/BilateralUpsample.compute")]
            public ComputeShader bilateralUpsampleCS;
            [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/TemporalFilter.compute")]
            public ComputeShader temporalFilterCS;
            [Reload("Runtime/RenderPipeline/Raytracing/Shaders/Denoising/DiffuseDenoiser.compute")]
            public ComputeShader diffuseDenoiserCS;

#if UNITY_EDITOR
            // Furnace Testing (BSDF Energy Conservation)
            [Reload("Tests/Editor/Utilities/FurnaceTests.compute")]
            public ComputeShader furnaceTestCS;
#endif
        }

        [Serializable, ReloadGroup]
        public sealed class ShaderGraphResources
        {
            [Reload("Runtime/ShaderLibrary/SolidColor.shadergraph")]
            public Shader objectIDPS;
            [Reload("Runtime/RenderPipelineResources/ShaderGraph/DefaultFogVolume.shadergraph")]
            public Shader defaultFogVolumeShader;
        }

        public ShaderResources shaders;
        public ShaderGraphResources shaderGraphs;
    }
}
