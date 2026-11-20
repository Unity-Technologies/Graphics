using System;
using System.Collections.Generic;
using Unity.RenderPipelines.Core.Runtime.Shared;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Categorization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowQuality = UnityEngine.ShadowQuality;

namespace UnityEditor.Rendering.Universal
{
    [Serializable]
    [URPHelpURL("urp/features/rp-converter")]
    [PipelineConverter("Built-in", "Universal Render Pipeline (Universal Renderer)")]
    [BatchModeConverterClassInfo("BuiltInToURP", "RenderSettings")]
    [ElementInfo(Name = "Rendering Settings",
                 Order = int.MinValue,
                 Description = "This converter creates Universal Render Pipeline (URP) assets and corresponding Renderer assets, configuring their settings to match the equivalent settings from the Built-in Render Pipeline.")]
    class BuiltInToURP3DRenderSettingsConverter : RenderSettingsConverter
    {
        public override bool isEnabled => true;

        public override string isDisabledMessage => string.Empty;

        protected override RenderPipelineAsset CreateAsset(string name)
        {
            string path = $"Assets/{UniversalProjectSettings.projectSettingsFolderPath}/{name}.asset";
            if (AssetDatabase.AssetPathExists(path))
                return AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);

            try
            {
                CoreUtils.EnsureFolderTreeInAssetFilePath(path);
                var asset = ScriptableObject.CreateInstance(typeof(UniversalRenderPipelineAsset)) as UniversalRenderPipelineAsset;
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssetIfDirty(asset);
                return asset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unable to create asset at path {path} with exception {ex.Message}");
                return null;
            }
        }

#pragma warning disable CS0618 // Type or member is obsolete
        UniversalRendererData CreateUniversalRendererDataAsset(RenderingPath renderingPath, RenderingMode renderingMode)
        {
            string path = $"Assets/{UniversalProjectSettings.projectSettingsFolderPath}/Default_{renderingMode}_Renderer.asset";
            if (AssetDatabase.AssetPathExists(path))
                return AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);

            CoreUtils.EnsureFolderTreeInAssetFilePath(path);

            var asset = UniversalRenderPipelineAsset.CreateRendererAsset(path, RendererType.UniversalRenderer, relativePath: false) as UniversalRendererData;
            asset.renderingMode = renderingPath == RenderingPath.Forward ? RenderingMode.Forward : RenderingMode.Deferred;

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);

            return asset;
        }
