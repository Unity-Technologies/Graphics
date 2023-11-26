using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    [Category("Miscellaneous")]
    [HideInInspector] // TODO Remove when UI has fully being migrated
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    class SpecularFadeSettings : IRenderPipelineGraphicsSettings
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

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        #region SerializeFields

        [SerializeField]
        [InspectorName("Specular Fade")]
        [Tooltip("When enabled, specular values below 2% will be gradually faded to suppress specular lighting completely. Do note that this behavior is NOT physically correct.")]
        private bool m_SpecularFade;
        #endregion

        #region Data Accessors

        /// <summary>
        /// When enabled, specular values below 2% will be gradually faded to suppress specular lighting completely. Do note that this behavior is NOT physically correct.
        /// </summary>
        public bool enabled
        {
            get => m_SpecularFade;
            set => this.SetValueAndNotify(ref m_SpecularFade, value);
        }

        #endregion
    }
}
