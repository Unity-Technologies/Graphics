using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowQuality = UnityEngine.ShadowQuality;

namespace UnityEditor.Rendering.Universal
{
    internal class RenderSettingsConverter : RenderPipelineConverter
    {
        public override int priority => -9000;
        public override string name => "Renderer and Settings Assets Setup";

        public override string info =>
            "This converter creates Universal Render Pipeline (URP) assets and corresponding Renderer assets, configuring their settings " +
            "to match the equivalent settings from the Built-in Render Pipeline.";


        public override Type container => typeof(BuiltInToURPConverterContainer);

        public override void OnInitialize(InitializeConverterContext context, Action callback)
        {
            QualitySettings.ForEach((index, name) =>
            {
                if (QualitySettings.renderPipeline is not UniversalRenderPipelineAsset)
                {
                    context.AddAssetToConvert(new ConverterItemDescriptor
                        { name = $"[{index}] {name}", info = "Quality Level must reference a Universal Render Pipeline Asset" });
                }
            });

            callback?.Invoke();
        }

        Regex s_Regex = new Regex(@"^\[(\d+)\]", RegexOptions.Compiled);

        public override void OnRun(ref RunItemContext context)
        {
            var item = context.item;
            Match match = s_Regex.Match(item.descriptor.name);
            if (match.Success)
            {
                int index = int.Parse(match.Groups[1].Value);
                if (!CreateURPAssetForQualityLevel(index, out var message))
                {
                    context.didFail = true;
                    context.info = L10n.Tr(message);
                }
            }
            else
            {
                context.didFail = true;
                context.info = L10n.Tr($"Unable to retrieve index from {item.descriptor.name}. Expected : [index] name format.");
            }
        }

        private bool CreateURPAssetForQualityLevel(int qualityIndex, out string message)
        {
            bool ok = false;
            message = string.Empty;

            var currentQualityLevel = QualitySettings.GetQualityLevel();

            QualitySettings.SetQualityLevel(qualityIndex);

            if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                message = $"Quality Level {qualityIndex} already references a Universal Render Pipeline Asset: {urpAsset.name}.";
            }
            else
            {
                var asset = CreateAsset($"{QualitySettings.names[qualityIndex]}");

                if (asset != null)
                {
                    // Map built-in data to the URP asset data
                    SetPipelineSettings(asset);

                    GetRenderers(out var renderers, out var defaultIndex);
                    asset.m_RendererDataList = renderers;
                    asset.m_DefaultRendererIndex = defaultIndex;

                    // Set the asset dirty to make sure that the renderer data is saved
                    EditorUtility.SetDirty(asset);

                    QualitySettings.renderPipeline = asset;
                    ok = true;
                }
                else
                {
                    message = "Failed to create Universal Render Pipeline Asset.";
                }
            }

            // Restore back the quality level
            QualitySettings.SetQualityLevel(currentQualityLevel);

            return ok;
        }

        UniversalRenderPipelineAsset CreateAsset(string name)
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

        UniversalRendererData CreateUniversalRendererDataAsset(RenderingPath renderingPath, RenderingMode renderingMode)
        {
            string path = $"Assets/{UniversalProjectSettings.projectSettingsFolderPath}/Default_{renderingMode}_Renderer.asset";
            if (AssetDatabase.AssetPathExists(path))
                return AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);

            CoreUtils.EnsureFolderTreeInAssetFilePath(path);

            var asset = UniversalRenderPipelineAsset.CreateRendererAsset(path, RendererType.UniversalRenderer, relativePath: false) as UniversalRendererData;
            asset.renderingMode = renderingPath == RenderingPath.Forward ? RenderingMode.Forward : RenderingMode.Deferred;

            return asset;
        }

        void GetRenderers(out ScriptableRendererData[] renderers, out int defaultIndex)
        {
            defaultIndex = 0;

            var targetGrp = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var tier = EditorGraphicsSettings.GetTierSettings(targetGrp, GraphicsTier.Tier3);

            using (ListPool<ScriptableRendererData>.Get(out var tmp))
            {
                var renderingPath = tier.renderingPath;
                var renderingMode = GetEquivalentRenderMode(renderingPath);
                tmp.Add(CreateUniversalRendererDataAsset(renderingPath, renderingMode));

                // In case we need multiple renderers modify the defaultIndex and add more renderers here
                // ...

                renderers = tmp.ToArray();
            }
        }

        private void SetPipelineSettings(UniversalRenderPipelineAsset asset)
        {
            var targetGrp = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var tier = EditorGraphicsSettings.GetTierSettings(targetGrp, GraphicsTier.Tier3);

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
            asset.supportsCameraDepthTexture = softParticles;

            // Quality
            asset.supportsHDR = hdr;
            asset.msaaSampleCount = msaa == 0 ? 1 : msaa;

            // Main Light
            asset.mainLightRenderingMode = pixelLightCount == 0
                ? LightRenderingMode.Disabled
                : LightRenderingMode.PerPixel;
            asset.supportsMainLightShadows = shadows != ShadowQuality.Disable;
            asset.mainLightShadowmapResolution =
                GetEquivalentMainlightShadowResolution((int)shadowResolution);

            // Additional Lights
            asset.additionalLightsRenderingMode = pixelLightCount == 0
                ? LightRenderingMode.PerVertex
                : LightRenderingMode.PerPixel;
            asset.maxAdditionalLightsCount = pixelLightCount != 0 ? Mathf.Max(0, pixelLightCount) : 4;
            asset.supportsAdditionalLightShadows = shadows != ShadowQuality.Disable;
            asset.additionalLightsShadowmapResolution =
                GetEquivalentAdditionalLightAtlasShadowResolution((int)shadowResolution);

            // Reflection Probes
            asset.reflectionProbeBlending = reflectionProbeBlending;
            asset.reflectionProbeBoxProjection = reflectionProbeBoxProjection;

            // Shadows
            asset.shadowDistance = shadowDistance;
            asset.shadowCascadeCount = cascadeShadows ? shadowCascadeCount : 1;
            asset.cascade2Split = cascadeSplit2;
            asset.cascade4Split = cascadeSplit4;
            asset.supportsSoftShadows = shadows == ShadowQuality.All;
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
    }
}
