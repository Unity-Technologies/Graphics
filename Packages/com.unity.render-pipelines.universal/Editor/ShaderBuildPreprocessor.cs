using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
#if XR_MANAGEMENT_4_0_1_OR_NEWER
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
#endif
using ShaderPrefilteringData = UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.ShaderPrefilteringData;
using PrefilteringMode = UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.PrefilteringMode;
using PrefilteringModeMainLightShadows = UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.PrefilteringModeMainLightShadows;
using PrefilteringModeAdditionalLights = UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.PrefilteringModeAdditionalLights;

namespace UnityEditor.Rendering.Universal
{
    [Flags]
    enum ShaderFeatures : long
    {
        None = 0,
        MainLight = (1L << 0),
        MainLightShadows = (1L << 1),
        AdditionalLightsPixel = (1L << 2),
        AdditionalLightShadows = (1L << 3),
        AdditionalLightsVertex = (1L << 4),
        SoftShadows = (1L << 5),
        MixedLighting = (1L << 6),
        TerrainHoles = (1L << 7),
        DeferredShading = (1L << 8), // DeferredRenderer is in the list of renderer
        AccurateGbufferNormals = (1L << 9),
        ScreenSpaceOcclusion = (1L << 10),
        ScreenSpaceShadows = (1L << 11),
        UseFastSRGBLinearConversion = (1L << 12),
        LightLayers = (1L << 13),
        ReflectionProbeBlending = (1L << 14),
        ReflectionProbeBoxProjection = (1L << 15),
        DBufferMRT1 = (1L << 16),
        DBufferMRT2 = (1L << 17),
        DBufferMRT3 = (1L << 18),
        DecalScreenSpace = (1L << 19),
        DecalGBuffer = (1L << 20),
        DecalNormalBlendLow = (1L << 21),
        DecalNormalBlendMedium = (1L << 22),
        DecalNormalBlendHigh = (1L << 23),
        ForwardPlus = (1L << 24),
        RenderPassEnabled = (1L << 25),
        MainLightShadowsCascade = (1L << 26),
        DrawProcedural = (1L << 27),
        ScreenSpaceOcclusionAfterOpaque = (1L << 28),
        AdditionalLightsKeepOffVariants = (1L << 29),
        ShadowsKeepOffVariants = (1L << 30),
        UseLegacyLightmaps = (1L << 31),
        DecalLayers = (1L << 32),
        OpaqueWriteRenderingLayers = (1L << 33),
        GBufferWriteRenderingLayers = (1L << 34),
        DepthNormalPassRenderingLayers = (1L << 35),
        LightCookies = (1L << 36),
        LODCrossFade =  (1L << 37),
        ProbeVolumeL1 = (1L << 38),
        ProbeVolumeL2 = (1L << 39),
        HdrGrading = (1L << 40),
        AutoSHMode = (1L << 41),
        AutoSHModePerVertex = (1L << 42),
        ExplicitSHMode = (1L << 43),
        DataDrivenLensFlare = (1L << 44),
        ScreenSpaceLensFlare = (1L << 45),
        SoftShadowsLow = (1L << 46),
        SoftShadowsMedium = (1L << 47),
        SoftShadowsHigh = (1L << 48),
        AlphaOutput = (1L << 49),
        StencilLODCrossFade = (1L << 50),
        DeferredPlus = (1L << 51),
        ReflectionProbeAtlas = (1L << 52),
        All = ~0
    }

    [Flags]
    enum VolumeFeatures
    {
        None = 0,
        Calculated = (1 << 0),
        LensDistortion = (1 << 1),
        //2: Unused for now
        ChromaticAberration = (1 << 3),
        ToneMapping = (1 << 4),
        FilmGrain = (1 << 5),
        DepthOfField = (1 << 6),
        CameraMotionBlur = (1 << 7),
        PaniniProjection = (1 << 8),
        BloomLQ     = (1 << 9),
        BloomLQDirt = (1 << 10),
        BloomHQ     = (1 << 11),
        BloomHQDirt = (1 << 12),
        All = ~0
    }


    /// <summary>
    /// This class is used solely to make sure Shader Prefiltering data inside the
    /// URP Assets get updated before anything (Like Asset Bundles) are built.
    /// </summary>
    class UpdateShaderPrefilteringDataBeforeBuild : IPreprocessShaders
    {
        public int callbackOrder => -100;

        public UpdateShaderPrefilteringDataBeforeBuild()
        {
            ShaderBuildPreprocessor.GatherShaderFeatures(Debug.isDebugBuild);
        }

		public void OnProcessShader(Shader shader, ShaderSnippetData snippetData, IList<ShaderCompilerData> compilerDataList){}
    }

    /// <summary>
    /// Preprocess Build class used to determine the shader features used in the project.
    /// Also called when building Asset Bundles.
    /// </summary>
    class ShaderBuildPreprocessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        // Public
        public int callbackOrder => 0;
        public static bool s_StripUnusedVariants;
        public static bool s_StripDebugDisplayShaders;
        public static bool s_StripUnusedPostProcessingVariants;
        public static bool s_StripScreenCoordOverrideVariants;
        public static bool s_StripBicubicLightmapSamplingVariants;
        public static bool s_Strip2DPasses;
        public static bool s_UseSoftShadowQualityLevelKeywords;
        public static bool s_StripXRVariants;

        public static List<ShaderFeatures> supportedFeaturesList
        {
            get
            {
                // This can happen for example when building AssetBundles.
                if (s_SupportedFeaturesList.Count == 0)
                    GatherShaderFeatures(Debug.isDebugBuild);

                return s_SupportedFeaturesList;
            }
        }

        public static VolumeFeatures volumeFeatures
        {
            get
            {
                // This can happen for example when building AssetBundles.
                if (s_VolumeFeatures == VolumeFeatures.None)
                    GetSupportedFeaturesFromVolumes(ref s_VolumeFeatures);

                return s_VolumeFeatures;
            }
        }

        // Private
        private static bool s_KeepOffVariantForAdditionalLights;
        private static bool s_UseSHPerVertexForSHAuto;
        private static VolumeFeatures s_VolumeFeatures;
        private static List<ShaderFeatures> s_SupportedFeaturesList = new();

