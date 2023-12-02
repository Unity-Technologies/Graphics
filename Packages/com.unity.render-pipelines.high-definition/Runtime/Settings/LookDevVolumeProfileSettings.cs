using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Settings class that stores the volume profile for HDRP LookDev.
    /// </summary>
    [Serializable]
    [Category("Volume/LookDev Profile")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    public class LookDevVolumeProfileSettings : IRenderPipelineGraphicsSettings
    {
        #region Version
        internal enum Version : int
        {
            Initial = 0,
        }

        [SerializeField][HideInInspector]
        Version m_Version;

        /// <summary>Current version.</summary>
        public int version => (int)m_Version;
        #endregion

        [SerializeField]
        VolumeProfile m_VolumeProfile;

        /// <summary>
        /// The volume profile to be used for LookDev.
        /// </summary>
        public VolumeProfile volumeProfile
        {
            get => m_VolumeProfile;
            set => this.SetValueAndNotify(ref m_VolumeProfile, value);
        }
    }
}

