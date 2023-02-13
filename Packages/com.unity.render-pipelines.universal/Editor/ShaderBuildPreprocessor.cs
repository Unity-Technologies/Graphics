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
        AdditionalLights = (1L << 2),
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
        // Unused = (1L << 31),
        DecalLayers = (1L << 32),
        OpaqueWriteRenderingLayers = (1L << 33),
        GBufferWriteRenderingLayers = (1L << 34),
        DepthNormalPassRenderingLayers = (1L << 35),
        LightCookies = (1L << 36),
        // Unused =  (1L << 37),
        ProbeVolumeL1 = (1L << 38),
        ProbeVolumeL2 = (1L << 39),
        HdrGrading = (1L << 40),
    }

    [Flags]
    enum VolumeFeatures
    {
        None = 0,
        Calculated = (1 << 0),
        LensDistortion = (1 << 1),
        Bloom = (1 << 2),
        ChromaticAberration = (1 << 3),
        ToneMapping = (1 << 4),
        FilmGrain = (1 << 5),
        DepthOfField = (1 << 6),
        CameraMotionBlur = (1 << 7),
        PaniniProjection = (1 << 8),
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
        public static bool s_Strip2DPasses;

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
                    GetSupportedFeaturesFromVolumes();

                return s_VolumeFeatures;
            }
        }

        // Private
        private static bool s_StripXRVariants;
        private static bool s_KeepOffVariantForAdditionalLights;
        private static VolumeFeatures s_VolumeFeatures;
        private static List<ShaderFeatures> s_SupportedFeaturesList = new List<ShaderFeatures>();

        // Struct used to contain data used in various functions
        // while determining the features used in the build.
        internal struct StrippingData
        {
            public bool isAssetUsing2D;
            public bool isAssetUsingForward;
            public bool everyRendererHasSSAO;

            public RenderingMode rendererMode;
            public ShaderFeatures shaderFeatures;
            public ShaderFeatures urpAssetShaderFeatures;
            public ShaderFeatures combinedURPAssetShaderFeatures;

            public UniversalRenderer universalRenderer;
            public ScriptableRenderer renderer;
            public UniversalRendererData universalRendererData;
            public ScriptableRendererData rendererData;
            public ScriptableRendererFeature rendererFeature;
            public UniversalRenderPipelineAsset urpAsset;
            public List<ScreenSpaceAmbientOcclusionSettings> ssaoRendererFeatures;

            public StrippingData(UniversalRenderPipelineAsset pipelineAsset)
            {
                isAssetUsing2D = false;
                rendererMode = RenderingMode.Forward;
                isAssetUsingForward = false;
                everyRendererHasSSAO = false;
                urpAsset = pipelineAsset;
                shaderFeatures = new ShaderFeatures();
                urpAssetShaderFeatures = new ShaderFeatures();
                combinedURPAssetShaderFeatures = new ShaderFeatures();

                renderer = null;
                universalRenderer = null;
                rendererData = null;
                universalRendererData = null;
                rendererFeature = null;

                ssaoRendererFeatures = new List<ScreenSpaceAmbientOcclusionSettings>();
            }
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
            AssetDatabase.SaveAssets();
#if PROFILE_BUILD
            Profiler.enabled = false;
#endif
        }

        // Gathers all the shader features and updates the prefiltering
        // settings for all URP Assets in the quality settings
        private static void GatherShaderFeatures(bool isDevelopmentBuild)
        {
            GetGlobalAndPlatformSettings(isDevelopmentBuild);
            GetSupportedFeaturesFromVolumes();
            GetSupportedShaderFeaturesFromAssets();
        }

        // Retrieves the global and platform settings used in the project...
        private static void GetGlobalAndPlatformSettings(bool isDevelopmentBuild)
        {
            UniversalRenderPipelineGlobalSettings globalSettings = UniversalRenderPipelineGlobalSettings.instance;
            if (globalSettings)
            {
                s_StripUnusedPostProcessingVariants = globalSettings.stripUnusedPostProcessingVariants;
                s_StripDebugDisplayShaders = !isDevelopmentBuild || globalSettings.stripDebugVariants;
                s_StripUnusedVariants = globalSettings.stripUnusedVariants;
                s_StripScreenCoordOverrideVariants = globalSettings.stripScreenCoordOverrideVariants;
            }
            else
            {
                s_StripDebugDisplayShaders = true;
            }

            #if XR_MANAGEMENT_4_0_1_OR_NEWER
            // XR Stripping
            XRGeneralSettings generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
            s_StripXRVariants = generalSettings == null || generalSettings.Manager == null || generalSettings.Manager.activeLoaders.Count <= 0;

            var buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (buildTargetSettings != null && buildTargetSettings.AssignedSettings != null && buildTargetSettings.AssignedSettings.activeLoaders.Count > 0)
                s_StripDebugDisplayShaders = true;

            // Additional Lights for XR...

            // XRTODO: We need to figure out what's the proper way to detect HL target platform when building.
            // For now, HL is the only XR platform available on WSA so we assume this case targets HL platform.
            // Due to the performance consideration, keep additional light off variant to avoid extra ALU cost related to dummy additional light handling.
            XRGeneralSettings wsaTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.WSA);
            if (wsaTargetSettings != null && wsaTargetSettings.AssignedSettings != null && wsaTargetSettings.AssignedSettings.activeLoaders.Count > 0)
                s_KeepOffVariantForAdditionalLights = true;

            XRGeneralSettings questTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (questTargetSettings != null && questTargetSettings.AssignedSettings != null && questTargetSettings.AssignedSettings.activeLoaders.Count > 0)
                s_KeepOffVariantForAdditionalLights = true;
            #else
            s_StripXRVariants = true;
            #endif
        }

        // Checks each Volume Profile Assets for used features...
        private static void GetSupportedFeaturesFromVolumes()
        {
            if (!s_StripUnusedPostProcessingVariants)
                return;

            s_VolumeFeatures = VolumeFeatures.Calculated;
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
                    s_VolumeFeatures |= VolumeFeatures.LensDistortion;
                if (asset.Has<Bloom>())
                    s_VolumeFeatures |= VolumeFeatures.Bloom;
                if (asset.Has<Tonemapping>())
                    s_VolumeFeatures |= VolumeFeatures.ToneMapping;
                if (asset.Has<FilmGrain>())
                    s_VolumeFeatures |= VolumeFeatures.FilmGrain;
                if (asset.Has<DepthOfField>())
                    s_VolumeFeatures |= VolumeFeatures.DepthOfField;
                if (asset.Has<MotionBlur>())
                    s_VolumeFeatures |= VolumeFeatures.CameraMotionBlur;
                if (asset.Has<PaniniProjection>())
                    s_VolumeFeatures |= VolumeFeatures.PaniniProjection;
                if (asset.Has<ChromaticAberration>())
                    s_VolumeFeatures |= VolumeFeatures.ChromaticAberration;
            }
        }

        // Checks each Universal Render Pipeline Asset for features used...
        private static void GetSupportedShaderFeaturesFromAssets()
        {
            using (ListPool<UniversalRenderPipelineAsset>.Get(out List<UniversalRenderPipelineAsset> urpAssets))
            {
                bool success = EditorUserBuildSettings.activeBuildTarget.TryGetRenderPipelineAssets(urpAssets);
                if (!success)
                {
                    Debug.LogError("Unable to get UniversalRenderPipelineAssets from EditorUserBuildSettings.activeBuildTarget.");
                    return;
                }

                // Get Supported features & update data used for Shader Prefiltering
                s_Strip2DPasses = true;
                s_SupportedFeaturesList.Clear();
                for (int urpAssetIndex = 0; urpAssetIndex < urpAssets.Count; urpAssetIndex++)
                {
                    UniversalRenderPipelineAsset urpAsset = urpAssets[urpAssetIndex];
                    if (urpAsset == null)
                        continue;

                    // Create a new StrippingData container for this URP Asset
                    StrippingData data = new(urpAsset);

                    // Check the asset for supported features
                    data.urpAssetShaderFeatures = GetSupportedShaderFeaturesFromAsset(ref data);

                    // Check each renderer & renderer feature
                    GetSupportedShaderFeaturesFromRenderers(ref data);

                    // Creates a struct containing all the prefiltering settings for this asset
                    ShaderPrefilteringData spd = CreatePrefilteringSettings(
                        ref data,
                        s_StripXRVariants,
                        !PlayerSettings.useHDRDisplay || !data.urpAsset.supportsHDR,
                        s_StripDebugDisplayShaders,
                        s_StripScreenCoordOverrideVariants
                    );

                    // Update the Shader Prefiltering data and send it to the URP Asset
                    urpAsset.UpdateShaderKeywordPrefiltering(ref spd);

                    // Update whether 2D passes can be stripped
                    s_Strip2DPasses &= !data.isAssetUsing2D;

                    EditorUtility.SetDirty(urpAsset);
                }
            }
        }

        // Checks the assigned Universal Pipeline Asset for features used...
        private static ShaderFeatures GetSupportedShaderFeaturesFromAsset(ref StrippingData data)
        {
            ref UniversalRenderPipelineAsset urpAsset = ref data.urpAsset;
            ShaderFeatures shaderFeatures = ShaderFeatures.MainLight;

            // Main Light Shadows & Soft Shadows...
            // Main Light Shadows keyword is always included to improve build times.
            // ShaderFeatures.ShadowsKeepOffVariants controls whether the OFF variant is kept or not.
            shaderFeatures |= ShaderFeatures.MainLightShadows;
            if (urpAsset.supportsMainLightShadows && urpAsset.mainLightRenderingMode == LightRenderingMode.PerPixel)
            {
                // User can change cascade count at runtime, so we have to include both of them for now
                shaderFeatures |= ShaderFeatures.MainLightShadowsCascade;

                if (urpAsset.supportsSoftShadows)
                    shaderFeatures |= ShaderFeatures.SoftShadows;
            }

            // Additional Lights and Shadows...
            switch (urpAsset.additionalLightsRenderingMode)
            {
                case LightRenderingMode.PerVertex:
                    shaderFeatures |= ShaderFeatures.AdditionalLightsVertex;
                    break;
                case LightRenderingMode.PerPixel:
                    shaderFeatures |= ShaderFeatures.AdditionalLights;
                    break;
                case LightRenderingMode.Disabled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (urpAsset.lightProbeSystem == LightProbeSystem.ProbeVolumes)
            {
                if (urpAsset.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL1)
                    shaderFeatures |= ShaderFeatures.ProbeVolumeL1;

                if (urpAsset.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                    shaderFeatures |= ShaderFeatures.ProbeVolumeL2;
            }

            if (urpAsset.supportsMixedLighting)
                shaderFeatures |= ShaderFeatures.MixedLighting;

            if (urpAsset.supportsTerrainHoles)
                shaderFeatures |= ShaderFeatures.TerrainHoles;

            if (urpAsset.useFastSRGBLinearConversion)
                shaderFeatures |= ShaderFeatures.UseFastSRGBLinearConversion;

            if (urpAsset.useRenderingLayers)
                shaderFeatures |= ShaderFeatures.LightLayers;

            if (urpAsset.supportsLightCookies)
                shaderFeatures |= ShaderFeatures.LightCookies;

            if (urpAsset.colorGradingMode == ColorGradingMode.HighDynamicRange)
                shaderFeatures |= ShaderFeatures.HdrGrading;

            return shaderFeatures;
        }

        // Checks each Universal Renderer in the assigned URP Asset for features used...
        private static void GetSupportedShaderFeaturesFromRenderers(ref StrippingData data)
        {
            ref UniversalRenderPipelineAsset urpAsset = ref data.urpAsset;

            data.everyRendererHasSSAO = true;
            ScriptableRendererData[] rendererDataArray = urpAsset.m_RendererDataList;
            for (int rendererIndex = 0; rendererIndex < rendererDataArray.Length; ++rendererIndex)
            {
                data.renderer = urpAsset.GetRenderer(rendererIndex);
                data.universalRenderer = (data.renderer != null) ? data.renderer as UniversalRenderer : null;
                data.rendererData = rendererDataArray[rendererIndex];
                data.universalRendererData = (data.rendererData != null) ? data.rendererData as UniversalRendererData : null;
                data.rendererMode = (data.universalRendererData != null) ? data.universalRendererData.renderingMode : RenderingMode.Forward;
                data.isAssetUsing2D |= data.renderer is Renderer2D;

                // Get & add Supported features from renderers used for
                // Scriptable Stripping and update the prefiltering data.
                GetSupportedShaderFeaturesFromRenderer(ref data);
                data.combinedURPAssetShaderFeatures |= data.shaderFeatures;
                s_SupportedFeaturesList.Add(data.shaderFeatures);

                // Check to see if it's possible to remove the OFF variant for SSAO
                data.everyRendererHasSSAO &= IsFeatureEnabled(data.shaderFeatures, ShaderFeatures.ScreenSpaceOcclusion);
            }
        }

        // Checks the assigned Universal renderer for features used...
        private static void GetSupportedShaderFeaturesFromRenderer(ref StrippingData data)
        {
            ref UniversalRenderPipelineAsset urpAsset = ref data.urpAsset;
            data.shaderFeatures = data.urpAssetShaderFeatures;
            ref ShaderFeatures shaderFeatures = ref data.shaderFeatures;

            #if ENABLE_VR && ENABLE_XR_MODULE
            if (data.universalRendererData != null && data.universalRendererData.xrSystemData != null)
                shaderFeatures |= ShaderFeatures.DrawProcedural;
            #endif

            // Rendering Modes
            switch (data.rendererMode)
            {
                case RenderingMode.ForwardPlus:
                    shaderFeatures |= ShaderFeatures.ForwardPlus;
                    break;
                case RenderingMode.Deferred:
                    shaderFeatures |= ShaderFeatures.DeferredShading;
                    break;
                default:
                    data.isAssetUsingForward = true;
                    break;
            }

            // Check renderer features...
            if (data.rendererData != null)
                GetSupportedShaderFeaturesFromRendererFeatures(ref data);

            if (!data.renderer.stripShadowsOffVariants)
                shaderFeatures |= ShaderFeatures.ShadowsKeepOffVariants;

            if (s_KeepOffVariantForAdditionalLights || !data.renderer.stripAdditionalLightOffVariants)
                shaderFeatures |= ShaderFeatures.AdditionalLightsKeepOffVariants;

            if (data.rendererMode == RenderingMode.ForwardPlus)
            {
                shaderFeatures |= ShaderFeatures.AdditionalLightsKeepOffVariants;
                shaderFeatures |= ShaderFeatures.ForwardPlus;
                {
                    shaderFeatures &= ~(ShaderFeatures.AdditionalLights | ShaderFeatures.AdditionalLightsVertex);
                }
            }

            // Additional Light Shadows keyword is always included to improve build times.
            // ShaderFeatures.ShadowsKeepOffVariants controls whether the OFF variant is kept or not.
            shaderFeatures |= ShaderFeatures.AdditionalLightShadows;
            if (urpAsset.supportsAdditionalLightShadows)
            {
                // Soft shadows
                if (urpAsset.additionalLightsRenderingMode == LightRenderingMode.PerPixel || data.rendererMode == RenderingMode.ForwardPlus)
                {
                    if (urpAsset.supportsSoftShadows)
                        shaderFeatures |= ShaderFeatures.SoftShadows;
                }
            }

            if (data.rendererMode == RenderingMode.Deferred)
            {
                if (urpAsset.useRenderingLayers)
                    shaderFeatures |= ShaderFeatures.GBufferWriteRenderingLayers;

                if (data.universalRenderer != null)
                {
                    if (data.universalRenderer.accurateGbufferNormals)
                        shaderFeatures |= ShaderFeatures.AccurateGbufferNormals;

                    if (data.universalRenderer.useRenderPassEnabled)
                        shaderFeatures |= ShaderFeatures.RenderPassEnabled;
                }
            }

            if (urpAsset.reflectionProbeBlending)
                shaderFeatures |= ShaderFeatures.ReflectionProbeBlending;

            if (urpAsset.reflectionProbeBoxProjection)
                shaderFeatures |= ShaderFeatures.ReflectionProbeBoxProjection;
        }

        // Checks each Universal Renderer Feature in the assigned renderer...
        private static void GetSupportedShaderFeaturesFromRendererFeatures(ref StrippingData data)
        {
            bool usesRenderingLayers = false;
            RenderingLayerUtils.Event renderingLayersEvent = RenderingLayerUtils.Event.Opaque;
            bool isDeferredRenderer = (data.rendererMode == RenderingMode.Deferred);
            for (int rendererFeatureIndex = 0; rendererFeatureIndex < data.rendererData.rendererFeatures.Count; rendererFeatureIndex++)
            {
                data.rendererFeature = data.rendererData.rendererFeatures[rendererFeatureIndex];

                // We don't add disabled renderer features if "Strip Unused Variants" is enabled.
                if (s_StripUnusedVariants && !data.rendererFeature.isActive)
                    continue;

                // Rendering Layers...
                if (data.universalRendererData != null && data.rendererFeature.RequireRenderingLayers(isDeferredRenderer, out RenderingLayerUtils.Event rendererEvent, out _))
                {
                    usesRenderingLayers = true;
                    RenderingLayerUtils.CombineRendererEvents(isDeferredRenderer, data.urpAsset.msaaSampleCount, rendererEvent, ref renderingLayersEvent);
                }

                // Check the remaining Renderer Features...
                GetSupportedShaderFeaturesFromRendererFeature(ref data);
            }

            // If using rendering layers, enable the appropriate feature
            if (usesRenderingLayers)
            {
                switch (renderingLayersEvent)
                {
                    case RenderingLayerUtils.Event.DepthNormalPrePass:
                        data.shaderFeatures |= ShaderFeatures.DepthNormalPassRenderingLayers;
                        data.shaderFeatures |=  (data.rendererMode == RenderingMode.Deferred) ? ShaderFeatures.GBufferWriteRenderingLayers : ShaderFeatures.OpaqueWriteRenderingLayers;
                        break;

                    case RenderingLayerUtils.Event.Opaque:
                        data.shaderFeatures |= (data.rendererMode == RenderingMode.Deferred) ? ShaderFeatures.GBufferWriteRenderingLayers : ShaderFeatures.OpaqueWriteRenderingLayers;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        // Checks the assigned Universal Renderer Features for features used...
        private static void GetSupportedShaderFeaturesFromRendererFeature(ref StrippingData data)
        {
            ref ShaderFeatures shaderFeatures = ref data.shaderFeatures;

            // Screen Space Shadows...
            ScreenSpaceShadows sssFeature = data.rendererFeature as ScreenSpaceShadows;
            if (sssFeature != null)
            {
                // Add it if it's enabled or if unused variants should not be stripped...
                if (sssFeature.isActive || !s_StripUnusedVariants)
                    shaderFeatures |= ShaderFeatures.ScreenSpaceShadows;

                return;
            }

            // Screen Space Ambient Occlusion (SSAO)...
            // Removing the OFF variant requires every renderer to use SSAO. That is checked later.
            ScreenSpaceAmbientOcclusion ssaoFeature = data.rendererFeature as ScreenSpaceAmbientOcclusion;
            if (ssaoFeature != null)
            {
                ScreenSpaceAmbientOcclusionSettings ssaoSettings = ssaoFeature.settings;
                data.ssaoRendererFeatures.Add(ssaoSettings);

                // Keep _SCREEN_SPACE_OCCLUSION and the Off variant when stripping of unused variants is disabled
                if (!s_StripUnusedVariants)
                {
                    shaderFeatures |= ShaderFeatures.ScreenSpaceOcclusion;
                    shaderFeatures |= ShaderFeatures.ScreenSpaceOcclusionAfterOpaque;
                }

                // If SSAO feature is active with After Opaque disabled, add it...
                else if (ssaoFeature.isActive && !ssaoSettings.AfterOpaque)
                    shaderFeatures |= ShaderFeatures.ScreenSpaceOcclusion;

                // Otherwise the keyword will not be used

                return;
            }

            // Decals...
            DecalRendererFeature decal = data.rendererFeature as DecalRendererFeature;
            if (decal != null)
            {
                DecalTechnique technique = decal.GetTechnique(data.renderer);
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
        internal static ShaderPrefilteringData CreatePrefilteringSettings(ref StrippingData data, bool stripXR = true, bool stripHDR = true, bool stripDebug = true, bool stripScreenCoord = true)
        {
            ref ShaderFeatures shaderFeatures = ref data.combinedURPAssetShaderFeatures;
            bool isAssetUsingForward = data.isAssetUsingForward;
            bool isAssetUsingForwardPlus = IsFeatureEnabled(shaderFeatures, ShaderFeatures.ForwardPlus);
            bool isAssetUsingDeferred = IsFeatureEnabled(shaderFeatures, ShaderFeatures.DeferredShading);

            ShaderPrefilteringData spd = new ShaderPrefilteringData();
            spd.stripXRKeywords = stripXR;
            spd.stripHDRKeywords = stripHDR;
            spd.stripDebugDisplay = stripDebug;
            spd.stripScreenCoordOverride = stripScreenCoord;

            // Rendering Modes
            // Check if only Deferred is being used
            spd.deferredPrefilteringMode = PrefilteringMode.Remove;
            if (isAssetUsingDeferred)
            {
                // Only Deferred being used...
                if (!isAssetUsingForward && !isAssetUsingForwardPlus)
                    spd.deferredPrefilteringMode = PrefilteringMode.SelectOnly;
                else
                    spd.deferredPrefilteringMode = PrefilteringMode.Select;
            }

            // Check if only Forward+ is being used
            spd.forwardPlusPrefilteringMode = PrefilteringMode.Remove;
            if (isAssetUsingForwardPlus)
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

            if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.AdditionalLights))
            {
                if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.AdditionalLightsKeepOffVariants))
                    spd.additionalLightsPrefilteringMode = PrefilteringModeAdditionalLights.SelectPixelAndOff;
                else
                    spd.additionalLightsPrefilteringMode = PrefilteringModeAdditionalLights.SelectPixel;
            }

            // Shadows...
            // Main Light Shadows...
            spd.mainLightShadowsPrefilteringMode = PrefilteringModeMainLightShadows.SelectMainLight;
            if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.MainLightShadowsCascade))
            {
                if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.ShadowsKeepOffVariants))
                    spd.mainLightShadowsPrefilteringMode = PrefilteringModeMainLightShadows.SelectAll;
                else
                    spd.mainLightShadowsPrefilteringMode = PrefilteringModeMainLightShadows.SelectMainLightAndCascades;
            }
            else if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.ShadowsKeepOffVariants))
                    spd.mainLightShadowsPrefilteringMode = PrefilteringModeMainLightShadows.SelectMainLightAndOff;

            // Additional Light Shadows...
            // The _ADDITIONAL_LIGHT_SHADOWS keyword is always kept.
            // But whether the OFF variant is kept as well is controlled by ShadowsKeepOffVariants
            if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.ShadowsKeepOffVariants))
                spd.additionalLightsShadowsPrefilteringMode = PrefilteringMode.Select;
            else
                spd.additionalLightsShadowsPrefilteringMode = PrefilteringMode.SelectOnly;

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

            // Screen Space Ambient Occlusion
            spd.screenSpaceOcclusionPrefilteringMode = PrefilteringMode.Remove;
            spd.stripSSAODepthNormals      = true;
            spd.stripSSAOSourceDepthLow    = true;
            spd.stripSSAOSourceDepthMedium = true;
            spd.stripSSAOSourceDepthHigh   = true;
            spd.stripSSAOBlueNoise         = true;
            spd.stripSSAOInterleaved       = true;
            spd.stripSSAOSampleCountLow    = true;
            spd.stripSSAOSampleCountMedium = true;
            spd.stripSSAOSampleCountHigh   = true;
            if (IsFeatureEnabled(shaderFeatures, ShaderFeatures.ScreenSpaceOcclusion))
            {
                // Remove the SSAO's OFF variant if Global Settings allow it and every renderer uses it.
                if (s_StripUnusedVariants && data.everyRendererHasSSAO)
                    spd.screenSpaceOcclusionPrefilteringMode = PrefilteringMode.SelectOnly;
                // Otherwise we keep both
                else
                    spd.screenSpaceOcclusionPrefilteringMode = PrefilteringMode.Select;

                ref List<ScreenSpaceAmbientOcclusionSettings> ssaoRendererFeatures = ref data.ssaoRendererFeatures;
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
            }

            return spd;
        }

        // Checks whether a ShaderFeature is enabled or not
        private static bool IsFeatureEnabled(ShaderFeatures featureMask, ShaderFeatures feature)
        {
            return (featureMask & feature) != 0;
        }
    }
}
