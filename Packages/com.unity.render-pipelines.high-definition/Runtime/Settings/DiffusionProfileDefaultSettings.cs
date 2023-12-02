using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [Category("Miscellaneous")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
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
        [Tooltip("When enabled, diffusion profiles referenced by an imported material will be automatically added to the diffusion profile list in the HDRP Global Settings.")]
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
