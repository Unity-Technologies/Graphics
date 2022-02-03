using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowQuality = UnityEngine.ShadowQuality;
using ShadowResolution = UnityEngine.ShadowResolution;

namespace UnityEditor.Rendering.Universal
{
    internal class RenderSettingsConverter : RenderPipelineConverter
    {
        public override int priority => -9000;
        public override string name => "Rendering Settings";

        public override string info =>
            "This converter will look at creating Universal Render Pipeline assets and respective Renderer Assets and configure" +
            " their settings based on equivalent settings from builtin renderer.";

        public override Type container => typeof(BuiltInToURPConverterContainer);

        // Used to store settings specific to Graphics Tiers
        GraphicsTierSettings m_GraphicsTierSettings;

        // Settings items, currently tracks Quality settings only
        List<SettingsItem> m_SettingsItems;

        // List of the rendering modes required
        List<RenderingMode> m_RenderingModes;

        const string k_PipelineAssetPath = "Settings";

        public override void OnInitialize(InitializeConverterContext context, Action callback)
        {
            m_SettingsItems = new List<SettingsItem>();
            m_RenderingModes = new List<RenderingMode>();

            // check graphics tiers
            GatherGraphicsTiers();

            // check quality levels
            GatherQualityLevels(ref context);

            callback?.Invoke();
        }

        /// <summary>
        /// Grabs the 3rd tier from the Graphics Tier Settings based off the current build platform
        /// </summary>
        private void GatherGraphicsTiers()
        {
            var targetGrp = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var tier = EditorGraphicsSettings.GetTierSettings(targetGrp, GraphicsTier.Tier3);

            // Add the Graphic Tier Render Path settings as the first rendering mode
            m_RenderingModes.Add(GetEquivalentRenderMode(tier.renderingPath));

            m_GraphicsTierSettings.RenderingPath = tier.renderingPath;
            m_GraphicsTierSettings.ReflectionProbeBlending = tier.reflectionProbeBlending;
            m_GraphicsTierSettings.ReflectionProbeBoxProjection = tier.reflectionProbeBoxProjection;
            m_GraphicsTierSettings.CascadeShadows = tier.cascadedShadowMaps;
            m_GraphicsTierSettings.HDR = tier.hdr;
        }

        /// <summary>
        /// Iterates over all Quality Settings and saves relevant settings to a RenderSettingsItem.
        /// This will also create the required information for the Render Pipeline Converter UI.
        /// </summary>
        /// <param name="context">Converter context to add elements to.</param>
        private void GatherQualityLevels(ref InitializeConverterContext context)
        {
            var currentQuality = QualitySettings.GetQualityLevel();
            var id = 0;
            foreach (var levelName in QualitySettings.names)
            {
                QualitySettings.SetQualityLevel(id);

                var projectSettings = new RenderSettingItem
                {
                    Index = id,
                    LevelName = levelName,
                    PixelLightCount = QualitySettings.pixelLightCount,
                    MSAA = QualitySettings.antiAliasing,
                    Shadows = QualitySettings.shadows,
                    ShadowResolution = QualitySettings.shadowResolution,
                    ShadowDistance = QualitySettings.shadowDistance,
                    ShadowCascadeCount = QualitySettings.shadowCascades,
                    CascadeSplit2 = QualitySettings.shadowCascade2Split,
                    CascadeSplit4 = QualitySettings.shadowCascade4Split,
                    SoftParticles = QualitySettings.softParticles,
                };
                m_SettingsItems.Add(projectSettings);

                var setting = QualitySettings.GetRenderPipelineAssetAt(id);
                var item = new ConverterItemDescriptor { name = $"Quality Level {id}: {levelName}" };

                if (setting != null)
                {
                    item.warningMessage = setting.GetType() == typeof(UniversalRenderPipelineAsset)
                        ? "Contains URP Asset, will override existing asset."
                        : "Contains SRP Asset, will override existing asset with URP asset.";
                }

                context.AddAssetToConvert(item);
                id++;
            }

            QualitySettings.SetQualityLevel(currentQuality);
        }

        public override void OnRun(ref RunItemContext context)
        {
            var item = context.item;
            // is quality item
            if (m_SettingsItems[item.index].GetType() == typeof(RenderSettingItem))
            {
                GeneratePipelineAsset(m_SettingsItems[item.index] as RenderSettingItem);
            }
        }

        private void GeneratePipelineAsset(RenderSettingItem settings)
        {
            // store current quality level
            var currentQualityLevel = QualitySettings.GetQualityLevel();

            //creating pipeline asset
            var asset =
                ScriptableObject.CreateInstance(typeof(UniversalRenderPipelineAsset)) as UniversalRenderPipelineAsset;
            if (!AssetDatabase.IsValidFolder($"Assets/{k_PipelineAssetPath}"))
                AssetDatabase.CreateFolder("Assets", k_PipelineAssetPath);
            var path = $"Assets/{k_PipelineAssetPath}/{settings.LevelName}_PipelineAsset.asset";

            // Setting Pipeline Asset settings
            SetPipelineSettings(asset, settings);

            // Create Renderers
            var defaultIndex = 0;
            var renderers = new List<ScriptableRendererData>();
            if (m_RenderingModes.Contains(RenderingMode.Forward))
            {
                renderers.Add(CreateRendererDataAsset(path, RenderingPath.Forward, "ForwardRenderer"));
                if (GetEquivalentRenderMode(m_GraphicsTierSettings.RenderingPath) == RenderingMode.Forward)
                    defaultIndex = m_RenderingModes.IndexOf(RenderingMode.Forward);
            }

            if (m_RenderingModes.Contains(RenderingMode.Deferred))
            {
                renderers.Add(CreateRendererDataAsset(path, RenderingPath.DeferredShading, "DeferredRenderer"));
                if (GetEquivalentRenderMode(m_GraphicsTierSettings.RenderingPath) == RenderingMode.Deferred)
                    defaultIndex = m_RenderingModes.IndexOf(RenderingMode.Deferred);
            }

            asset.m_RendererDataList = renderers.ToArray();
            asset.m_DefaultRendererIndex = defaultIndex;

            // Create Pipeline asset on disk
            AssetDatabase.CreateAsset(asset, path);
            // Assign asset
            QualitySettings.SetQualityLevel(settings.Index);
            QualitySettings.renderPipeline = asset;

            // return to original quality level
            QualitySettings.SetQualityLevel(currentQualityLevel);
            // Set graphics settings
            if (currentQualityLevel == settings.Index || GraphicsSettings.defaultRenderPipeline == null || GraphicsSettings.defaultRenderPipeline.GetType() !=
                typeof(UniversalRenderPipelineAsset))
            {
                GraphicsSettings.defaultRenderPipeline = asset;
            }
        }

