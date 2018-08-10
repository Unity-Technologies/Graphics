using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class RenderPipelineResources : ScriptableObject
    {
        const int currentVersion = 3;
        [SerializeField]
        [FormerlySerializedAs("version")]
        int m_Version = 1;

        // Default Material / Shader
        public Material defaultDiffuseMaterial;
        public Material defaultMirrorMaterial;
        public Material defaultDecalMaterial;
        public Shader defaultShader;

        // Debug
        public Texture2D debugFontTexture;
        public Shader debugDisplayLatlongShader;
        public Shader debugViewMaterialGBufferShader;
        public Shader debugViewTilesShader;
        public Shader debugFullScreenShader;
        public Shader debugColorPickerShader;
        public Shader debugLightVolumeShader;

        // Lighting resources
        public Shader deferredShader;
        public ComputeShader colorPyramidCS;
        public ComputeShader depthPyramidCS;
        public ComputeShader copyChannelCS;
        public ComputeShader texturePaddingCS;
        public ComputeShader applyDistortionCS;

        // Lighting tile pass resources
        public ComputeShader clearDispatchIndirectShader;
        public ComputeShader buildDispatchIndirectShader;
        public ComputeShader buildScreenAABBShader;
        public ComputeShader buildPerTileLightListShader;     // FPTL
        public ComputeShader buildPerBigTileLightListShader;
        public ComputeShader buildPerVoxelLightListShader;    // clustered
        public ComputeShader buildMaterialFlagsShader;
        public ComputeShader deferredComputeShader;
        public ComputeShader screenSpaceShadowComputeShader;
        public ComputeShader volumeVoxelizationCS;
        public ComputeShader volumetricLightingCS;

        public ComputeShader subsurfaceScatteringCS; // Disney SSS
        public Shader subsurfaceScattering; // Jimenez SSS
        public Shader combineLighting;

        // General
        public Shader cameraMotionVectors;
        public Shader copyStencilBuffer;
        public Shader copyDepthBuffer;
        public Shader blit;

        // Sky
        public Shader blitCubemap;
        public ComputeShader buildProbabilityTables;
        public ComputeShader computeGgxIblSampleData;
        public Shader GGXConvolve;
        public Shader opaqueAtmosphericScattering;
        public Shader hdriSky;
        public Shader integrateHdriSky;
        public Shader proceduralSky;
        public Shader skyboxCubemap;
        public Shader gradientSky;

        // Material
        public Shader preIntegratedFGD_GGXDisneyDiffuse;
        public Shader preIntegratedFGD_CharlieFabricLambert;

        // Utilities / Core
        public ComputeShader encodeBC6HCS;
        public Shader cubeToPanoShader;
        public Shader blitCubeTextureFace;

        // Shadow
        public Shader shadowClearShader;
        public ComputeShader shadowBlurMoments;
        public Shader debugShadowMapShader;
        
#if UNITY_EDITOR
        public void UpgradeIfNeeded()
        {
            if (m_Version != currentVersion)
            {
                Init();

                m_Version = currentVersion;
            }
        }

        // Note: move this to a static using once we can target C#6+
        T Load<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        public void Init()
        {
            // Load default renderPipelineResources / Material / Shader
            string HDRenderPipelinePath = HDUtils.GetHDRenderPipelinePath();
            string CorePath = HDUtils.GetCorePath();

            defaultDiffuseMaterial = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/DefaultHDMaterial.mat");
            defaultMirrorMaterial = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/DefaultHDMirrorMaterial.mat");

            defaultDecalMaterial = Load<Material>(HDRenderPipelinePath + "RenderPipelineResources/DefaultHDDecalMaterial.mat");
            defaultShader = Load<Shader>(HDRenderPipelinePath + "Material/Lit/Lit.shader");

            debugFontTexture = Load<Texture2D>(HDRenderPipelinePath + "RenderPipelineResources/DebugFont.tga");
            debugDisplayLatlongShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugDisplayLatlong.Shader");
            debugViewMaterialGBufferShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugViewMaterialGBuffer.Shader");
            debugViewTilesShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugViewTiles.Shader");
            debugFullScreenShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugFullScreen.Shader");
            debugColorPickerShader = Load<Shader>(HDRenderPipelinePath + "Debug/DebugColorPicker.Shader");
            debugLightVolumeShader  = Load<Shader>(HDRenderPipelinePath + "Debug/DebugLightVolume.Shader");

            deferredShader = Load<Shader>(HDRenderPipelinePath + "Lighting/Deferred.Shader");
            colorPyramidCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipelineResources/ColorPyramid.compute");
            depthPyramidCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipelineResources/DepthPyramid.compute");
            copyChannelCS = Load<ComputeShader>(CorePath + "CoreResources/GPUCopy.compute");
            texturePaddingCS = Load<ComputeShader>(CorePath + "CoreResources/TexturePadding.compute");
            applyDistortionCS = Load<ComputeShader>(HDRenderPipelinePath + "RenderPipelineResources/ApplyDistorsion.compute");

            clearDispatchIndirectShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/cleardispatchindirect.compute");
            buildDispatchIndirectShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/builddispatchindirect.compute");
            buildScreenAABBShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/scrbound.compute");
            buildPerTileLightListShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild.compute");
            buildPerBigTileLightListShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild-bigtile.compute");
            buildPerVoxelLightListShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/lightlistbuild-clustered.compute");
            buildMaterialFlagsShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/materialflags.compute");
            deferredComputeShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/LightLoop/Deferred.compute");

            screenSpaceShadowComputeShader = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/ScreenSpaceShadow.compute");
            volumeVoxelizationCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/Volumetrics/VolumeVoxelization.compute");
            volumetricLightingCS = Load<ComputeShader>(HDRenderPipelinePath + "Lighting/Volumetrics/VolumetricLighting.compute");

            subsurfaceScatteringCS = Load<ComputeShader>(HDRenderPipelinePath + "Material/SubsurfaceScattering/SubsurfaceScattering.compute");
            subsurfaceScattering = Load<Shader>(HDRenderPipelinePath + "Material/SubsurfaceScattering/SubsurfaceScattering.shader");
            combineLighting = Load<Shader>(HDRenderPipelinePath + "Material/SubsurfaceScattering/CombineLighting.shader");

            // General
            cameraMotionVectors = Load<Shader>(HDRenderPipelinePath + "RenderPipelineResources/CameraMotionVectors.shader");
            copyStencilBuffer = Load<Shader>(HDRenderPipelinePath + "RenderPipelineResources/CopyStencilBuffer.shader");
            copyDepthBuffer = Load<Shader>(HDRenderPipelinePath + "RenderPipelineResources/CopyDepthBuffer.shader");
            blit = Load<Shader>(HDRenderPipelinePath + "RenderPipelineResources/Blit.shader");

            // Sky
            blitCubemap = Load<Shader>(HDRenderPipelinePath + "Sky/BlitCubemap.shader");
            buildProbabilityTables = Load<ComputeShader>(HDRenderPipelinePath + "Material/GGXConvolution/BuildProbabilityTables.compute");
            computeGgxIblSampleData = Load<ComputeShader>(HDRenderPipelinePath + "Material/GGXConvolution/ComputeGgxIblSampleData.compute");
            GGXConvolve = Load<Shader>(HDRenderPipelinePath + "Material/GGXConvolution/GGXConvolve.shader");
            opaqueAtmosphericScattering = Load<Shader>(HDRenderPipelinePath + "Lighting/AtmosphericScattering/OpaqueAtmosphericScattering.shader");
            hdriSky = Load<Shader>(HDRenderPipelinePath + "Sky/HDRISky/HDRISky.shader");
            integrateHdriSky = Load<Shader>(HDRenderPipelinePath + "Sky/HDRISky/IntegrateHDRISky.shader");
            proceduralSky = Load<Shader>(HDRenderPipelinePath + "Sky/ProceduralSky/ProceduralSky.shader");
            gradientSky = Load<Shader>(HDRenderPipelinePath + "Sky/GradientSky/GradientSky.shader");
            // Skybox/Cubemap is a builtin shader, must use Sahder.Find to access it. It is fine because we are in the editor
            skyboxCubemap = Shader.Find("Skybox/Cubemap");

            // Material
            preIntegratedFGD_GGXDisneyDiffuse = Load<Shader>(HDRenderPipelinePath + "Material/PreIntegratedFGD/PreIntegratedFGD_GGXDisneyDiffuse.shader");
            preIntegratedFGD_CharlieFabricLambert = Load<Shader>(HDRenderPipelinePath + "Material/PreIntegratedFGD/PreIntegratedFGD_CharlieFabricLambert.shader");

            // Utilities / Core
            encodeBC6HCS = Load<ComputeShader>(CorePath + "CoreResources/EncodeBC6H.compute");
            cubeToPanoShader = Load<Shader>(CorePath + "CoreResources/CubeToPano.shader");
            blitCubeTextureFace = Load<Shader>(CorePath + "CoreResources/BlitCubeTextureFace.shader");

            // Shadow
            shadowClearShader = Load<Shader>(CorePath + "Shadow/ShadowClear.shader");
            shadowBlurMoments = Load<ComputeShader>(CorePath + "Shadow/ShadowBlurMoments.compute");
            debugShadowMapShader = Load<Shader>(CorePath + "Shadow/DebugDisplayShadowMap.shader");
        }
#endif
    }
}
