using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ChangeShadowCascadeSettings : MonoBehaviour
{
    public int shadowCascadeCount;

    public float cascade2Split = 0.25f;
    public Vector2 cascade3Split = new Vector2(0.154f, 0.478f);
    public Vector3 cascade4Split = new Vector3(0.067f, 0.2f, 0.467f);
    public float maxShadowDistance;

    UniversalRenderPipelineAsset m_Asset;
    VolumeProfile m_CreatedProfile;
    ShadowsVolumeOverride m_AddedOverride;
    ShadowsVolumeOverride m_PrevOverride;

    void Awake()
    {
        m_Asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        var profile = m_Asset.volumeProfile;
        if (profile == null)
        {
            m_CreatedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            m_Asset.volumeProfile = m_CreatedProfile;
            profile = m_CreatedProfile;
        }

        if (profile.TryGet(out ShadowsVolumeOverride shadows))
            m_PrevOverride = Instantiate(shadows);
        else
            m_AddedOverride = shadows = profile.Add<ShadowsVolumeOverride>();

        Vector3 splits = cascade4Split;
        if (shadowCascadeCount == 2)
            splits.x = cascade2Split;
        else if (shadowCascadeCount == 3)
            splits = new Vector3(cascade3Split.x, cascade3Split.y, splits.z);

        shadows.cascadeCount.Override(shadowCascadeCount);
        shadows.cascadeSplit0.Override(splits.x);
        shadows.cascadeSplit1.Override(splits.y);
        shadows.cascadeSplit2.Override(splits.z);
        if (maxShadowDistance > 0.0f)
            shadows.maxShadowDistance.Override(maxShadowDistance);

        NotifyVolumeManager();
    }

    void OnDestroy()
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
