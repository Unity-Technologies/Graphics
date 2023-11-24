#if UNITY_EDITOR
using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [HideInInspector]
    [Category("Resources/Editor Assets")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class HDRenderPipelineEditorAssets : IRenderPipelineResources
    {
        public int version => 0;

        #region Volume Profiles
        [Header("Volumes")]
        [SerializeField]
        [ResourcePath("Editor/RenderPipelineResources/DefaultSettingsVolumeProfile.asset")]
        private VolumeProfile m_DefaultSettingsVolumeProfile;

        public VolumeProfile defaultVolumeProfile
        {
            get => m_DefaultSettingsVolumeProfile;
            set => this.SetValueAndNotify(ref m_DefaultSettingsVolumeProfile, value);
        }

        [SerializeField]
        [ResourcePath("Editor/RenderPipelineResources/DefaultLookDevProfile.asset")]
        private VolumeProfile m_LookDevDefaultLookDevVolumeProfile;

        public VolumeProfile lookDevVolumeProfile
        {
            get => m_LookDevDefaultLookDevVolumeProfile;
            set => this.SetValueAndNotify(ref m_LookDevDefaultLookDevVolumeProfile, value);
        }
        #endregion

        #region Diffusion Profiles
        [Header("Diffusion Profiles")]
        [ResourcePaths(new[]
        {
            "Runtime/RenderPipelineResources/SkinDiffusionProfile.asset",
            "Runtime/RenderPipelineResources/FoliageDiffusionProfile.asset"
        })]
        [SerializeField]
        private DiffusionProfileSettings[] m_DefaultDiffusionProfileSettingsList;

        public DiffusionProfileSettings[] defaultDiffusionProfileSettingsList
        {
            get => m_DefaultDiffusionProfileSettingsList;
            set => this.SetValueAndNotify(ref m_DefaultDiffusionProfileSettingsList, value);
        }
        #endregion
    }
}
#endif
