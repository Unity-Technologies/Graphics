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

    private UniversalRenderPipelineAsset m_ChangedAsset;
    private VolumeProfile m_CreatedProfile;
    private ShadowsVolumeOverride m_AddedOverride;
    private ShadowsVolumeOverride m_PrevOverride;

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

        GetShadowSettingsFromAsset(GetURPAsset(QualitySettings.GetQualityLevel()), ref shadowSettings);
        getActiveSettings = false;
    }

    public override void BeforeChangingQualityLevel(int prevQualityLevelIndex, int newQualityLevelIndex)
    {
        if (!CheckIfNextQualityLevelIsValid(newQualityLevelIndex))
            return;

        // Store the settings in the URP Asset that will be used, which will be used to revert once the test finishes
        m_ChangedAsset = GetURPAsset(newQualityLevelIndex);
        GetShadowSettingsFromAsset(m_ChangedAsset, ref prevShadowSettings);
        UpdateShadowmapResolutions(m_ChangedAsset, ref shadowSettings);
        OverrideShadowSettingsInQualityProfile(m_ChangedAsset, ref shadowSettings);
    }

    public override void BeforeRevertingQualityLevel(int prevQualityLevelIndex, int newQualityLevelIndex)
    {
        if (m_ChangedAsset == null)
            return;

        // Revert the shadow changes made previously...
        UpdateShadowmapResolutions(m_ChangedAsset, ref prevShadowSettings);
        RevertQualityProfile(m_ChangedAsset);
        m_ChangedAsset = null;
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

    private static UniversalRenderPipelineAsset GetURPAsset(int qualityLevel)
    {
        return (UniversalRenderPipelineAsset)QualitySettings.GetRenderPipelineAssetAt(qualityLevel);
    }

    private static void GetShadowSettingsFromAsset(UniversalRenderPipelineAsset urpAsset, ref ShadowSettings settings)
    {
        Enum.TryParse($"{urpAsset.mainLightShadowmapResolution}", true, out ShadowResolution mainLightResolution);
        Enum.TryParse($"{urpAsset.additionalLightsShadowmapResolution}", true, out ShadowResolution additionalLightShadowmapResolution);
        settings.mainLightShadowmapResolution = mainLightResolution;
        settings.additionalLightShadowmapResolution = additionalLightShadowmapResolution;

        ShadowsVolumeOverride profileOverride = null;
        bool hasOverride = urpAsset.volumeProfile != null && urpAsset.volumeProfile.TryGet(out profileOverride);
        var shadows = hasOverride ? profileOverride : ScriptableObject.CreateInstance<ShadowsVolumeOverride>();

        float split0 = shadows.cascadeSplit0.value;
        float split1 = shadows.cascadeSplit1.value;
        settings.shadowCascadeCount = shadows.cascadeCount.value;
        settings.cascade2Split = split0;
        settings.cascade3Split = new Vector2(split0, split1);
        settings.cascade4Split = new Vector3(split0, split1, shadows.cascadeSplit2.value);
        settings.cascadeBorder = shadows.lastCascadeBorder.value;
        settings.shadowDistance = shadows.maxShadowDistance.value;

        if (!hasOverride)
            CoreUtils.Destroy(shadows);
    }

    private static void UpdateShadowmapResolutions(UniversalRenderPipelineAsset urpAsset, ref ShadowSettings settings)
    {
        urpAsset.mainLightShadowmapResolution = (int) settings.mainLightShadowmapResolution;
        urpAsset.additionalLightsShadowmapResolution = (int) settings.additionalLightShadowmapResolution;
    }

    private void OverrideShadowSettingsInQualityProfile(UniversalRenderPipelineAsset urpAsset, ref ShadowSettings settings)
    {
        var profile = urpAsset.volumeProfile;
        bool notify = IsUsedByVolumeManager(urpAsset, profile);
        if (profile == null)
        {
            m_CreatedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            urpAsset.volumeProfile = m_CreatedProfile;
            profile = m_CreatedProfile;
        }

        if (profile.TryGet(out ShadowsVolumeOverride shadows))
        {
            m_PrevOverride = Instantiate(shadows);
        }
        else
        {
            shadows = profile.Add<ShadowsVolumeOverride>();
            m_AddedOverride = shadows;
        }

        int cascadeCount = settings.shadowCascadeCount;
        Vector3 splits = settings.cascade4Split;
        if (cascadeCount == 2)
            splits.x = settings.cascade2Split;
        else if (cascadeCount == 3)
            splits = new Vector3(settings.cascade3Split.x, settings.cascade3Split.y, splits.z);

        shadows.SetAllOverridesTo(true);
        shadows.maxShadowDistance.value = settings.shadowDistance;
        shadows.cascadeCount.value = cascadeCount;
        shadows.cascadeSplit0.value = splits.x;
        shadows.cascadeSplit1.value = splits.y;
        shadows.cascadeSplit2.value = splits.z;
        shadows.lastCascadeBorder.value = settings.cascadeBorder;

        if (notify)
            VolumeManager.instance.SetQualityDefaultProfile(profile);
    }

    private void RevertQualityProfile(UniversalRenderPipelineAsset urpAsset)
    {
        var profile = urpAsset.volumeProfile;
        bool notify = IsUsedByVolumeManager(urpAsset, profile);

        if (m_PrevOverride != null)
        {
            if (profile.TryGet(out ShadowsVolumeOverride shadows))
            {
                for (int i = 0; i < shadows.parameters.Count; i++)
                {
                    shadows.parameters[i].SetValue(m_PrevOverride.parameters[i]);
                    shadows.parameters[i].overrideState = m_PrevOverride.parameters[i].overrideState;
                }
            }

            CoreUtils.Destroy(m_PrevOverride);
            m_PrevOverride = null;
        }

        if (m_AddedOverride != null)
            profile.Remove<ShadowsVolumeOverride>();

        if (m_CreatedProfile != null)
            urpAsset.volumeProfile = null;

        if (notify)
            VolumeManager.instance.SetQualityDefaultProfile(urpAsset.volumeProfile);

        if (m_AddedOverride != null)
        {
            CoreUtils.Destroy(m_AddedOverride);
            m_AddedOverride = null;
        }

        if (m_CreatedProfile != null)
        {
            CoreUtils.Destroy(m_CreatedProfile);
            m_CreatedProfile = null;
        }
    }

    private static bool IsUsedByVolumeManager(UniversalRenderPipelineAsset urpAsset, VolumeProfile profile)
    {
        if (!VolumeManager.instance.isInitialized)
            return false;

        return GraphicsSettings.currentRenderPipeline == urpAsset
            || (profile != null && VolumeManager.instance.qualityDefaultProfile == profile);
    }
}
