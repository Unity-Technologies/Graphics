#if UNITY_EDITOR
using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Editor Assets", Order = 1000), HideInInspector]
    class UniversalRenderPipelineEditorAssets : IRenderPipelineResources
    {
        public int version => 0;

        [SerializeField]
        [ResourcePath("Editor/Volume/DefaultVolumeProfile.asset")]
        private VolumeProfile m_DefaultSettingsVolumeProfile;
        
        public VolumeProfile defaultVolumeProfile
        {
            get => m_DefaultSettingsVolumeProfile;
            set => this.SetValueAndNotify(ref m_DefaultSettingsVolumeProfile, value);
        }
    }
}
#endif
