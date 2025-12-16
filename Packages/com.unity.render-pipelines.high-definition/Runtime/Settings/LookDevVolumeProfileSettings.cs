using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Settings class that stores the volume profile for HDRP LookDev.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Volume", Order = 0)]
    [Categorization.ElementInfo(Order = 10)]
    public class LookDevVolumeProfileSettings : IRenderPipelineGraphicsSettings
    {
        #region Version
        internal enum Version : int
        {
            Initial = 0,
        }

        [SerializeField][HideInInspector]
        Version m_Version;

        /// <summary>Current version of these settings container. Used only for upgrading a project.</summary>
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

        void IRenderPipelineGraphicsSettings.Reset()
        {
#if UNITY_EDITOR
            if (UnityEditor.Rendering.EditorGraphicsSettings.TryGetRenderPipelineSettingsForPipeline<HDRenderPipelineEditorAssets, HDRenderPipeline>(out var rpgs))
            {
                //UUM-100350
                //When opening the new HDRP project from the template the first time, the global settings is created and the population of IRenderPipelineGraphicsSettings
                //will call this Reset() method. At this time, the copied item will appear ok but will be seen as null soon after. This lead to errors when opening the 
                //inspector of the LookDev's VolumeProfile (at the creation of Editors for VolumeComponent). Closing and opening the project would make this issue disappear.
                //This asset data base manipulation issue disappear if we delay it.
                UnityEditor.EditorApplication.delayCall += () =>
                    volumeProfile = VolumeUtils.CopyVolumeProfileFromResourcesToAssets(rpgs.lookDevVolumeProfile);
            }
#endif
        }
    }
}