        private ScriptableRendererData CreateRendererDataAsset(string assetPath, RenderingPath renderingPath,
            string fileName)
        {
            var rendererAsset =
                UniversalRenderPipelineAsset.CreateRendererAsset(assetPath, RendererType.UniversalRenderer, true, fileName)
                as UniversalRendererData;
            //Missing API to set deferred or forward
            rendererAsset.renderingMode =
                renderingPath == RenderingPath.Forward ? RenderingMode.Forward : RenderingMode.Deferred;
            //missing API to assign to pipeline asset
            return rendererAsset;
        }

        /// <summary>
        /// Sets all relevant RP settings in order they appear in URP
        /// </summary>
        /// <param name="asset">Pipeline asset to set</param>
        /// <param name="settings">The ProjectSettingItem with stored settings</param>
        private void SetPipelineSettings(UniversalRenderPipelineAsset asset, RenderSettingItem settings)
        {
            // General
            asset.supportsCameraDepthTexture = settings.SoftParticles;

            // Quality
            asset.supportsHDR = m_GraphicsTierSettings.HDR;
            asset.msaaSampleCount = settings.MSAA == 0 ? 1 : settings.MSAA;

            // Main Light
            asset.mainLightRenderingMode = settings.PixelLightCount == 0
                ? LightRenderingMode.Disabled
                : LightRenderingMode.PerPixel;
            asset.supportsMainLightShadows = settings.Shadows != ShadowQuality.Disable;
            asset.mainLightShadowmapResolution =
                GetEquivalentMainlightShadowResolution((int)settings.ShadowResolution);

            // Additional Lights
            asset.additionalLightsRenderingMode = settings.PixelLightCount == 0
                ? LightRenderingMode.PerVertex
                : LightRenderingMode.PerPixel;
            asset.maxAdditionalLightsCount = settings.PixelLightCount != 0 ? Mathf.Max(0, settings.PixelLightCount) : 4;
            asset.supportsAdditionalLightShadows = settings.Shadows != ShadowQuality.Disable;
            asset.additionalLightsShadowmapResolution =
                GetEquivalentAdditionalLightAtlasShadowResolution((int)settings.ShadowResolution);

            // Reflection Probes
            asset.reflectionProbeBlending = m_GraphicsTierSettings.ReflectionProbeBlending;
            asset.reflectionProbeBoxProjection = m_GraphicsTierSettings.ReflectionProbeBoxProjection;

            // Shadows
            asset.shadowDistance = settings.ShadowDistance;
            asset.shadowCascadeCount = m_GraphicsTierSettings.CascadeShadows ? settings.ShadowCascadeCount : 1;
            asset.cascade2Split = settings.CascadeSplit2;
            asset.cascade4Split = settings.CascadeSplit4;
            asset.supportsSoftShadows = settings.Shadows == ShadowQuality.All;
        }

        #region HelperFunctions

        private static int GetEquivalentMainlightShadowResolution(int value)
        {
            return GetEquivalentShadowResolution(value);
        }

        private static int GetEquivalentAdditionalLightAtlasShadowResolution(int value)
        {
            return GetEquivalentShadowResolution(value);
        }

        private static int GetEquivalentShadowResolution(int value)
        {
            switch (value)
            {
                case 0: // low
                    return 1024;
                case 1: // med
                    return 2048;
                case 2: // high
                    return 4096;
                case 3: // very high
                    return 4096;
                default: // backup
                    return 1024;
            }
        }

        private RenderingMode GetEquivalentRenderMode(RenderingPath path)
        {
            switch (path)
            {
                case RenderingPath.VertexLit:
                case RenderingPath.Forward:
                    return RenderingMode.Forward;
                case RenderingPath.DeferredShading:
                    return RenderingMode.Deferred;
                default:
                    return RenderingMode.Forward;
            }
        }

        #endregion

        #region Data

        private struct GraphicsTierSettings
        {
            public bool ReflectionProbeBoxProjection;
            public bool ReflectionProbeBlending;
            public bool CascadeShadows;
            public bool HDR;
            public RenderingPath RenderingPath;
        }

        private class SettingsItem { }

        private class RenderSettingItem : SettingsItem
        {
            // General
            public int Index;

            public string LevelName;

            // Settings
            public int PixelLightCount;
            public int MSAA;
            public ShadowQuality Shadows;
            public ShadowResolution ShadowResolution;
            public float ShadowDistance;
            public int ShadowCascadeCount;
            public float CascadeSplit2;
            public Vector3 CascadeSplit4;
            public bool SoftParticles;
        }

        #endregion
    }
}
