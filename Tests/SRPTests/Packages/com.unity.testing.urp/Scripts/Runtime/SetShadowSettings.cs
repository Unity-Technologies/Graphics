using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution;

public class SetShadowSettings : SetQualityCallbackObject
{
    public ShadowSettings shadowSettings;
    private ShadowSettings prevShadowSettings;

    [Serializable]
    public struct ShadowSettings
    {
        public ShadowResolution mainLightShadowmapResolution;
        public ShadowResolution additionalLightShadowmapResolution;

        public int shadowCascadeCount;
        public float cascade2Split;
        public Vector2 cascade3Split;
        public Vector3 cascade4Split;
        public float cascadeBorder;
    }

    public override void BeforeChangingQualityLevel(int prevQualityLevelIndex, int newQualityLevelIndex)
    {
        var qualityLevelNames = QualitySettings.names;
        if (newQualityLevelIndex >= qualityLevelNames.Length)
        {
            Debug.LogError("SetShadowSettings.BeforeChangingQualityLevel: Quality Level Index " + newQualityLevelIndex + " is not available!");
            return;
        }

        RenderPipelineAsset asset = QualitySettings.GetRenderPipelineAssetAt(newQualityLevelIndex);
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)asset;

        Enum.TryParse($"{urpAsset.mainLightShadowmapResolution}", true, out ShadowResolution mainLightResolution);
        Enum.TryParse($"{urpAsset.additionalLightsShadowmapResolution}", true, out ShadowResolution additionalLightShadowmapResolution);
        prevShadowSettings = new ShadowSettings()
        {
            mainLightShadowmapResolution = mainLightResolution,
            additionalLightShadowmapResolution = additionalLightShadowmapResolution,
            shadowCascadeCount = urpAsset.shadowCascadeCount,
            cascade2Split = urpAsset.cascade2Split,
            cascade3Split = urpAsset.cascade3Split,
            cascade4Split = urpAsset.cascade4Split,
            cascadeBorder = urpAsset.cascadeBorder
        };

        UpdateShadowSettingsInURPAsset(urpAsset, shadowSettings);
    }

    public override void BeforeRevertingQualityLevel(int prevQualityLevelIndex, int newQualityLevelIndex)
    {
        var qualityLevelNames = QualitySettings.names;
        if (newQualityLevelIndex >= qualityLevelNames.Length)
        {
            Debug.LogError("SetShadowSettings.BeforeRevertingQualityLevel: Quality Level Index " + newQualityLevelIndex + " is not available!");
            return;
        }

        RenderPipelineAsset asset = QualitySettings.GetRenderPipelineAssetAt(newQualityLevelIndex);
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)asset;

        UpdateShadowSettingsInURPAsset(urpAsset, prevShadowSettings);
    }

    private void UpdateShadowSettingsInURPAsset(UniversalRenderPipelineAsset urpAsset, ShadowSettings settings)
    {
        urpAsset.mainLightShadowmapResolution = (int) settings.mainLightShadowmapResolution;
        urpAsset.additionalLightsShadowmapResolution = (int) settings.additionalLightShadowmapResolution;
        urpAsset.shadowCascadeCount = settings.shadowCascadeCount;
        urpAsset.cascade2Split = settings.cascade2Split;
        urpAsset.cascade3Split = settings.cascade3Split;
        urpAsset.cascade4Split = settings.cascade4Split;
        urpAsset.cascadeBorder = settings.cascadeBorder;
    }
}
