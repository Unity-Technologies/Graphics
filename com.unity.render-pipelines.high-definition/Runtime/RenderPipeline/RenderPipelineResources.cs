using System;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class RenderPipelineResources : ScriptableObject
    {
        [Serializable]
        public sealed class ShaderResources
        {
            // Defaults
            public Shader defaultPS;

            // Debug
            public Shader debugDisplayLatlongPS;
            public Shader debugViewMaterialGBufferPS;
            public Shader debugViewTilesPS;
            public Shader debugFullScreenPS;
            public Shader debugColorPickerPS;
            public Shader debugLightVolumePS;
            public ComputeShader debugLightVolumeCS;

            // Lighting
            public Shader deferredPS;
            public ComputeShader colorPyramidCS;
            public Shader colorPyramidPS;
            public ComputeShader depthPyramidCS;
            public ComputeShader copyChannelCS;
            public ComputeShader screenSpaceReflectionsCS;
            public Shader applyDistortionPS;

            // Lighting tile pass
            public ComputeShader clearDispatchIndirectCS;
            public ComputeShader buildDispatchIndirectCS;
            public ComputeShader buildScreenAABBCS;
            public ComputeShader buildPerTileLightListCS;               // FPTL
            public ComputeShader buildPerBigTileLightListCS;
            public ComputeShader buildPerVoxelLightListCS;              // clustered
            public ComputeShader buildMaterialFlagsCS;
            public ComputeShader deferredCS;
            public ComputeShader screenSpaceShadowCS;
            public ComputeShader volumeVoxelizationCS;
            public ComputeShader volumetricLightingCS;
            public Shader deferredTilePS;

            public ComputeShader subsurfaceScatteringCS;                // Disney SSS
            public Shader combineLightingPS;

            // General
            public Shader cameraMotionVectorsPS;
            public Shader copyStencilBufferPS;
            public Shader copyDepthBufferPS;
            public Shader blitPS;

            // Sky
            public Shader blitCubemapPS;
            public ComputeShader buildProbabilityTablesCS;
            public ComputeShader computeGgxIblSampleDataCS;
            public Shader GGXConvolvePS;
            public Shader charlieConvolvePS;
            public Shader opaqueAtmosphericScatteringPS;
            public Shader hdriSkyPS;
            public Shader integrateHdriSkyPS;
            public Shader proceduralSkyPS;
            public Shader skyboxCubemapPS;
            public Shader gradientSkyPS;
            public ComputeShader ambientProbeConvolutionCS;

            // Material
            public Shader preIntegratedFGD_GGXDisneyDiffusePS;
            public Shader preIntegratedFGD_CharlieFabricLambertPS;
            public Shader preIntegratedFGD_WardPS;
            public Shader preIntegratedFGD_CookTorrancePS;

            // Utilities / Core
            public ComputeShader encodeBC6HCS;
            public Shader cubeToPanoPS;
            public Shader blitCubeTextureFacePS;
            public Shader filterAreaLightCookiesPS;
            
            // Shadow
            public Shader shadowClearPS;
            public ComputeShader evsmBlurCS;
            public Shader debugHDShadowMapPS;
            public ComputeShader momentShadowsCS;

            // Decal
            public Shader decalNormalBufferPS;

            // Ambient occlusion
            public ComputeShader aoDownsample1CS;
            public ComputeShader aoDownsample2CS;
            public ComputeShader aoRenderCS;
            public ComputeShader aoUpsampleCS;
            public Shader aoResolvePS;

            // MSAA Shaders
            public Shader depthValuesPS;
            public Shader colorResolvePS;

            // Post-processing
            public ComputeShader nanKillerCS;
            public ComputeShader exposureCS;
            public ComputeShader uberPostCS;
            public ComputeShader lutBuilder3DCS;
            public ComputeShader temporalAntialiasingCS;
            public ComputeShader depthOfFieldKernelCS;
            public ComputeShader depthOfFieldCoCCS;
            public ComputeShader depthOfFieldCoCReprojectCS;
            public ComputeShader depthOfFieldDilateCS;
            public ComputeShader depthOfFieldMipCS;
            public ComputeShader depthOfFieldMipSafeCS;
            public ComputeShader depthOfFieldPrefilterCS;
            public ComputeShader depthOfFieldTileMaxCS;
            public ComputeShader depthOfFieldGatherCS;
            public ComputeShader depthOfFieldCombineCS;
            public ComputeShader paniniProjectionCS;
            public ComputeShader motionBlurVelocityPrepCS;
            public ComputeShader motionBlurTileGenCS;
            public ComputeShader motionBlurCS;
            public ComputeShader bloomPrefilterCS;
            public ComputeShader bloomBlurCS;
            public ComputeShader bloomUpsampleCS;
            public ComputeShader FXAACS;
            public Shader finalPassPS;
            public Shader clearBlackPS;
            public Shader SMAAPS;


#if ENABLE_RAYTRACING
            // Raytracing shaders
            public RaytracingShader aoRaytracing;
            public RaytracingShader reflectionRaytracing;
            public RaytracingShader indirectDiffuseRaytracing;
            public RaytracingShader shadowsRaytracing;
            public Shader           raytracingFlagMask;
            public RaytracingShader forwardRaytracing;
            public ComputeShader areaBillateralFilterCS;
            public ComputeShader jointBilateralFilterCS;
            public ComputeShader reflectionBilateralFilterCS;
            public ComputeShader lightClusterBuildCS;
            public ComputeShader lightClusterDebugCS;
            public ComputeShader countTracedRays;
#endif
        }

        [Serializable]
        public sealed class MaterialResources
        {
            // Defaults
            public Material defaultDiffuseMat;
            public Material defaultMirrorMat;
            public Material defaultDecalMat;
            public Material defaultTerrainMat;
        }

        [Serializable]
        public sealed class TextureResources
        {
            // Debug
            public Texture2D debugFontTex;
            public Texture2D colorGradient;

            // Pre-baked noise
            public Texture2D[] blueNoise16LTex;
            public Texture2D[] blueNoise16RGBTex;
            public Texture2D owenScrambledTex;
            public Texture2D scramblingTex;

            // Post-processing
            public Texture2D[] filmGrainTex;
            public Texture2D   SMAASearchTex;
            public Texture2D   SMAAAreaTex;
        }

        [Serializable]
        public sealed class ShaderGraphResources
        {
        }

        public ShaderResources shaders;
        public MaterialResources materials;
        public TextureResources textures;
        public ShaderGraphResources shaderGraphs;

#if UNITY_EDITOR
        // Note: move this to a static using once we can target C#6+
        T Load<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        public void Init()
        {
            // Load default renderPipelineResources / Material / Shader
            string HDRenderPipelinePath = HDUtils.GetHDRenderPipelinePath() + "Runtime/";
            string CorePath = HDUtils.GetHDRenderPipelinePath() + "Runtime/Core/"; // HDUtils.GetCorePath(); // All CoreRP have been move to HDRP currently for out of preview of SRP and LW

            // Shaders
            shaders = new ShaderResources
            {
                // Defaults
                defaultPS = Load<Shader>(HDRenderPipelinePath + "Material/Lit/Lit.shader"),

                // Debug
                debugDisplayLatlongPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugDisplayLatlong.Shader"),
                debugViewMaterialGBufferPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugViewMaterialGBuffer.Shader"),
                debugViewTilesPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugViewTiles.Shader"),
                debugFullScreenPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugFullScreen.Shader"),
                debugColorPickerPS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugColorPicker.Shader"),
                debugLightVolumePS = Load<Shader>(HDRenderPipelinePath + "Debug/DebugLightVolumes.Shader"),
                debugLightVolumeCS = Load<ComputeShader>(HDRenderPipelinePath + "Debug/DebugLightVolumes.compute"),
                // Lighting
                deferredPS = Load<Shader>(HDRenderPipelinePath + "Lighting/Deferred.Shader"),
                colorPyramidCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/ColorPyramid.compute"),
                colorPyramidPS = Load<Shader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/ColorPyramidPS.Shader"),
                depthPyramidCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/DepthPyramid.compute"),
                copyChannelCS = Load<ComputeShader>(CorePath + "CoreResources/GPUCopy.compute"),
                applyDistortionPS = Load<Shader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/Distortion/ApplyDistortion.shader"),
                screenSpaceReflectionsCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/ScreenSpaceLighting/ScreenSpaceReflections.compute"),

                // Lighting tile pass
                clearDispatchIndirectCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/cleardispatchindirect.compute"),
                buildDispatchIndirectCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/builddispatchindirect.compute"),
                buildScreenAABBCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/scrbound.compute"),
                buildPerTileLightListCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild.compute"),
                buildPerBigTileLightListCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild-bigtile.compute"),
                buildPerVoxelLightListCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild-clustered.compute"),
                buildMaterialFlagsCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/materialflags.compute"),
                deferredCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/Deferred.compute"),

                screenSpaceShadowCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/Shadow/ScreenSpaceShadow.compute"),
                volumeVoxelizationCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/VolumetricLighting/VolumeVoxelization.compute"),
                volumetricLightingCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/VolumetricLighting/VolumetricLighting.compute"),

                deferredTilePS = Load<Shader>(HDRenderPipelinePath + "Lighting/LightLoop/DeferredTile.shader"),

                subsurfaceScatteringCS = Load<ComputeShader>(HDRenderPipelinePath + "Material/SubsurfaceScattering/SubsurfaceScattering.compute"),
                combineLightingPS = Load<Shader>(HDRenderPipelinePath + "Material/SubsurfaceScattering/CombineLighting.shader"),
                
                // General
                cameraMotionVectorsPS = Load<Shader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/MotionVectors/CameraMotionVectors.shader"),
                copyStencilBufferPS = Load<Shader>(HDRenderPipelinePath + "ShaderLibrary/CopyStencilBuffer.shader"),
                copyDepthBufferPS = Load<Shader>(HDRenderPipelinePath + "ShaderLibrary/CopyDepthBuffer.shader"),
                blitPS = Load<Shader>(HDRenderPipelinePath + "ShaderLibrary/Blit.shader"),

                // Sky
                blitCubemapPS = Load<Shader>(HDRenderPipelinePath + "Sky/BlitCubemap.shader"),
                buildProbabilityTablesCS = Load<ComputeShader>(HDRenderPipelinePath + "Material/GGXConvolution/BuildProbabilityTables.compute"),
                computeGgxIblSampleDataCS = Load<ComputeShader>(HDRenderPipelinePath + "Material/GGXConvolution/ComputeGgxIblSampleData.compute"),
                GGXConvolvePS = Load<Shader>(HDRenderPipelinePath + "Material/GGXConvolution/GGXConvolve.shader"),
                charlieConvolvePS = Load<Shader>(HDRenderPipelinePath + "Material/Fabric/CharlieConvolve.shader"),
                opaqueAtmosphericScatteringPS = Load<Shader>(HDRenderPipelinePath + "Lighting/AtmosphericScattering/OpaqueAtmosphericScattering.shader"),
                hdriSkyPS = Load<Shader>(HDRenderPipelinePath + "Sky/HDRISky/HDRISky.shader"),
                integrateHdriSkyPS = Load<Shader>(HDRenderPipelinePath + "Sky/HDRISky/IntegrateHDRISky.shader"),
                proceduralSkyPS = Load<Shader>(HDRenderPipelinePath + "Sky/ProceduralSky/ProceduralSky.shader"),
                gradientSkyPS = Load<Shader>(HDRenderPipelinePath + "Sky/GradientSky/GradientSky.shader"),
                ambientProbeConvolutionCS = Load<ComputeShader>(HDRenderPipelinePath + "Sky/AmbientProbeConvolution.compute"),

                // Skybox/Cubemap is a builtin shader, must use Shader.Find to access it. It is fine because we are in the editor
                skyboxCubemapPS = Shader.Find("Skybox/Cubemap"),

                // Material
                preIntegratedFGD_GGXDisneyDiffusePS = Load<Shader>(HDRenderPipelinePath + "Material/PreIntegratedFGD/PreIntegratedFGD_GGXDisneyDiffuse.shader"),
                preIntegratedFGD_CharlieFabricLambertPS = Load<Shader>(HDRenderPipelinePath + "Material/PreIntegratedFGD/PreIntegratedFGD_CharlieFabricLambert.shader"),
                preIntegratedFGD_CookTorrancePS = Load<Shader>(HDRenderPipelinePath + "Material/AxF/PreIntegratedFGD_CookTorrance.shader"),
                preIntegratedFGD_WardPS = Load<Shader>(HDRenderPipelinePath + "Material/AxF/PreIntegratedFGD_Ward.shader"),

                // Utilities / Core
                encodeBC6HCS = Load<ComputeShader>(CorePath + "CoreResources/EncodeBC6H.compute"),
                cubeToPanoPS = Load<Shader>(CorePath + "CoreResources/CubeToPano.shader"),
                blitCubeTextureFacePS = Load<Shader>(CorePath + "CoreResources/BlitCubeTextureFace.shader"),
                filterAreaLightCookiesPS = Load<Shader>(HDRenderPipelinePath + "Material/LTCAreaLight/FilterAreaLightCookies.shader"),

                // Shadow
                shadowClearPS = Load<Shader>(HDRenderPipelinePath + "Lighting/Shadow/ShadowClear.shader"),
                evsmBlurCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/Shadow/EVSMBlur.compute"),
                debugHDShadowMapPS = Load<Shader>(HDRenderPipelinePath + "Lighting/Shadow/DebugDisplayHDShadowMap.shader"),
                momentShadowsCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/Shadow/MomentShadows.compute"),

                // Decal
                decalNormalBufferPS = Load<Shader>(HDRenderPipelinePath + "Material/Decal/DecalNormalBuffer.shader"),
                
                // Ambient occlusion
                aoDownsample1CS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/ScreenSpaceLighting/AmbientOcclusionDownsample1.compute"),
                aoDownsample2CS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/ScreenSpaceLighting/AmbientOcclusionDownsample2.compute"),
                aoRenderCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/ScreenSpaceLighting/AmbientOcclusionRender.compute"),
                aoUpsampleCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/ScreenSpaceLighting/AmbientOcclusionUpsample.compute"),

                // MSAA
                depthValuesPS = Load<Shader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/MSAA/DepthValues.shader"),
                colorResolvePS = Load<Shader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/MSAA/ColorResolve.shader"),
                aoResolvePS = Load<Shader>(HDRenderPipelinePath + "RenderPipeline/RenderPass/MSAA/AmbientOcclusionResolve.shader"),

                // Post-processing
                nanKillerCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/NaNKiller.compute"),
                exposureCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/Exposure.compute"),
                uberPostCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/UberPost.compute"),
                lutBuilder3DCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/LutBuilder3D.compute"),
                temporalAntialiasingCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/TemporalAntialiasing.compute"),
                depthOfFieldKernelCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/DepthOfFieldKernel.compute"),
                depthOfFieldCoCCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/DepthOfFieldCoC.compute"),
                depthOfFieldCoCReprojectCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/DepthOfFieldCoCReproject.compute"),
                depthOfFieldDilateCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/DepthOfFieldCoCDilate.compute"),
                depthOfFieldMipCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/DepthOfFieldMip.compute"),
                depthOfFieldMipSafeCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/DepthOfFieldMipSafe.compute"),
                depthOfFieldPrefilterCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/DepthOfFieldPrefilter.compute"),
                depthOfFieldTileMaxCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/DepthOfFieldTileMax.compute"),
                depthOfFieldGatherCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/DepthOfFieldGather.compute"),
                depthOfFieldCombineCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/DepthOfFieldCombine.compute"),
                motionBlurTileGenCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/MotionBlurTilePass.compute"),
                motionBlurCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/MotionBlur.compute"),
                motionBlurVelocityPrepCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/MotionBlurVelocityPrep.compute"),
                paniniProjectionCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/PaniniProjection.compute"),
                bloomPrefilterCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/BloomPrefilter.compute"),
                bloomBlurCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/BloomBlur.compute"),
                bloomUpsampleCS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/BloomUpsample.compute"),
                FXAACS = Load<ComputeShader>(HDRenderPipelinePath + "PostProcessing/Shaders/FXAA.compute"),
                finalPassPS = Load<Shader>(HDRenderPipelinePath + "PostProcessing/Shaders/FinalPass.shader"),
                clearBlackPS = Load<Shader>(HDRenderPipelinePath + "PostProcessing/Shaders/ClearBlack.shader"),
                SMAAPS = Load<Shader>(HDRenderPipelinePath + "PostProcessing/Shaders/SubpixelMorphologicalAntialiasing.shader"),

#if ENABLE_RAYTRACING
                aoRaytracing = Load<RaytracingShader>(HDRenderPipelinePath + "RenderPipeline/Raytracing/Shaders/RaytracingAmbientOcclusion.raytrace"),
                reflectionRaytracing = Load<RaytracingShader>(HDRenderPipelinePath + "RenderPipeline/Raytracing/Shaders/RaytracingReflections.raytrace"),
                indirectDiffuseRaytracing = Load<RaytracingShader>(HDRenderPipelinePath + "RenderPipeline/Raytracing/Shaders/RaytracingIndirectDiffuse.raytrace"),
                shadowsRaytracing = Load<RaytracingShader>(HDRenderPipelinePath + "RenderPipeline/Raytracing/Shaders/RaytracingAreaShadows.raytrace"),
                areaBillateralFilterCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipeline/Raytracing/Shaders/AreaBilateralShadow.compute"),
                jointBilateralFilterCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipeline/Raytracing/Shaders/JointBilateralFilter.compute"),
                reflectionBilateralFilterCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipeline/Raytracing/Shaders/RaytracingReflectionFilter.compute"),
                lightClusterBuildCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipeline/Raytracing/Shaders/RaytracingLightCluster.compute"),
                lightClusterDebugCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipeline/Raytracing/Shaders/DebugLightCluster.compute"),
				countTracedRays = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipeline/Raytracing/Shaders/CountTracedRays.compute"),
#endif
            };

            // Materials
            materials = new MaterialResources
            {
                defaultDiffuseMat = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/Material/DefaultHDMaterial.mat"),
                defaultMirrorMat = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/Material/DefaultHDMirrorMaterial.mat"),
                defaultDecalMat = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/Material/DefaultHDDecalMaterial.mat"),
                defaultTerrainMat = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/Material/DefaultHDTerrainMaterial.mat")
            };

            // Textures
            textures = new TextureResources
            {
                // Debug
                debugFontTex = Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/DebugFont.tga"),
                colorGradient = Load<Texture2D>(HDRenderPipelinePath + "Debug/ColorGradient.png"),

                filmGrainTex = new[]
                {
                    // These need to stay in this specific order!
                    Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/FilmGrain/Thin01.png"),
                    Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/FilmGrain/Thin02.png"),
                    Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/FilmGrain/Medium01.png"),
                    Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/FilmGrain/Medium02.png"),
                    Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/FilmGrain/Medium03.png"),
                    Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/FilmGrain/Medium04.png"),
                    Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/FilmGrain/Medium05.png"),
                    Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/FilmGrain/Medium06.png"),
                    Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/FilmGrain/Large01.png"),
                    Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/FilmGrain/Large02.png")
                },

                SMAAAreaTex = Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/SMAA/AreaTex.tga"),
                SMAASearchTex = Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/SMAA/SearchTex.tga"),

                blueNoise16LTex = new Texture2D[32],
                blueNoise16RGBTex = new Texture2D[32],
            };

            // ShaderGraphs
            shaderGraphs = new ShaderGraphResources
            {
            };

            // Fill-in blue noise textures
            for (int i = 0; i < 32; i++)
            {
                textures.blueNoise16LTex[i] = Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/BlueNoise16/L/LDR_LLL1_" + i + ".png");
                textures.blueNoise16RGBTex[i] = Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/BlueNoise16/RGB/LDR_RGB1_" + i + ".png");
            }

            // Coherent noise textures
            textures.owenScrambledTex = Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/CoherentNoise/OwenScrambledNoise.png");
            textures.scramblingTex = Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/Texture/CoherentNoise/ScrambleNoise.png");
        }

        bool NeedReload()
        {
            bool needReload = false;
            needReload |= shaders == null;
            if (needReload) return true;
            needReload |= shaders.defaultPS == null;
            needReload |= shaders.debugDisplayLatlongPS == null;
            needReload |= shaders.debugViewMaterialGBufferPS == null;
            needReload |= shaders.debugViewTilesPS == null;
            needReload |= shaders.debugFullScreenPS == null;
            needReload |= shaders.debugColorPickerPS == null;
            needReload |= shaders.debugLightVolumePS == null;
            needReload |= shaders.debugLightVolumeCS == null;
            needReload |= shaders.colorPyramidCS == null;
            needReload |= shaders.colorPyramidPS == null;
            needReload |= shaders.depthPyramidCS == null;
            needReload |= shaders.copyChannelCS == null;
            needReload |= shaders.applyDistortionPS == null;
            needReload |= shaders.screenSpaceReflectionsCS == null;
            needReload |= shaders.clearDispatchIndirectCS == null;
            needReload |= shaders.buildDispatchIndirectCS == null;
            needReload |= shaders.buildScreenAABBCS == null;
            needReload |= shaders.buildPerTileLightListCS == null;
            needReload |= shaders.buildPerBigTileLightListCS == null;
            needReload |= shaders.buildPerVoxelLightListCS == null;
            needReload |= shaders.buildMaterialFlagsCS == null;
            needReload |= shaders.deferredCS == null;
            needReload |= shaders.screenSpaceShadowCS == null;
            needReload |= shaders.volumeVoxelizationCS == null;
            needReload |= shaders.volumetricLightingCS == null;
            needReload |= shaders.deferredTilePS == null;
            needReload |= shaders.subsurfaceScatteringCS == null;
            needReload |= shaders.combineLightingPS == null;
            needReload |= shaders.cameraMotionVectorsPS == null;
            needReload |= shaders.copyStencilBufferPS == null;
            needReload |= shaders.copyDepthBufferPS == null;
            needReload |= shaders.blitPS == null;
            needReload |= shaders.blitCubemapPS == null;
            needReload |= shaders.buildProbabilityTablesCS == null;
            needReload |= shaders.computeGgxIblSampleDataCS == null;
            needReload |= shaders.GGXConvolvePS == null;
            needReload |= shaders.charlieConvolvePS == null;
            needReload |= shaders.opaqueAtmosphericScatteringPS == null;
            needReload |= shaders.hdriSkyPS == null;
            needReload |= shaders.integrateHdriSkyPS == null;
            needReload |= shaders.proceduralSkyPS == null;
            needReload |= shaders.gradientSkyPS == null;
            needReload |= shaders.ambientProbeConvolutionCS == null;
            needReload |= shaders.skyboxCubemapPS == null;
            needReload |= shaders.preIntegratedFGD_GGXDisneyDiffusePS == null;
            needReload |= shaders.preIntegratedFGD_CharlieFabricLambertPS == null;
            needReload |= shaders.preIntegratedFGD_CookTorrancePS == null;
            needReload |= shaders.preIntegratedFGD_WardPS == null;
            needReload |= shaders.encodeBC6HCS == null;
            needReload |= shaders.cubeToPanoPS == null;
            needReload |= shaders.blitCubeTextureFacePS == null;
            needReload |= shaders.filterAreaLightCookiesPS == null;
            needReload |= shaders.shadowClearPS == null;
            needReload |= shaders.evsmBlurCS == null;
            needReload |= shaders.debugHDShadowMapPS == null;
            needReload |= shaders.momentShadowsCS == null;
            needReload |= shaders.decalNormalBufferPS == null;
            needReload |= shaders.aoDownsample1CS == null;
            needReload |= shaders.aoDownsample2CS == null;
            needReload |= shaders.aoRenderCS == null;
            needReload |= shaders.aoUpsampleCS == null;
            needReload |= shaders.depthValuesPS == null;
            needReload |= shaders.colorResolvePS == null;
            needReload |= shaders.aoResolvePS == null;
            needReload |= shaders.nanKillerCS == null;
            needReload |= shaders.exposureCS == null;
            needReload |= shaders.uberPostCS == null;
            needReload |= shaders.lutBuilder3DCS == null;
            needReload |= shaders.temporalAntialiasingCS == null;
            needReload |= shaders.depthOfFieldKernelCS == null;
            needReload |= shaders.depthOfFieldCoCCS == null;
            needReload |= shaders.depthOfFieldCoCReprojectCS == null;
            needReload |= shaders.depthOfFieldDilateCS == null;
            needReload |= shaders.depthOfFieldMipCS == null;
            needReload |= shaders.depthOfFieldMipSafeCS == null;
            needReload |= shaders.depthOfFieldPrefilterCS == null;
            needReload |= shaders.depthOfFieldGatherCS == null;
            needReload |= shaders.depthOfFieldCombineCS == null;
            needReload |= shaders.motionBlurTileGenCS == null;
            needReload |= shaders.motionBlurCS == null;
            needReload |= shaders.motionBlurVelocityPrepCS == null;
            needReload |= shaders.paniniProjectionCS == null;
            needReload |= shaders.bloomPrefilterCS == null;
            needReload |= shaders.bloomBlurCS == null;
            needReload |= shaders.bloomUpsampleCS == null;
            needReload |= shaders.FXAACS == null;
            needReload |= shaders.finalPassPS == null;
            needReload |= shaders.clearBlackPS == null;
            needReload |= shaders.SMAAPS == null;
            
            needReload |= materials == null;
            if (needReload) return true;

            needReload |= textures == null;
            if (needReload) return true;
            needReload |= textures.debugFontTex == null;
            needReload |= textures.colorGradient == null;
            needReload |= textures.owenScrambledTex == null;
            needReload |= textures.scramblingTex == null;
            needReload |= textures.SMAAAreaTex == null;
            needReload |= textures.SMAASearchTex == null;
            needReload |= textures.SMAASearchTex == null;
            needReload |= textures.SMAASearchTex == null;
            needReload |= textures.blueNoise16LTex == null;
            if (needReload) return true;
            needReload |= textures.blueNoise16LTex.Length != 32;
            for (int i = 0; i < textures.blueNoise16LTex.Length; ++i)
                needReload |= textures.blueNoise16LTex[i] == null;
            needReload |= textures.blueNoise16RGBTex == null;
            if (needReload) return true;
            needReload |= textures.blueNoise16RGBTex.Length != 32;
            for (int i = 0; i < textures.blueNoise16RGBTex.Length; ++i)
                needReload |= textures.blueNoise16RGBTex[i] == null;
            needReload |= textures.filmGrainTex == null;
            if (needReload) return true;
            needReload |= textures.filmGrainTex.Length != 10;
            for (int i = 0; i < textures.filmGrainTex.Length; ++i)
                needReload |= textures.filmGrainTex[i] == null;

            return needReload;
        }

        public void ReloadIfNeeded()
        {
            if (NeedReload())
                Init();
        }
#endif
    }
}