        // Helper class to detect XR build targets at build time.
        internal sealed class PlatformBuildTimeDetect
        {
            private static PlatformBuildTimeDetect s_PlatformInfo;
            internal bool isStandaloneXR { get; private set; }
            internal bool isHololens { get; private set; }
            internal bool isQuest { get; private set; }
            internal bool isSwitch { get; private set; }

            private PlatformBuildTimeDetect()
            {
                BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                isSwitch = buildTargetGroup == BuildTargetGroup.Switch;

#if XR_MANAGEMENT_4_0_1_OR_NEWER
                var buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
                if (buildTargetSettings != null && buildTargetSettings.AssignedSettings != null && buildTargetSettings.AssignedSettings.activeLoaders.Count > 0)
                {
                    isStandaloneXR = buildTargetGroup == BuildTargetGroup.Standalone;
                    isHololens = buildTargetGroup == BuildTargetGroup.WSA;
                    isQuest = buildTargetGroup == BuildTargetGroup.Android;
                }
#endif
            }

            internal static PlatformBuildTimeDetect GetInstance()
            {
                if (s_PlatformInfo == null)
                    s_PlatformInfo = new PlatformBuildTimeDetect();

                return s_PlatformInfo;
            }

            internal static void ClearInstance()
            {
                s_PlatformInfo = null;
            }
        }

        internal struct RendererRequirements
        {
            public int msaaSampleCount;
            public bool isUniversalRenderer;
            public bool needsProcedural;
            public bool needsMainLightShadows;
            public bool needsAdditionalLightShadows;
            public bool needsSoftShadows;
            public bool needsSoftShadowsQualityLevels;
            public bool needsShadowsOff;
            public bool needsAdditionalLightsOff;
            public bool needsGBufferRenderingLayers;
            public bool needsGBufferAccurateNormals;
            public bool needsRenderPass;
            public bool needsReflectionProbeBlending;
            public bool needsReflectionProbeBoxProjection;
            public bool needsReflectionProbeAtlas;
            public bool needsSHVertexForSHAuto;
            public RenderingMode renderingMode;
            public bool needsDeferredLighting => renderingMode == RenderingMode.Deferred || renderingMode == RenderingMode.DeferredPlus;
            public bool needsClusterLightLoop => renderingMode == RenderingMode.ForwardPlus || renderingMode == RenderingMode.DeferredPlus;
        }

        // Called before the build is started...
        public void OnPreprocessBuild(BuildReport report)
        {
#if PROFILE_BUILD
            Profiler.enableBinaryLog = true;
            Profiler.logFile = "profilerlog.raw";
            Profiler.enabled = true;
#endif

            bool isDevelopmentBuild = (report.summary.options & BuildOptions.Development) != 0;
            GatherShaderFeatures(isDevelopmentBuild);
        }

        // Called after the build has finished...
        public void OnPostprocessBuild(BuildReport report)
        {
            PlatformBuildTimeDetect.ClearInstance();
#if PROFILE_BUILD
            Profiler.enabled = false;
#endif
        }

        // Gathers all the shader features and updates the prefiltering
        // settings for all URP Assets in the quality settings
        internal static void GatherShaderFeatures(bool isDevelopmentBuild)
        {
            s_SupportedFeaturesList.Clear();
            GetGlobalAndPlatformSettings(isDevelopmentBuild);

            // If stripping of unused volume features is disabled, the s_VolumeFeatures
            // variable is set to include every keyword used by volumes shaders.
            // Otherwise it tries to gather all the volume features used in the project.
            if (s_StripUnusedPostProcessingVariants)
                GetSupportedFeaturesFromVolumes(ref s_VolumeFeatures);
            else
                GetEveryVolumeFeatures(ref s_VolumeFeatures);

            // If stripping of unused shader variants is disabled, the s_SupportedFeaturesList
            // list is set to include one item containing every keyword used by URP.
            // Otherwise it tries to gather all the shader features used in the project.
            if (s_StripUnusedVariants)
                HandleEnabledShaderStripping();
            else
                GetEveryShaderFeatureAndUpdateURPAssets(s_SupportedFeaturesList);
        }

        // Retrieves the global and platform settings used in the project...
        private static void GetGlobalAndPlatformSettings(bool isDevelopmentBuild)
        {
            if (GraphicsSettings.TryGetRenderPipelineSettings<ShaderStrippingSetting>(out var shaderStrippingSettings))
                s_StripDebugDisplayShaders = !isDevelopmentBuild || shaderStrippingSettings.stripRuntimeDebugShaders;
            else
                s_StripDebugDisplayShaders = true;

            if (GraphicsSettings.TryGetRenderPipelineSettings<URPShaderStrippingSetting>(out var urpShaderStrippingSettings))
            {
                s_StripUnusedPostProcessingVariants = urpShaderStrippingSettings.stripUnusedPostProcessingVariants;
                s_StripUnusedVariants               = urpShaderStrippingSettings.stripUnusedVariants;
                s_StripScreenCoordOverrideVariants  = urpShaderStrippingSettings.stripScreenCoordOverrideVariants;
            }

            if (GraphicsSettings.TryGetRenderPipelineSettings<LightmapSamplingSettings>(out var lightmapSamplingSettings))
                s_StripBicubicLightmapSamplingVariants = !lightmapSamplingSettings.useBicubicLightmapSampling;
            else
                s_StripBicubicLightmapSamplingVariants = true;

            PlatformBuildTimeDetect platformBuildTimeDetect = PlatformBuildTimeDetect.GetInstance();
            bool isShaderAPIMobileDefined = GraphicsSettings.HasShaderDefine(BuiltinShaderDefine.SHADER_API_MOBILE);
            if (platformBuildTimeDetect.isSwitch || isShaderAPIMobileDefined)
                s_UseSHPerVertexForSHAuto = true;

            // XR Stripping
            #if XR_MANAGEMENT_4_0_1_OR_NEWER
                BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                XRGeneralSettings generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
                s_StripXRVariants = generalSettings == null || generalSettings.Manager == null || generalSettings.Manager.activeLoaders.Count <= 0;

                if (platformBuildTimeDetect.isStandaloneXR)
                    s_StripDebugDisplayShaders = true;

                if (platformBuildTimeDetect.isHololens || platformBuildTimeDetect.isQuest)
                {
                    s_KeepOffVariantForAdditionalLights = true;
                    s_UseSoftShadowQualityLevelKeywords = true;
                    s_UseSHPerVertexForSHAuto = true;
                }
            #else
                s_UseSoftShadowQualityLevelKeywords = false;
                s_StripXRVariants = true;
            #endif
        }

