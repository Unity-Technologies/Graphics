using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution;

public class SetShadowSettings : SetQualityCallbackObject
{
    public bool getActiveSettings;
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
        public float shadowDistance;
    }

    public void OnValidate()
    {
        if (!getActiveSettings)
            return;

        GetShadowSettingsFromAsset(QualitySettings.GetQualityLevel(), ref shadowSettings);
        getActiveSettings = false;
    }

    public override void BeforeChangingQualityLevel(int prevQualityLevelIndex, int newQualityLevelIndex)
    {
        if (!CheckIfNextQualityLevelIsValid(newQualityLevelIndex))
            return;

        // Store the settings in the URP Asset that will be used, which will be used to revert once the test finishes
        GetShadowSettingsFromAsset(newQualityLevelIndex, ref prevShadowSettings);
        UpdateShadowSettingsInURPAsset(newQualityLevelIndex, ref shadowSettings);
    }

    public override void BeforeRevertingQualityLevel(int prevQualityLevelIndex, int newQualityLevelIndex)
    {
        if (!CheckIfNextQualityLevelIsValid(newQualityLevelIndex))
            return;

        // Revert the shadow changes made previously...
        UpdateShadowSettingsInURPAsset(newQualityLevelIndex, ref prevShadowSettings);
    }

    private bool CheckIfNextQualityLevelIsValid(int newQualityLevelIndex)
    {
        string[] qualityLevelNames = QualitySettings.names;
        if (newQualityLevelIndex >= qualityLevelNames.Length)
        {
            Debug.LogError("SetShadowSettings.CheckIfNextQualityLevelIsValid(" + newQualityLevelIndex + "): Quality Level Index is not available!");
            return false;
        }

        return true;
    }

    private void GetShadowSettingsFromAsset(int qualityLevel, ref ShadowSettings settings)
    {
        RenderPipelineAsset asset = QualitySettings.GetRenderPipelineAssetAt(qualityLevel);
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)asset;

        Enum.TryParse($"{urpAsset.mainLightShadowmapResolution}", true, out ShadowResolution mainLightResolution);
        Enum.TryParse($"{urpAsset.additionalLightsShadowmapResolution}", true, out ShadowResolution additionalLightShadowmapResolution);
        settings.mainLightShadowmapResolution = mainLightResolution;
        settings.additionalLightShadowmapResolution = additionalLightShadowmapResolution;
        settings.shadowCascadeCount = urpAsset.shadowCascadeCount;
        settings.cascade2Split = urpAsset.cascade2Split;
        settings.cascade3Split = urpAsset.cascade3Split;
        settings.cascade4Split = urpAsset.cascade4Split;
        settings.cascadeBorder = urpAsset.cascadeBorder;
        settings.shadowDistance = urpAsset.shadowDistance;
    }

    private void UpdateShadowSettingsInURPAsset(int qualityLevel, ref ShadowSettings settings)
    {
        RenderPipelineAsset asset = QualitySettings.GetRenderPipelineAssetAt(qualityLevel);
        UniversalRenderPipelineAsset urpAsset = (UniversalRenderPipelineAsset)asset;

        urpAsset.mainLightShadowmapResolution = (int) settings.mainLightShadowmapResolution;
        urpAsset.additionalLightsShadowmapResolution = (int) settings.additionalLightShadowmapResolution;
        urpAsset.shadowCascadeCount = settings.shadowCascadeCount;
        urpAsset.cascade2Split = settings.cascade2Split;
        urpAsset.cascade3Split = settings.cascade3Split;
        urpAsset.cascade4Split = settings.cascade4Split;
        urpAsset.cascadeBorder = settings.cascadeBorder;
        urpAsset.shadowDistance = settings.shadowDistance;
    }
}