#pragma warning restore CS0618

        void GetRenderers(out ScriptableRendererData[] renderers, out int defaultIndex)
        {
            defaultIndex = 0;

            var targetGrp = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
#pragma warning disable CS0618 // Type or member is obsolete
            var tier = EditorGraphicsSettings.GetTierSettings(targetGrp, GraphicsTier.Tier3);
#pragma warning restore CS0618

            using (UnityEngine.Pool.ListPool<ScriptableRendererData>.Get(out var tmp))
            {
                var renderingPath = tier.renderingPath;
                var renderingMode = GetEquivalentRenderMode(renderingPath);
                tmp.Add(CreateUniversalRendererDataAsset(renderingPath, renderingMode));

                // In case we need multiple renderers modify the defaultIndex and add more renderers here
                // ...

                renderers = tmp.ToArray();
            }

            // Tell the asset database to regenerate the fileId, otherwise when adding the reference to the URP
            // asset the fileId might not be computed and the reference might be lost.
            AssetDatabase.Refresh();
        }

        protected override void SetPipelineSettings(RenderPipelineAsset asset)
        {
            if (asset is not UniversalRenderPipelineAsset urpAsset)
                return;

            GetRenderers(out var renderers, out var defaultIndex);
            urpAsset.m_RendererDataList = renderers;
            urpAsset.m_DefaultRendererIndex = defaultIndex;

            var targetGrp = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
#pragma warning disable CS0618 // Type or member is obsolete
            var tier = EditorGraphicsSettings.GetTierSettings(targetGrp, GraphicsTier.Tier3);
#pragma warning restore CS0618

            var pixelLightCount  = QualitySettings.pixelLightCount;
            var shadows          = QualitySettings.shadows;
            var shadowResolution = QualitySettings.shadowResolution;
            var shadowDistance   = QualitySettings.shadowDistance;
            
            var reflectionProbeBlending = tier.reflectionProbeBlending;
            var reflectionProbeBoxProjection = tier.reflectionProbeBoxProjection;
            bool cascadeShadows = tier.cascadedShadowMaps;
            var shadowCascadeCount = QualitySettings.shadowCascades;
            var cascadeSplit2 = QualitySettings.shadowCascade2Split;
            var cascadeSplit4 = QualitySettings.shadowCascade4Split;

            bool hdr = tier.hdr;
            var msaa = QualitySettings.antiAliasing;
            var softParticles = QualitySettings.softParticles;

            // General
            urpAsset.supportsCameraDepthTexture = softParticles;

            // Quality
            urpAsset.supportsHDR = hdr;
            urpAsset.msaaSampleCount = msaa == 0 ? 1 : msaa;

            // Main Light
            urpAsset.mainLightRenderingMode = pixelLightCount == 0
                ? LightRenderingMode.Disabled
                : LightRenderingMode.PerPixel;
            urpAsset.supportsMainLightShadows = shadows != ShadowQuality.Disable;
            urpAsset.mainLightShadowmapResolution =
                GetEquivalentMainlightShadowResolution((int)shadowResolution);

            // Additional Lights
            urpAsset.additionalLightsRenderingMode = pixelLightCount == 0
                ? LightRenderingMode.PerVertex
                : LightRenderingMode.PerPixel;
            urpAsset.maxAdditionalLightsCount = pixelLightCount != 0 ? Mathf.Max(0, pixelLightCount) : 4;
            urpAsset.supportsAdditionalLightShadows = shadows != ShadowQuality.Disable;
            urpAsset.additionalLightsShadowmapResolution =
                GetEquivalentAdditionalLightAtlasShadowResolution((int)shadowResolution);

            // Light Falloff
            UniversalRenderPipelineGlobalSettings.Ensure();
            if (EditorGraphicsSettings.TryGetRenderPipelineSettingsForPipeline<URPLightFalloffSettings, UniversalRenderPipeline>(out var lightFalloffSettings))
                lightFalloffSettings.lightFalloffMode = LightFalloffMode.Quadratic;

            // Reflection Probes
            urpAsset.reflectionProbeBlending = reflectionProbeBlending;
            urpAsset.reflectionProbeBoxProjection = reflectionProbeBoxProjection;

            // Shadows
            urpAsset.supportsSoftShadows = shadows == ShadowQuality.All;
            ConvertShadowDistanceAndCascades(urpAsset, shadowDistance, cascadeShadows ? shadowCascadeCount : 1, cascadeSplit2, cascadeSplit4);

            WarnIfUsedElsewhere(urpAsset);
        }

        static void ConvertShadowDistanceAndCascades(UniversalRenderPipelineAsset urpAsset, float shadowDistance, int cascadeCount, float cascade2Split, Vector3 cascade4Split)
        {
#pragma warning disable 618 // Type or member is obsolete
            var values = UniversalRenderPipelineGlobalSettings.ShadowSettingsValues.FromCascadeSplitSets(
                shadowDistance, cascadeCount, cascade2Split, urpAsset.m_ObsoleteCascade3Split, cascade4Split, urpAsset.m_ObsoleteCascadeBorder);
#pragma warning restore 618

            var source = urpAsset.volumeProfile;
            var profile = UniversalRenderPipelineGlobalSettings.CreateQualityVolumeProfile<ShadowsVolumeOverride>(urpAsset);
            if (profile == null || !UniversalRenderPipelineGlobalSettings.WriteShadowSettings(profile, values) || source == null)
                return;

            EditorGraphicsSettings.TryGetRenderPipelineSettingsForPipeline<URPDefaultVolumeProfileSettings, UniversalRenderPipeline>(out var defaultProfileSettings);
            string description = UniversalRenderPipelineGlobalSettings.DescribeVolumeProfileCopy(
                new List<UniversalRenderPipelineAsset> { urpAsset }, profile, source, defaultProfileSettings?.volumeProfile);
            string message = $"URP: The pipeline asset {description}, so later changes to '{AssetLinkUtils.AssetLink(source)}' no longer apply to it.";

            var remainingUsers = UniversalRenderPipelineGlobalSettings.FindPipelineAssets().FindAll(other => other.volumeProfile == source);
            if (remainingUsers.Count > 0)
                message += $" {UniversalRenderPipelineGlobalSettings.DescribeRemainingUsers(source, remainingUsers)}.";

            Debug.LogWarning(message);
        }

        static void WarnIfUsedElsewhere(UniversalRenderPipelineAsset urpAsset)
        {
            var qualityLevels = new List<string>();
            for (int i = 0; i < QualitySettings.names.Length; ++i)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(i) == urpAsset)
                    qualityLevels.Add($"'{QualitySettings.names[i]}'");
            }

            string assetLink = AssetLinkUtils.AssetLink(urpAsset);
            if (qualityLevels.Count == 1)
                Debug.LogWarning($"URP: The pipeline asset '{assetLink}' is also used by the quality level {qualityLevels[0]}, so converting '{assetLink}' changed {qualityLevels[0]} as well.");
            else if (qualityLevels.Count > 1)
                Debug.LogWarning($"URP: The pipeline asset '{assetLink}' is also used by the quality levels {string.Join(", ", qualityLevels)}, so converting '{assetLink}' changed them as well.");

            if (GraphicsSettings.defaultRenderPipeline == urpAsset)
                Debug.LogWarning($"URP: The pipeline asset '{assetLink}' is also used as the default render pipeline in Graphics settings, so converting '{assetLink}' changed it as well.");
        }

        #region HelperFunctions

        internal static int GetEquivalentMainlightShadowResolution(int value)
        {
            return GetEquivalentShadowResolution(value);
        }

        internal static int GetEquivalentAdditionalLightAtlasShadowResolution(int value)
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

#pragma warning disable CS0618 // Type or member is obsolete
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
#pragma warning restore CS0618

        #endregion
    }
}
