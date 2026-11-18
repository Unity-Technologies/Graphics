using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public sealed class QualityProfileShadowDistance : IDisposable
{
    UniversalRenderPipelineAsset m_Asset;
    VolumeProfile m_CreatedProfile;
    ShadowsVolumeOverride m_AddedOverride;
    ShadowsVolumeOverride m_PrevOverride;

    public QualityProfileShadowDistance(UniversalRenderPipelineAsset urpAsset, float maxShadowDistance)
    {
        m_Asset = urpAsset;

        var profile = urpAsset.volumeProfile;
        if (profile == null)
        {
            m_CreatedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            urpAsset.volumeProfile = m_CreatedProfile;
            profile = m_CreatedProfile;
        }

        if (profile.TryGet(out ShadowsVolumeOverride shadows))
            m_PrevOverride = Object.Instantiate(shadows);
        else
            m_AddedOverride = shadows = profile.Add<ShadowsVolumeOverride>();

        shadows.maxShadowDistance.Override(maxShadowDistance);
        NotifyVolumeManager();
    }

    public void Dispose()
    {
        if (m_Asset == null)
            return;

        var profile = m_Asset.volumeProfile;

        if (m_PrevOverride != null && profile.TryGet(out ShadowsVolumeOverride shadows))
        {
            for (int i = 0; i < shadows.parameters.Count; i++)
            {
                shadows.parameters[i].SetValue(m_PrevOverride.parameters[i]);
                shadows.parameters[i].overrideState = m_PrevOverride.parameters[i].overrideState;
            }
        }

        if (m_AddedOverride != null)
            profile.Remove<ShadowsVolumeOverride>();

        if (m_CreatedProfile != null)
            m_Asset.volumeProfile = null;

        NotifyVolumeManager();

        if (m_PrevOverride != null)
            CoreUtils.Destroy(m_PrevOverride);
        if (m_AddedOverride != null)
            CoreUtils.Destroy(m_AddedOverride);
        if (m_CreatedProfile != null)
            CoreUtils.Destroy(m_CreatedProfile);
        m_Asset = null;
    }

    void NotifyVolumeManager()
    {
        if (!VolumeManager.instance.isInitialized)
            return;

        if (m_CreatedProfile == null)
            VolumeManager.instance.OnVolumeProfileChanged(m_Asset.volumeProfile);
        else if (GraphicsSettings.currentRenderPipeline == m_Asset)
            VolumeManager.instance.SetQualityDefaultProfile(m_Asset.volumeProfile);
    }
}
