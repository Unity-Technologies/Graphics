using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Settings class that stores the default volume profile for Volume Framework.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Volume", Order = 0)]
    [Categorization.ElementInfo(Order = 0)]
    public class HDRPDefaultVolumeProfileSettings : IDefaultVolumeProfileSettings
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
        /// The default volume profile asset.
        /// </summary>
        public VolumeProfile volumeProfile
        {
            get => m_VolumeProfile;
            set => this.SetValueAndNotify(ref m_VolumeProfile, value);
        }
    }
}
