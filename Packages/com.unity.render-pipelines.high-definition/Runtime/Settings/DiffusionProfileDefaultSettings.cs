using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Miscellaneous", Order = 100)]
    [Categorization.ElementInfo(Order = 10)]
    class DiffusionProfileDefaultSettings : IRenderPipelineGraphicsSettings
    {
        #region Version
        internal enum Version : int
        {
            Initial = 0,
        }

        [SerializeField][HideInInspector]
        private Version m_Version;

        /// <summary>Current version.</summary>
        public int version => (int)m_Version;
        #endregion

        #region SerializeFields

        [SerializeField]
        [InspectorName("Auto Register Diffusion Profiles")]
        [Tooltip("When enabled, diffusion profiles referenced by an imported material will be automatically added to the Diffusion Profile List under Project Settings > Graphics > HDRP > Default Volume.")]
        private bool m_AutoRegisterDiffusionProfiles;
        #endregion

        #region Data Accessors

        /// <summary>
        /// When enabled, diffusion profiles referenced by an imported material will be automatically added to the diffusion profile list in the HDRP Global Settings.
        /// </summary>
        public bool autoRegister
        {
            get => m_AutoRegisterDiffusionProfiles;
            set => this.SetValueAndNotify(ref m_AutoRegisterDiffusionProfiles, value);
        }

        #endregion
    }
}