        internal static void GetEveryVolumeFeatures(ref VolumeFeatures volumeFeatures)
        {
            volumeFeatures = VolumeFeatures.All;
        }

        // Checks each Volume Profile Assets for used features...
        private static void GetSupportedFeaturesFromVolumes(ref VolumeFeatures volumeFeatures)
        {
            if (!s_StripUnusedPostProcessingVariants)
                return;

            volumeFeatures = VolumeFeatures.Calculated;
            string[] guids = AssetDatabase.FindAssets("t:VolumeProfile");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // We only care what is in assets folder
                if (!path.StartsWith("Assets"))
                    continue;

                VolumeProfile asset = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if (asset == null)
                    continue;

                if (asset.Has<LensDistortion>())
                    volumeFeatures |= VolumeFeatures.LensDistortion;

                Bloom bloom;
                if (asset.TryGet<Bloom>(out bloom))
                {
                    //strip unused bloom variants. #pragma multi_compile_local_fragment _ _BLOOM_LQ _BLOOM_HQ _BLOOM_LQ_DIRT _BLOOM_HQ_DIRT
                    if (bloom.highQualityFiltering.value)
                    {
                        if (bloom.dirtIntensity.value > 0f && bloom.dirtTexture.value != null)
                            volumeFeatures |= VolumeFeatures.BloomHQDirt;
                        else
                            volumeFeatures |= VolumeFeatures.BloomHQ;
                    }
                    else
                    {
                        if (bloom.dirtIntensity.value > 0f && bloom.dirtTexture.value != null)
                            volumeFeatures |= VolumeFeatures.BloomLQDirt;
                        else
                            volumeFeatures |= VolumeFeatures.BloomLQ;
                    }
                }

                if (asset.Has<Tonemapping>())
                    volumeFeatures |= VolumeFeatures.ToneMapping;
                if (asset.Has<FilmGrain>())
                    volumeFeatures |= VolumeFeatures.FilmGrain;
                if (asset.Has<DepthOfField>())
                    volumeFeatures |= VolumeFeatures.DepthOfField;
                if (asset.Has<MotionBlur>())
                    volumeFeatures |= VolumeFeatures.CameraMotionBlur;
                if (asset.Has<PaniniProjection>())
                    volumeFeatures |= VolumeFeatures.PaniniProjection;
                if (asset.Has<ChromaticAberration>())
                    volumeFeatures |= VolumeFeatures.ChromaticAberration;
            }
        }

        internal static void GetEveryShaderFeatureAndPrefilteringData(List<ShaderFeatures> rendererFeaturesList, ref ShaderPrefilteringData spd)
        {
            // Add one Shader Features item that includes every keyword used by URP Shaders
            ShaderFeatures shaderFeatures = ShaderFeatures.All;
            rendererFeaturesList.Add(shaderFeatures);

            // Shader Prefiltering
            // Get prefiltering data that has every feature enabled
            spd = ShaderPrefilteringData.GetDefault();
        }

        // Used when Strip Unused Variants is disabled in the Global Settings.
        // One ShaderFeatures item, containing all the keywords used in URP, is added to the
        // s_SupportedFeaturesList and then every URP asset is updated so it doesn't prefilter any keywords.
        private static void GetEveryShaderFeatureAndUpdateURPAssets(List<ShaderFeatures> rendererFeaturesList)
        {
            ShaderPrefilteringData spd = new();
            GetEveryShaderFeatureAndPrefilteringData(rendererFeaturesList, ref spd);

            // Update each asset so it has every feature enabled
            using (ListPool<UniversalRenderPipelineAsset>.Get(out List<UniversalRenderPipelineAsset> urpAssets))
            {
                bool buildingForURP = EditorUserBuildSettings.activeBuildTarget.TryGetRenderPipelineAssets(urpAssets);
                if (!buildingForURP)
                    return;

                for (int urpAssetIndex = 0; urpAssetIndex < urpAssets.Count; urpAssetIndex++)
                {
                    UniversalRenderPipelineAsset urpAsset = urpAssets[urpAssetIndex];
                    if (urpAsset == null)
                        continue;

                    // Update the Prefiltering settings for this URP asset
                    urpAsset.UpdateShaderKeywordPrefiltering(ref spd);

                    // Mark the asset dirty so it can be serialized once the build is finished
                    EditorUtility.SetDirty(urpAsset);
                }
            }
        }

        // The path for gathering shader features for normal shader stripping
        private static void HandleEnabledShaderStripping()
        {
            s_Strip2DPasses = true;
            using (ListPool<UniversalRenderPipelineAsset>.Get(out List<UniversalRenderPipelineAsset> urpAssets))
            {
                bool buildingForURP = EditorUserBuildSettings.activeBuildTarget.TryGetRenderPipelineAssets(urpAssets);
                if (buildingForURP)
                {
                    // Get Supported features & update data used for Shader Prefiltering and Scriptable Stripping
                    GetSupportedShaderFeaturesFromAssets(ref urpAssets, ref s_SupportedFeaturesList, s_StripUnusedVariants);
                }
            }
        }

        // Checks each Universal Render Pipeline Asset for features used...
        private static void GetSupportedShaderFeaturesFromAssets(ref List<UniversalRenderPipelineAsset> urpAssets, ref List<ShaderFeatures> rendererFeaturesList, bool stripUnusedVariants)
        {
            List<ScreenSpaceAmbientOcclusionSettings> ssaoRendererFeatures = new List<ScreenSpaceAmbientOcclusionSettings>(16);
            for (int urpAssetIndex = 0; urpAssetIndex < urpAssets.Count; urpAssetIndex++)
            {
                // Get the asset and check if it's valid
                UniversalRenderPipelineAsset urpAsset = urpAssets[urpAssetIndex];
                if (urpAsset == null)
                    continue;

                // Check the asset for supported features
                ShaderFeatures urpAssetShaderFeatures = GetSupportedShaderFeaturesFromAsset(
                    ref urpAsset,
                    ref rendererFeaturesList,
                    ref ssaoRendererFeatures,
                    stripUnusedVariants,
                    out bool containsForwardRenderer,
                    out bool everyRendererHasSSAO
                );

                // Creates a struct containing all the prefiltering settings for this asset
                ShaderPrefilteringData spd = CreatePrefilteringSettings(
                    ref urpAssetShaderFeatures,
                    containsForwardRenderer,
                    everyRendererHasSSAO,
                    s_StripXRVariants,
                    !PlayerSettings.allowHDRDisplaySupport || !urpAsset.supportsHDR,
                    s_StripDebugDisplayShaders,
                    s_StripScreenCoordOverrideVariants,
                    s_StripBicubicLightmapSamplingVariants,
                    s_StripUnusedVariants,
                    ref ssaoRendererFeatures
                    );

                // Update the Prefiltering settings for this URP asset
                urpAsset.UpdateShaderKeywordPrefiltering(ref spd);

                // Mark the asset dirty so it can be serialized once the build is finished
                EditorUtility.SetDirty(urpAsset);

                // Clean up
                ssaoRendererFeatures.Clear();
            }
        }

        // Checks the assigned Universal Pipeline Asset for features used...
        internal static ShaderFeatures GetSupportedShaderFeaturesFromAsset(
            ref UniversalRenderPipelineAsset urpAsset,
            ref List<ShaderFeatures> rendererFeaturesList,
            ref List<ScreenSpaceAmbientOcclusionSettings> ssaoRendererFeatures,
            bool stripUnusedVariants,
            out bool containsForwardRenderer,
            out bool everyRendererHasSSAO)
        {
            ShaderFeatures urpAssetShaderFeatures = ShaderFeatures.MainLight;

            // Additional Lights and Shadows...
            switch (urpAsset.additionalLightsRenderingMode)
            {
                case LightRenderingMode.PerVertex:
                    urpAssetShaderFeatures |= ShaderFeatures.AdditionalLightsVertex;
                    break;
                case LightRenderingMode.PerPixel:
                    urpAssetShaderFeatures |= ShaderFeatures.AdditionalLightsPixel;
                    break;
                case LightRenderingMode.Disabled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (urpAsset.lightProbeSystem == LightProbeSystem.ProbeVolumes)
            {
                if (urpAsset.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL1)
                    urpAssetShaderFeatures |= ShaderFeatures.ProbeVolumeL1;

                if (urpAsset.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                    urpAssetShaderFeatures |= ShaderFeatures.ProbeVolumeL2;
            }

            if (urpAsset.supportsMixedLighting)
                urpAssetShaderFeatures |= ShaderFeatures.MixedLighting;

            if (urpAsset.supportsTerrainHoles)
                urpAssetShaderFeatures |= ShaderFeatures.TerrainHoles;

            if (urpAsset.useFastSRGBLinearConversion)
                urpAssetShaderFeatures |= ShaderFeatures.UseFastSRGBLinearConversion;

            if (urpAsset.useRenderingLayers)
                urpAssetShaderFeatures |= ShaderFeatures.LightLayers;

            if (urpAsset.supportsLightCookies)
                urpAssetShaderFeatures |= ShaderFeatures.LightCookies;

            bool hasHDROutput = PlayerSettings.allowHDRDisplaySupport && urpAsset.supportsHDR;
            if (urpAsset.colorGradingMode == ColorGradingMode.HighDynamicRange || hasHDROutput)
                urpAssetShaderFeatures |= ShaderFeatures.HdrGrading;

            if (urpAsset.enableLODCrossFade)
            {
                urpAssetShaderFeatures |= ShaderFeatures.LODCrossFade;

                if (urpAsset.lodCrossFadeDitheringType == LODCrossFadeDitheringType.Stencil)
                    urpAssetShaderFeatures |= ShaderFeatures.StencilLODCrossFade;
            }

            if (urpAsset.shEvalMode == ShEvalMode.Auto)
                urpAssetShaderFeatures |= ShaderFeatures.AutoSHMode;

            if (urpAsset.supportScreenSpaceLensFlare)
                urpAssetShaderFeatures |= ShaderFeatures.ScreenSpaceLensFlare;

            if (urpAsset.supportDataDrivenLensFlare)
                urpAssetShaderFeatures |= ShaderFeatures.DataDrivenLensFlare;

            if (urpAsset.gpuResidentDrawerMode != GPUResidentDrawerMode.Disabled)
                urpAssetShaderFeatures |= ShaderFeatures.UseLegacyLightmaps;

            // URP post-processing and alpha output follows the back-buffer color format requested in the asset.
            // Back-buffer alpha format is required. Or a render texture with alpha formats.
            // Without any external option we would need to keep all shaders and assume potential alpha output for all projects.
            // Therefore we strip the shader based on the asset enabling the alpha output for post-processing.
            // Alpha backbuffer is supported for:
            //   SDR 32-bit, RGBA8, (!urpAsset.supportsHDR)
            //   HDR 64-bit, RGBA16Float, (urpAsset.supportsHDR && urpAsset.hdrColorBufferPrecision == HDRColorBufferPrecision._64Bits)
            if(urpAsset.allowPostProcessAlphaOutput)
                urpAssetShaderFeatures |= ShaderFeatures.AlphaOutput;

            // Check each renderer & renderer feature
            urpAssetShaderFeatures = GetSupportedShaderFeaturesFromRenderers(
                ref urpAsset,
                ref rendererFeaturesList,
                urpAssetShaderFeatures,
                ref ssaoRendererFeatures,
                stripUnusedVariants,
                out containsForwardRenderer,
                out everyRendererHasSSAO);

            return urpAssetShaderFeatures;
        }

        // Checks each Universal Renderer in the assigned URP Asset for features used...
        internal static ShaderFeatures GetSupportedShaderFeaturesFromRenderers(
            ref UniversalRenderPipelineAsset urpAsset,
            ref List<ShaderFeatures> rendererFeaturesList,
            ShaderFeatures urpAssetShaderFeatures,
            ref List<ScreenSpaceAmbientOcclusionSettings> ssaoRendererFeatures,
            bool stripUnusedVariants,
            out bool containsForwardRenderer,
            out bool everyRendererHasSSAO)
        {
            // Sanity check
            if (rendererFeaturesList == null)
                rendererFeaturesList = new List<ShaderFeatures>();

            // The combined URP Asset features used for Prefiltering
            // We start with None instead of URP Asset features as they can change
            // when iterating over the renderers, such as when Forward Plus is in use.
            ShaderFeatures combinedURPAssetShaderFeatures = ShaderFeatures.None;

            containsForwardRenderer = false;
            everyRendererHasSSAO = true;
            ScriptableRendererData[] rendererDataArray = urpAsset.m_RendererDataList;
            for (int rendererIndex = 0; rendererIndex < rendererDataArray.Length; ++rendererIndex)
            {
                if (rendererDataArray[rendererIndex] == null)
                    continue;

                // Get feature requirements from the renderer data
                ScriptableRendererData rendererData = rendererDataArray[rendererIndex];
                RendererRequirements rendererRequirements = GetRendererRequirements(ref urpAsset, ref rendererData);

                // Get & add Supported features from renderers used for Scriptable Stripping and prefiltering.
                ShaderFeatures rendererShaderFeatures = GetSupportedShaderFeaturesFromRenderer(ref rendererRequirements, ref rendererData, ref ssaoRendererFeatures, ref containsForwardRenderer, urpAssetShaderFeatures);
                rendererFeaturesList.Add(rendererShaderFeatures);

                // Check to see if it's possible to remove the OFF variant for SSAO
                everyRendererHasSSAO &= IsFeatureEnabled(rendererShaderFeatures, ShaderFeatures.ScreenSpaceOcclusion);

                // Check for completely removing 2D passes
                s_Strip2DPasses &= rendererData is not Renderer2DData;

                // Add the features from the renderer to the combined feature set for this URP Asset
                combinedURPAssetShaderFeatures |= rendererShaderFeatures;
            }

            return combinedURPAssetShaderFeatures;
        }

        internal static bool NeedsProceduralKeyword(ref RendererRequirements rendererRequirements)
        {
            #if ENABLE_VR && ENABLE_XR_MODULE
                var xrResourcesAreValid = GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineRuntimeXRResources>()?.valid ?? false;
                return rendererRequirements.isUniversalRenderer && xrResourcesAreValid;
            #else
                return false;
            #endif
        }


        internal static RendererRequirements GetRendererRequirements(ref UniversalRenderPipelineAsset urpAsset, ref ScriptableRendererData rendererData)
        {
            UniversalRendererData universalRendererData = rendererData as UniversalRendererData;

            RendererRequirements rsd = new();
            rsd.isUniversalRenderer               = universalRendererData != null;
            rsd.msaaSampleCount                   = urpAsset.msaaSampleCount;
            rsd.renderingMode                     = rsd.isUniversalRenderer ? universalRendererData.renderingMode : RenderingMode.Forward;
            rsd.needsMainLightShadows             = urpAsset.supportsMainLightShadows && urpAsset.mainLightRenderingMode == LightRenderingMode.PerPixel;
            rsd.needsAdditionalLightShadows       = urpAsset.supportsAdditionalLightShadows && (urpAsset.additionalLightsRenderingMode == LightRenderingMode.PerPixel || rsd.renderingMode == RenderingMode.ForwardPlus);
            rsd.needsSoftShadows                  = urpAsset.supportsSoftShadows && (rsd.needsMainLightShadows || rsd.needsAdditionalLightShadows);
            rsd.needsSoftShadowsQualityLevels     = rsd.needsSoftShadows && s_UseSoftShadowQualityLevelKeywords;
            rsd.needsShadowsOff                   = !rendererData.stripShadowsOffVariants;
            rsd.needsAdditionalLightsOff          = s_KeepOffVariantForAdditionalLights || !rendererData.stripAdditionalLightOffVariants;
            rsd.needsGBufferRenderingLayers       = (rsd.isUniversalRenderer && rsd.needsDeferredLighting && urpAsset.useRenderingLayers);
            rsd.needsGBufferAccurateNormals       = (rsd.isUniversalRenderer && rsd.needsDeferredLighting && (universalRendererData.renderingMode == RenderingMode.Deferred || universalRendererData.renderingMode == RenderingMode.DeferredPlus) && universalRendererData.accurateGbufferNormals);
            rsd.needsRenderPass                   = (rsd.isUniversalRenderer && rsd.needsDeferredLighting);
            rsd.needsReflectionProbeBlending      = urpAsset.reflectionProbeBlending;
            rsd.needsReflectionProbeBoxProjection = urpAsset.reflectionProbeBoxProjection;
            rsd.needsReflectionProbeAtlas         = urpAsset.reflectionProbeBlending && (rsd.renderingMode == RenderingMode.DeferredPlus || urpAsset.reflectionProbeAtlas || urpAsset.gpuResidentDrawerMode != GPUResidentDrawerMode.Disabled) && rsd.needsClusterLightLoop;
            rsd.needsProcedural                   = NeedsProceduralKeyword(ref rsd);
            rsd.needsSHVertexForSHAuto            = s_UseSHPerVertexForSHAuto;

            return rsd;
        }

        // Checks the assigned Universal renderer for features used...
        internal static ShaderFeatures GetSupportedShaderFeaturesFromRenderer(ref RendererRequirements rendererRequirements, ref ScriptableRendererData rendererData, ref List<ScreenSpaceAmbientOcclusionSettings> ssaoRendererFeatures, ref bool containsForwardRenderer, ShaderFeatures urpAssetShaderFeatures)
        {
            ShaderFeatures shaderFeatures = urpAssetShaderFeatures;

            // Procedural...
            if (rendererRequirements.needsProcedural)
                shaderFeatures |= ShaderFeatures.DrawProcedural;

            // Rendering Modes...
            switch (rendererRequirements.renderingMode)
            {
                case RenderingMode.ForwardPlus:
                    shaderFeatures |= ShaderFeatures.ForwardPlus;
                    break;
                case RenderingMode.DeferredPlus:
                    shaderFeatures |= ShaderFeatures.DeferredPlus;
                    break;
                case RenderingMode.Deferred:
                    shaderFeatures |= ShaderFeatures.DeferredShading;
                    break;
                case RenderingMode.Forward:
                default:
                    containsForwardRenderer = true;
                    break;
            }

            // Renderer features...
            if (rendererData != null)
            {
                List<ScriptableRendererFeature> rendererFeatures = rendererData.rendererFeatures;
                shaderFeatures |= GetSupportedShaderFeaturesFromRendererFeatures(ref rendererRequirements, ref rendererFeatures, ref ssaoRendererFeatures);
            }

            // The Off variant for Additional Lights
            if (rendererRequirements.needsAdditionalLightsOff)
                shaderFeatures |= ShaderFeatures.AdditionalLightsKeepOffVariants;

            // Additional light clustering features (Forward+/Deferred+)
            if (rendererRequirements.needsClusterLightLoop)
            {
                shaderFeatures |= ShaderFeatures.AdditionalLightsKeepOffVariants;
                shaderFeatures &= ~(ShaderFeatures.AdditionalLightsPixel | ShaderFeatures.AdditionalLightsVertex);
            }

            // Main & Additional Light Shadows

            // Keeps the Off variant for Main and Additional Light shadows
            if (rendererRequirements.needsShadowsOff)
                shaderFeatures |= ShaderFeatures.ShadowsKeepOffVariants;

            if (rendererRequirements.needsMainLightShadows)
            {
                // Cascade count can be changed at runtime, so include both of them
                shaderFeatures |= ShaderFeatures.MainLightShadows;
                shaderFeatures |= ShaderFeatures.MainLightShadowsCascade;
            }

            // Additional Light Shadows
            if (rendererRequirements.needsAdditionalLightShadows)
                shaderFeatures |= ShaderFeatures.AdditionalLightShadows;

            // Soft shadows for Main and Additional Lights
            if (rendererRequirements.needsSoftShadows && !rendererRequirements.needsSoftShadowsQualityLevels)
                shaderFeatures |= ShaderFeatures.SoftShadows;

            if (rendererRequirements.needsSoftShadowsQualityLevels)
            {
                if (UniversalRenderPipeline.asset?.softShadowQuality == SoftShadowQuality.Low)
                    shaderFeatures |= ShaderFeatures.SoftShadowsLow;
                if (UniversalRenderPipeline.asset?.softShadowQuality == SoftShadowQuality.Medium)
                    shaderFeatures |= ShaderFeatures.SoftShadowsMedium;
                if (UniversalRenderPipeline.asset?.softShadowQuality == SoftShadowQuality.High)
                    shaderFeatures |= ShaderFeatures.SoftShadowsHigh;
            }

            // Deferred GBuffer Rendering Layers
            if (rendererRequirements.needsGBufferRenderingLayers)
                shaderFeatures |= ShaderFeatures.GBufferWriteRenderingLayers;

            // Deferred GBuffer Accurate Normals
            if (rendererRequirements.needsGBufferAccurateNormals)
                shaderFeatures |= ShaderFeatures.AccurateGbufferNormals;

            // Deferred GBuffer Native Render Pass
            if (rendererRequirements.needsRenderPass)
                shaderFeatures |= ShaderFeatures.RenderPassEnabled;

            // Reflection Probe Blending
            if (rendererRequirements.needsReflectionProbeBlending)
                shaderFeatures |= ShaderFeatures.ReflectionProbeBlending;

            // Reflection Probe Box Projection
            if (rendererRequirements.needsReflectionProbeBoxProjection)
                shaderFeatures |= ShaderFeatures.ReflectionProbeBoxProjection;

            // Reflection Probe Atlas
            if (rendererRequirements.needsReflectionProbeAtlas)
                shaderFeatures |= ShaderFeatures.ReflectionProbeAtlas;

            if (rendererRequirements.needsSHVertexForSHAuto)
                shaderFeatures |= ShaderFeatures.AutoSHModePerVertex;

            return shaderFeatures;
        }

        // Checks each Universal Renderer Feature in the assigned renderer...
        internal static ShaderFeatures GetSupportedShaderFeaturesFromRendererFeatures(ref RendererRequirements rendererRequirements, ref List<ScriptableRendererFeature> rendererFeatures, ref List<ScreenSpaceAmbientOcclusionSettings> ssaoRendererFeatures)
        {
            ShaderFeatures shaderFeatures = ShaderFeatures.None;

            bool usesRenderingLayers = false;
            RenderingLayerUtils.Event renderingLayersEvent = RenderingLayerUtils.Event.Opaque;

            for (int rendererFeatureIndex = 0; rendererFeatureIndex < rendererFeatures.Count; rendererFeatureIndex++)
            {
                ScriptableRendererFeature rendererFeature = rendererFeatures[rendererFeatureIndex];

                // Make sure the renderer feature isn't missing
                if (rendererFeature == null)
                    continue;

                // We don't add disabled renderer features if "Strip Unused Variants" is enabled.
                if (!rendererFeature.isActive)
                    continue;

                // Rendering Layers...
                if (rendererRequirements.isUniversalRenderer &&
                    RenderingLayerUtils.RequireRenderingLayers(rendererFeatures,
                        rendererRequirements.renderingMode,
                        rendererRequirements.needsGBufferAccurateNormals,
                        rendererRequirements.msaaSampleCount, out RenderingLayerUtils.Event rendererEvent, out _))
                {
                    usesRenderingLayers = true;
                    RenderingLayerUtils.CombineRendererEvents(rendererRequirements.needsDeferredLighting, rendererRequirements.msaaSampleCount, rendererEvent, ref renderingLayersEvent);
                }

                // Screen Space Shadows...
                ScreenSpaceShadows sssFeature = rendererFeature as ScreenSpaceShadows;
                if (sssFeature != null)
                {
                    // The feature is active (Tested a few lines above)
                    shaderFeatures |= ShaderFeatures.ScreenSpaceShadows;
                    continue;
                }

                // Screen Space Ambient Occlusion (SSAO)...
                // Removing the OFF variant requires every renderer to use SSAO. That is checked later.
                ScreenSpaceAmbientOcclusion ssaoFeature = rendererFeature as ScreenSpaceAmbientOcclusion;
                if (ssaoFeature != null)
                {
                    ScreenSpaceAmbientOcclusionSettings ssaoSettings = ssaoFeature.settings;
                    ssaoRendererFeatures.Add(ssaoSettings);

                    // The feature is active (Tested a few lines above) so check for AfterOpaque
                    if (ssaoSettings.AfterOpaque)
                        shaderFeatures |= ShaderFeatures.ScreenSpaceOcclusionAfterOpaque;
                    else
                        shaderFeatures |= ShaderFeatures.ScreenSpaceOcclusion;

                    // Otherwise the keyword will not be used
                    continue;
                }

                // Decals...
                DecalRendererFeature decal = rendererFeature as DecalRendererFeature;
                if (decal != null && rendererRequirements.isUniversalRenderer)
                {
                    DecalTechnique technique = decal.GetTechnique(rendererRequirements.needsDeferredLighting, rendererRequirements.needsGBufferAccurateNormals, false);
                    switch (technique)
                    {
                        case DecalTechnique.DBuffer:
                            shaderFeatures |= GetFromDecalSurfaceData(decal.GetDBufferSettings().surfaceData);
                            break;
                        case DecalTechnique.ScreenSpace:
                            shaderFeatures |= GetFromNormalBlend(decal.GetScreenSpaceSettings().normalBlend);
                            shaderFeatures |= ShaderFeatures.DecalScreenSpace;
                            break;
                        case DecalTechnique.GBuffer:
                            shaderFeatures |= GetFromNormalBlend(decal.GetScreenSpaceSettings().normalBlend);
                            shaderFeatures |= ShaderFeatures.DecalGBuffer;
                            //data.shaderFeatures |= ShaderFeatures.DecalScreenSpace; // In case deferred is not supported it will fallback to forward
                            break;
                    }

                    if (decal.requiresDecalLayers)
                        shaderFeatures |= ShaderFeatures.DecalLayers;
                }
            }

            // If using rendering layers, enable the appropriate feature
            if (usesRenderingLayers)
            {
                if (rendererRequirements.needsDeferredLighting)
                {
                    // Rendering layers in both Depth Normal and GBuffer passes are needed
                    // as some object might be rendered in forward and others in deferred.
                    shaderFeatures |= ShaderFeatures.DepthNormalPassRenderingLayers;
                    shaderFeatures |= ShaderFeatures.GBufferWriteRenderingLayers;
                }
                else
                {
                    // Check if other passes need the keyword
                    switch (renderingLayersEvent)
                    {
                        case RenderingLayerUtils.Event.DepthNormalPrePass:
                            shaderFeatures |= ShaderFeatures.DepthNormalPassRenderingLayers;
                            break;

                        case RenderingLayerUtils.Event.Opaque:
                            shaderFeatures |= ShaderFeatures.OpaqueWriteRenderingLayers;
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
            }

            return shaderFeatures;
        }

        // Retrieves the correct feature used from the Decal Surface Data Settings...
        private static ShaderFeatures GetFromDecalSurfaceData(DecalSurfaceData surfaceData)
        {
            ShaderFeatures shaderFeatures = ShaderFeatures.None;
            switch (surfaceData)
            {
                case DecalSurfaceData.Albedo:
                    shaderFeatures |= ShaderFeatures.DBufferMRT1;
                    break;
                case DecalSurfaceData.AlbedoNormal:
                    shaderFeatures |= ShaderFeatures.DBufferMRT2;
                    break;
                case DecalSurfaceData.AlbedoNormalMAOS:
                    shaderFeatures |= ShaderFeatures.DBufferMRT3;
                    break;
            }
            return shaderFeatures;
        }

        // Retrieves the correct feature used from the Decal Normal Blend Settings...
        private static ShaderFeatures GetFromNormalBlend(DecalNormalBlend normalBlend)
        {
            ShaderFeatures shaderFeatures = ShaderFeatures.None;
            switch (normalBlend)
            {
                case DecalNormalBlend.Low:
                    shaderFeatures |= ShaderFeatures.DecalNormalBlendLow;
                    break;
                case DecalNormalBlend.Medium:
                    shaderFeatures |= ShaderFeatures.DecalNormalBlendMedium;
                    break;
                case DecalNormalBlend.High:
                    shaderFeatures |= ShaderFeatures.DecalNormalBlendHigh;
                    break;
            }
            return shaderFeatures;
        }

        // Creates a struct containing all the prefiltering settings for the asset sent as a parameter
        internal static ShaderPrefilteringData CreatePrefilteringSettings(
            ref ShaderFeatures shaderFeatures,
            bool isAssetUsingForward,
            bool everyRendererHasSSAO,
            bool stripXR,
            bool stripHDR,
            bool stripDebug,
            bool stripScreenCoord,
            bool stripBicubicLightmap,
            bool stripUnusedVariants,
            ref List<ScreenSpaceAmbientOcclusionSettings> ssaoRendererFeatures
            )
        {
            bool isAssetUsingForwardPlus = IsFeatureEnabled(shaderFeatures, ShaderFeatures.ForwardPlus);
            bool isAssetUsingDeferredPlus = IsFeatureEnabled(shaderFeatures, ShaderFeatures.DeferredPlus);
            bool isAssetUsingDeferred = IsFeatureEnabled(shaderFeatures, ShaderFeatures.DeferredShading);

            ShaderPrefilteringData spd = new();
            spd.stripXRKeywords = stripXR;
            spd.stripSoftShadowsQualityLow = !IsFeatureEnabled(shaderFeatures, ShaderFeatures.SoftShadowsLow);
            spd.stripSoftShadowsQualityMedium = !IsFeatureEnabled(shaderFeatures, ShaderFeatures.SoftShadowsMedium);
            spd.stripSoftShadowsQualityHigh = !IsFeatureEnabled(shaderFeatures, ShaderFeatures.SoftShadowsHigh);
            spd.stripHDRKeywords = stripHDR;
            spd.stripAlphaOutputKeywords = !IsFeatureEnabled(shaderFeatures, ShaderFeatures.AlphaOutput);
            spd.stripDebugDisplay = stripDebug;
            spd.stripScreenCoordOverride = stripScreenCoord;
            spd.stripBicubicLightmapSampling = stripBicubicLightmap;

            // Rendering Modes
            // Check if only Deferred is being used
            spd.deferredPrefilteringMode = PrefilteringMode.Remove;
            if (isAssetUsingDeferred || isAssetUsingDeferredPlus)
            {
                // Only Deferred being used...
                if (!isAssetUsingForward && !isAssetUsingForwardPlus)
                    spd.deferredPrefilteringMode = PrefilteringMode.SelectOnly;
                else
                    spd.deferredPrefilteringMode = PrefilteringMode.Select;
            }

            // Check if only Forward+ is being used
            spd.forwardPlusPrefilteringMode = PrefilteringMode.Remove;
            if (isAssetUsingForwardPlus || isAssetUsingDeferredPlus)
            {
                // Only Forward Plus being used...
                if (!isAssetUsingForward && !isAssetUsingDeferred)
                    spd.forwardPlusPrefilteringMode = PrefilteringMode.SelectOnly;
                else
                    spd.forwardPlusPrefilteringMode = PrefilteringMode.Select;
            }

            // Additional Lights...
            spd.additionalLightsPrefilteringMode = PrefilteringModeAdditionalLights.Remove;
            if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.AdditionalLightsVertex))
            {
                if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.AdditionalLightsKeepOffVariants))
                    spd.additionalLightsPrefilteringMode = PrefilteringModeAdditionalLights.SelectVertexAndOff;
                else
                    spd.additionalLightsPrefilteringMode = PrefilteringModeAdditionalLights.SelectVertex;
            }

            if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.AdditionalLightsPixel))
            {
                if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.AdditionalLightsKeepOffVariants))
                    spd.additionalLightsPrefilteringMode = PrefilteringModeAdditionalLights.SelectPixelAndOff;
                else
                    spd.additionalLightsPrefilteringMode = PrefilteringModeAdditionalLights.SelectPixel;
            }

            // Shadows...
            // Main Light Shadows...
            spd.mainLightShadowsPrefilteringMode = PrefilteringModeMainLightShadows.Remove;
            if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.MainLightShadows))
            {
                if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.MainLightShadowsCascade))
                {
                    if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.ShadowsKeepOffVariants))
                        spd.mainLightShadowsPrefilteringMode = PrefilteringModeMainLightShadows.SelectAll;
                    else
                        spd.mainLightShadowsPrefilteringMode = PrefilteringModeMainLightShadows.SelectMainLightAndCascades;
                }
                else
                {
                    if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.ShadowsKeepOffVariants))
                        spd.mainLightShadowsPrefilteringMode = PrefilteringModeMainLightShadows.SelectMainLightAndOff;
                    else
                        spd.mainLightShadowsPrefilteringMode = PrefilteringModeMainLightShadows.SelectMainLight;
                }
            }

            // Additional Light Shadows...
            spd.additionalLightsShadowsPrefilteringMode = PrefilteringMode.Remove;
            if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.AdditionalLightShadows))
            {
                if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.ShadowsKeepOffVariants))
                    spd.additionalLightsShadowsPrefilteringMode = PrefilteringMode.Select;
                else
                    spd.additionalLightsShadowsPrefilteringMode = PrefilteringMode.SelectOnly;
            }

            // Decals' MRT keywords
            spd.stripDBufferMRT1 = !IsFeatureEnabled(shaderFeatures, ShaderFeatures.DBufferMRT1);
            spd.stripDBufferMRT2 = !IsFeatureEnabled(shaderFeatures, ShaderFeatures.DBufferMRT2);
            spd.stripDBufferMRT3 = !IsFeatureEnabled(shaderFeatures, ShaderFeatures.DBufferMRT3);

            // Native Render Pass
            spd.stripNativeRenderPass = !IsFeatureEnabled(shaderFeatures, ShaderFeatures.RenderPassEnabled);

            // Rendering Layers
            spd.stripWriteRenderingLayers =
                   !IsFeatureEnabled(shaderFeatures, ShaderFeatures.DepthNormalPassRenderingLayers)
                && !IsFeatureEnabled(shaderFeatures, ShaderFeatures.GBufferWriteRenderingLayers)
                && !IsFeatureEnabled(shaderFeatures, ShaderFeatures.OpaqueWriteRenderingLayers);

            // Disable lightmap texture arrays (GPU resident drawer)
            spd.useLegacyLightmaps = IsFeatureEnabled(shaderFeatures, ShaderFeatures.UseLegacyLightmaps);

            // Screen Space Ambient Occlusion
            spd.screenSpaceOcclusionPrefilteringMode = PrefilteringMode.Remove;
            if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.ScreenSpaceOcclusion))
            {
                // Remove the SSAO's OFF variant if Global Settings allow it and every renderer uses it.
                if (stripUnusedVariants && everyRendererHasSSAO)
                    spd.screenSpaceOcclusionPrefilteringMode = PrefilteringMode.SelectOnly;
                // Otherwise we keep both
                else
                    spd.screenSpaceOcclusionPrefilteringMode = PrefilteringMode.Select;
            }

            // SSAO shader keywords
            spd.stripSSAODepthNormals      = true;
            spd.stripSSAOSourceDepthLow    = true;
            spd.stripSSAOSourceDepthMedium = true;
            spd.stripSSAOSourceDepthHigh   = true;
            spd.stripSSAOBlueNoise         = true;
            spd.stripSSAOInterleaved       = true;
            spd.stripSSAOSampleCountLow    = true;
            spd.stripSSAOSampleCountMedium = true;
            spd.stripSSAOSampleCountHigh   = true;
            for (int i = 0; i < ssaoRendererFeatures.Count; i++)
            {
                ScreenSpaceAmbientOcclusionSettings ssaoSettings = ssaoRendererFeatures[i];
                bool isUsingDepthNormals = ssaoSettings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
                spd.stripSSAODepthNormals      &= !isUsingDepthNormals;
                spd.stripSSAOSourceDepthLow    &= isUsingDepthNormals || ssaoSettings.NormalSamples != ScreenSpaceAmbientOcclusionSettings.NormalQuality.Low;
                spd.stripSSAOSourceDepthMedium &= isUsingDepthNormals || ssaoSettings.NormalSamples != ScreenSpaceAmbientOcclusionSettings.NormalQuality.Medium;
                spd.stripSSAOSourceDepthHigh   &= isUsingDepthNormals || ssaoSettings.NormalSamples != ScreenSpaceAmbientOcclusionSettings.NormalQuality.High;
                spd.stripSSAOBlueNoise         &= ssaoSettings.AOMethod != ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise;
                spd.stripSSAOInterleaved       &= ssaoSettings.AOMethod != ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.InterleavedGradient;
                spd.stripSSAOSampleCountLow    &= ssaoSettings.Samples != ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Low;
                spd.stripSSAOSampleCountMedium &= ssaoSettings.Samples != ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Medium;
                spd.stripSSAOSampleCountHigh   &= ssaoSettings.Samples != ScreenSpaceAmbientOcclusionSettings.AOSampleOption.High;
            }

            return spd;
        }

        // Checks whether a ShaderFeature is enabled or not
        internal static bool IsFeatureEnabled(ShaderFeatures featureMask, ShaderFeatures feature)
        {
            return (featureMask & feature) != 0;
        }
    }
}
