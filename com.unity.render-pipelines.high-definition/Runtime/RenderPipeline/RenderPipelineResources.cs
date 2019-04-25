using System;
using System.Reflection;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class RenderPipelineResources : ScriptableObject
    {
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            // Defaults
            [Reload("Material/Lit/Lit.shader")]
            public Shader defaultPS;

            // Debug
            [Reload("Debug/DebugDisplayLatlong.Shader")]
            public Shader debugDisplayLatlongPS;
            [Reload("Debug/DebugViewMaterialGBuffer.Shader")]
            public Shader debugViewMaterialGBufferPS;
            [Reload("Debug/DebugViewTiles.Shader")]
            public Shader debugViewTilesPS;
            [Reload("Debug/DebugFullScreen.Shader")]
            public Shader debugFullScreenPS;
            [Reload("Debug/DebugColorPicker.Shader")]
            public Shader debugColorPickerPS;
            [Reload("Debug/DebugLightVolumes.Shader")]
            public Shader debugLightVolumePS;
            [Reload("Debug/DebugLightVolumes.compute")]
            public ComputeShader debugLightVolumeCS;

            // Lighting
            [Reload("Lighting/Deferred.Shader")]
            public Shader deferredPS;
            [Reload("RenderPipeline/RenderPass/ColorPyramid.compute")]
            public ComputeShader colorPyramidCS;
            [Reload("RenderPipeline/RenderPass/ColorPyramidPS.Shader")]
            public Shader colorPyramidPS;
            [Reload("RenderPipeline/RenderPass/DepthPyramid.compute")]
            public ComputeShader depthPyramidCS;
            [Reload("CoreResources/GPUCopy.compute", ReloadAttribute.Package.CoreRuntime)]
            public ComputeShader copyChannelCS;
            [Reload("Lighting/ScreenSpaceLighting/ScreenSpaceReflections.compute")]
            public ComputeShader screenSpaceReflectionsCS;
            [Reload("RenderPipeline/RenderPass/Distortion/ApplyDistortion.shader")]
            public Shader applyDistortionPS;

            // Lighting tile pass
            [Reload("Lighting/LightLoop/cleardispatchindirect.compute")]
            public ComputeShader clearDispatchIndirectCS;
            [Reload("Lighting/LightLoop/builddispatchindirect.compute")]
            public ComputeShader buildDispatchIndirectCS;
            [Reload("Lighting/LightLoop/scrbound.compute")]
            public ComputeShader buildScreenAABBCS;
            [Reload("Lighting/LightLoop/lightlistbuild.compute")]
            public ComputeShader buildPerTileLightListCS;               // FPTL
            [Reload("Lighting/LightLoop/lightlistbuild-bigtile.compute")]
            public ComputeShader buildPerBigTileLightListCS;
            [Reload("Lighting/LightLoop/lightlistbuild-clustered.compute")]
            public ComputeShader buildPerVoxelLightListCS;              // clustered
            [Reload("Lighting/LightLoop/materialflags.compute")]
            public ComputeShader buildMaterialFlagsCS;
            [Reload("Lighting/LightLoop/Deferred.compute")]
            public ComputeShader deferredCS;
            [Reload("Lighting/Shadow/ScreenSpaceShadow.compute")]
            public ComputeShader screenSpaceShadowCS;
            [Reload("Lighting/VolumetricLighting/VolumeVoxelization.compute")]
            public ComputeShader volumeVoxelizationCS;
            [Reload("Lighting/VolumetricLighting/VolumetricLighting.compute")]
            public ComputeShader volumetricLightingCS;
            [Reload("Lighting/LightLoop/DeferredTile.shader")]
            public Shader deferredTilePS;

            [Reload("Material/SubsurfaceScattering/SubsurfaceScattering.compute")]
            public ComputeShader subsurfaceScatteringCS;                // Disney SSS
            [Reload("Material/SubsurfaceScattering/CombineLighting.shader")]
            public Shader combineLightingPS;

            // General
            [Reload("RenderPipeline/RenderPass/MotionVectors/CameraMotionVectors.shader")]
            public Shader cameraMotionVectorsPS;
            [Reload("ShaderLibrary/CopyStencilBuffer.shader")]
            public Shader copyStencilBufferPS;
            [Reload("ShaderLibrary/CopyDepthBuffer.shader")]
            public Shader copyDepthBufferPS;
            [Reload("ShaderLibrary/Blit.shader")]
            public Shader blitPS;

            [Reload("ShaderLibrary/DownsampleDepth.shader")]
            public Shader downsampleDepthPS;
            [Reload("ShaderLibrary/UpsampleTransparent.shader")]
            public Shader upsampleTransparentPS;

            // Sky
            [Reload("Sky/BlitCubemap.shader")]
            public Shader blitCubemapPS;
            [Reload("Material/GGXConvolution/BuildProbabilityTables.compute")]
            public ComputeShader buildProbabilityTablesCS;
            [Reload("Material/GGXConvolution/ComputeGgxIblSampleData.compute")]
            public ComputeShader computeGgxIblSampleDataCS;
            [Reload("Material/GGXConvolution/GGXConvolve.shader")]
            public Shader GGXConvolvePS;
            [Reload("Material/Fabric/CharlieConvolve.shader")]
            public Shader charlieConvolvePS;
            [Reload("Lighting/AtmosphericScattering/OpaqueAtmosphericScattering.shader")]
            public Shader opaqueAtmosphericScatteringPS;
            [Reload("Sky/HDRISky/HDRISky.shader")]
            public Shader hdriSkyPS;
            [Reload("Sky/HDRISky/IntegrateHDRISky.shader")]
            public Shader integrateHdriSkyPS;
            [Reload("Sky/ProceduralSky/ProceduralSky.shader")]
            public Shader proceduralSkyPS;
            [Reload("Skybox/Cubemap", ReloadAttribute.Package.Builtin)]
            public Shader skyboxCubemapPS;
            [Reload("Sky/GradientSky/GradientSky.shader")]
            public Shader gradientSkyPS;
            [Reload("Sky/AmbientProbeConvolution.compute")]
            public ComputeShader ambientProbeConvolutionCS;

            // Material
            [Reload("Material/PreIntegratedFGD/PreIntegratedFGD_GGXDisneyDiffuse.shader")]
            public Shader preIntegratedFGD_GGXDisneyDiffusePS;
            [Reload("Material/PreIntegratedFGD/PreIntegratedFGD_CharlieFabricLambert.shader")]
            public Shader preIntegratedFGD_CharlieFabricLambertPS;
            [Reload("Material/AxF/PreIntegratedFGD_Ward.shader")]
            public Shader preIntegratedFGD_WardPS;
            [Reload("Material/AxF/PreIntegratedFGD_CookTorrance.shader")]
            public Shader preIntegratedFGD_CookTorrancePS;

            // Utilities / Core
            [Reload("CoreResources/EncodeBC6H.compute", ReloadAttribute.Package.CoreRuntime)]
            public ComputeShader encodeBC6HCS;
            [Reload("CoreResources/CubeToPano.shader", ReloadAttribute.Package.CoreRuntime)]
            public Shader cubeToPanoPS;
            [Reload("CoreResources/BlitCubeTextureFace.shader", ReloadAttribute.Package.CoreRuntime)]
            public Shader blitCubeTextureFacePS;
            [Reload("Material/LTCAreaLight/FilterAreaLightCookies.shader")]
            public Shader filterAreaLightCookiesPS;

            // Shadow            
            [Reload("Lighting/Shadow/ShadowClear.shader")]
            public Shader shadowClearPS;
            [Reload("Lighting/Shadow/EVSMBlur.compute")]
            public ComputeShader evsmBlurCS;
            [Reload("Lighting/Shadow/DebugDisplayHDShadowMap.shader")]
            public Shader debugHDShadowMapPS;
            [Reload("Lighting/Shadow/MomentShadows.compute")]
            public ComputeShader momentShadowsCS;

            // Decal
            [Reload("Material/Decal/DecalNormalBuffer.shader")]
            public Shader decalNormalBufferPS;

            // Ambient occlusion
            [Reload("Lighting/ScreenSpaceLighting/AmbientOcclusionDownsample1.compute")]
            public ComputeShader aoDownsample1CS;
            [Reload("Lighting/ScreenSpaceLighting/AmbientOcclusionDownsample2.compute")]
            public ComputeShader aoDownsample2CS;
            [Reload("Lighting/ScreenSpaceLighting/AmbientOcclusionRender.compute")]
            public ComputeShader aoRenderCS;
            [Reload("Lighting/ScreenSpaceLighting/AmbientOcclusionUpsample.compute")]
            public ComputeShader aoUpsampleCS;
            [Reload("RenderPipeline/RenderPass/MSAA/AmbientOcclusionResolve.shader")]
            public Shader aoResolvePS;

            // MSAA Shaders
            [Reload("RenderPipeline/RenderPass/MSAA/DepthValues.shader")]
            public Shader depthValuesPS;
            [Reload("RenderPipeline/RenderPass/MSAA/ColorResolve.shader")]
            public Shader colorResolvePS;

            // Post-processing
            [Reload("PostProcessing/Shaders/NaNKiller.compute")]
            public ComputeShader nanKillerCS;
            [Reload("PostProcessing/Shaders/Exposure.compute")]
            public ComputeShader exposureCS;
            [Reload("PostProcessing/Shaders/UberPost.compute")]
            public ComputeShader uberPostCS;
            [Reload("PostProcessing/Shaders/LutBuilder3D.compute")]
            public ComputeShader lutBuilder3DCS;
            [Reload("PostProcessing/Shaders/TemporalAntialiasing.compute")]
            public ComputeShader temporalAntialiasingCS;
            [Reload("PostProcessing/Shaders/DepthOfFieldKernel.compute")]
            public ComputeShader depthOfFieldKernelCS;
            [Reload("PostProcessing/Shaders/DepthOfFieldCoC.compute")]
            public ComputeShader depthOfFieldCoCCS;
            [Reload("PostProcessing/Shaders/DepthOfFieldCoCReproject.compute")]
            public ComputeShader depthOfFieldCoCReprojectCS;
            [Reload("PostProcessing/Shaders/DepthOfFieldCoCDilate.compute")]
            public ComputeShader depthOfFieldDilateCS;
            [Reload("PostProcessing/Shaders/DepthOfFieldMip.compute")]
            public ComputeShader depthOfFieldMipCS;
            [Reload("PostProcessing/Shaders/DepthOfFieldMipSafe.compute")]
            public ComputeShader depthOfFieldMipSafeCS;
            [Reload("PostProcessing/Shaders/DepthOfFieldPrefilter.compute")]
            public ComputeShader depthOfFieldPrefilterCS;
            [Reload("PostProcessing/Shaders/DepthOfFieldTileMax.compute")]
            public ComputeShader depthOfFieldTileMaxCS;
            [Reload("PostProcessing/Shaders/DepthOfFieldGather.compute")]
            public ComputeShader depthOfFieldGatherCS;
            [Reload("PostProcessing/Shaders/DepthOfFieldCombine.compute")]
            public ComputeShader depthOfFieldCombineCS;
            [Reload("PostProcessing/Shaders/PaniniProjection.compute")]
            public ComputeShader paniniProjectionCS;
            [Reload("PostProcessing/Shaders/MotionBlurMotionVecPrep.compute")]
            public ComputeShader motionBlurMotionVecPrepCS;
            [Reload("PostProcessing/Shaders/MotionBlurTilePass.compute")]
            public ComputeShader motionBlurTileGenCS;
            [Reload("PostProcessing/Shaders/MotionBlur.compute")]
            public ComputeShader motionBlurCS;
            [Reload("PostProcessing/Shaders/BloomPrefilter.compute")]
            public ComputeShader bloomPrefilterCS;
            [Reload("PostProcessing/Shaders/BloomBlur.compute")]
            public ComputeShader bloomBlurCS;
            [Reload("PostProcessing/Shaders/BloomUpsample.compute")]
            public ComputeShader bloomUpsampleCS;
            [Reload("PostProcessing/Shaders/FXAA.compute")]
            public ComputeShader FXAACS;
            [Reload("PostProcessing/Shaders/FinalPass.shader")]
            public Shader finalPassPS;
            [Reload("PostProcessing/Shaders/ClearBlack.shader")]
            public Shader clearBlackPS;
            [Reload("PostProcessing/Shaders/SubpixelMorphologicalAntialiasing.shader")]
            public Shader SMAAPS;


#if ENABLE_RAYTRACING
            // Reflection
            [Reload("RenderPipeline/Raytracing/Shaders/RaytracingReflections.raytrace")]
            public RaytracingShader reflectionRaytracing;
            [Reload("RenderPipeline/Raytracing/Shaders/RaytracingReflectionFilter.compute")]
            public ComputeShader reflectionBilateralFilterCS;

            // Shadows
            [Reload("RenderPipeline/Raytracing/Shaders/RaytracingAreaShadows.raytrace")]
            public RaytracingShader areaShadowsRaytracingRT;
            [Reload("RenderPipeline/Raytracing/Shaders/AreaShadows/RaytracingAreaShadows.compute")]
            public ComputeShader areaShadowRaytracingCS;
            [Reload("RenderPipeline/Raytracing/Shaders/AreaBilateralShadow.compute")]
            public ComputeShader areaShadowFilterCS;

            // Primary visibility
            [Reload("RenderPipeline/Raytracing/Shaders/RaytracingRenderer.raytrace")]
            public RaytracingShader forwardRaytracing;
            [Reload("RenderPipeline/Raytracing/Shaders/RaytracingFlagMask.raytrace")]
            public Shader           raytracingFlagMask;

            // Light cluster
            [Reload("RenderPipeline/Raytracing/Shaders/RaytracingLightCluster.compute")]
            public ComputeShader lightClusterBuildCS;
            [Reload("RenderPipeline/Raytracing/Shaders/DebugLightCluster.compute")]
            public ComputeShader lightClusterDebugCS;

            // Indirect Diffuse
            [Reload("RenderPipeline/Raytracing/Shaders/RaytracingIndirectDiffuse.raytrace")]
            public RaytracingShader indirectDiffuseRaytracing;
            [Reload("RenderPipeline/Raytracing/Shaders/RaytracingAccumulation.compute")]
            public ComputeShader indirectDiffuseAccumulation;            

            // Ambient Occlusion
            [Reload("RenderPipeline/Raytracing/Shaders/RaytracingAmbientOcclusion.raytrace")]
            public RaytracingShader aoRaytracing;
            [Reload("RenderPipeline/Raytracing/Shaders/RaytracingAmbientOcclusionFilter.compute")]
            public ComputeShader raytracingAOFilterCS;

            // Ray count
            [Reload("RenderPipeline/Raytracing/Shaders/CountTracedRays.compute")]
            public ComputeShader countTracedRays;
#endif

            // Iterator to retrieve all compute shaders in reflection so we don't have to keep a list of
            // used compute shaders up to date (prefer editor-only usage)
            public IEnumerable<ComputeShader> GetAllComputeShaders()
            {
                var fields = typeof(ShaderResources).GetFields(BindingFlags.Public | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.GetValue(this) is ComputeShader computeShader)
                        yield return computeShader;
                }
            }
        }

        [Serializable, ReloadGroup]
        public sealed class MaterialResources
        {
            // Defaults
            [Reload("RenderPipelineResources/Material/DefaultHDMaterial.mat")]
            public Material defaultDiffuseMat;
            [Reload("RenderPipelineResources/Material/DefaultHDMirrorMaterial.mat")]
            public Material defaultMirrorMat;
            [Reload("RenderPipelineResources/Material/DefaultHDDecalMaterial.mat")]
            public Material defaultDecalMat;
            [Reload("RenderPipelineResources/Material/DefaultHDTerrainMaterial.mat")]
            public Material defaultTerrainMat;
        }

        [Serializable, ReloadGroup]
        public sealed class TextureResources
        {
            // Debug
            [Reload("RenderPipelineResources/Texture/DebugFont.tga")]
            public Texture2D debugFontTex;
            [Reload("Debug/ColorGradient.png")]
            public Texture2D colorGradient;
            
            // Pre-baked noise
            [Reload("RenderPipelineResources/Texture/BlueNoise16/L/LDR_LLL1_{0}.png", 0, 32)]
            public Texture2D[] blueNoise16LTex;
            [Reload("RenderPipelineResources/Texture/BlueNoise16/RGB/LDR_RGB1_{0}.png", 0, 32)]
            public Texture2D[] blueNoise16RGBTex;
            [Reload("RenderPipelineResources/Texture/CoherentNoise/OwenScrambledNoise.png")]
            public Texture2D owenScrambledTex;
            [Reload("RenderPipelineResources/Texture/CoherentNoise/ScrambleNoise.png")]
            public Texture2D scramblingTex;

            // Post-processing
            [Reload(new[]
            {
                "RenderPipelineResources/Texture/FilmGrain/Thin01.png",
                "RenderPipelineResources/Texture/FilmGrain/Thin02.png",
                "RenderPipelineResources/Texture/FilmGrain/Medium01.png",
                "RenderPipelineResources/Texture/FilmGrain/Medium02.png",
                "RenderPipelineResources/Texture/FilmGrain/Medium03.png",
                "RenderPipelineResources/Texture/FilmGrain/Medium04.png",
                "RenderPipelineResources/Texture/FilmGrain/Medium05.png",
                "RenderPipelineResources/Texture/FilmGrain/Medium06.png",
                "RenderPipelineResources/Texture/FilmGrain/Large01.png",
                "RenderPipelineResources/Texture/FilmGrain/Large02.png"
            })]
            public Texture2D[] filmGrainTex;
            [Reload("RenderPipelineResources/Texture/SMAA/SearchTex.tga")]
            public Texture2D   SMAASearchTex;
            [Reload("RenderPipelineResources/Texture/SMAA/AreaTex.tga")]
            public Texture2D   SMAAAreaTex;
        }

        [Serializable, ReloadGroup]
        public sealed class ShaderGraphResources
        {
        }

        [Serializable, ReloadGroup]
        public sealed class AssetResources
        {
            [Reload("RenderPipelineResources/defaultDiffusionProfile.asset")]
            public DiffusionProfileSettings defaultDiffusionProfile;
        }

        public ShaderResources shaders;
        public MaterialResources materials;
        public TextureResources textures;
        public ShaderGraphResources shaderGraphs;
        public AssetResources assets;
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(RenderPipelineResources))]
    class RenderPipelineResourcesEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Add a "Reload All" button in inspector when we are in developer's mode
            if (UnityEditor.EditorPrefs.GetBool("DeveloperMode")
                && GUILayout.Button("Reload All"))
            {
                var resources = target as RenderPipelineResources;
                resources.materials = null;
                resources.textures = null;
                resources.shaders = null;
                resources.shaderGraphs = null;
                ResourceReloader.ReloadAllNullIn(target);
            }
        }
    }
#endif
}
