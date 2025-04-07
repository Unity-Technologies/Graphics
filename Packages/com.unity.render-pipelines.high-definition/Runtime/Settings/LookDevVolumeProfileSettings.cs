using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A graphics settings container for the <see cref="VolumeProfile"/> used by LookDev with <see cref="HDRenderPipeline"/>.
    /// </summary>
    /// <remarks>
    /// To change those settings, go to Editor > Project Settings in the Graphics tab (HDRP).
    /// Changing this through the API is only allowed in the Editor. In the Player, this raises an error.
    /// 
    /// This container is removed from all build Players.
    /// </remarks>
    /// <seealso cref="IRenderPipelineGraphicsSettings"/>
    /// <example>
    /// <para> Here is an example of how to get the default volume profile used by the LookDev in HDRP. </para>
    /// <code>
    /// using UnityEngine.Rendering;
    /// using UnityEngine.Rendering.HighDefinition;
    /// 
    /// public static class HDRPLookDevVolumeProfileHelper
    /// {
    ///     public static VolumeProfile volumeProfile
    ///     {
    ///         get
    ///         {
    ///             var gs = GraphicsSettings.GetRenderPipelineSettings&lt;LookDevVolumeProfileSettings&gt;();
    ///             if (gs == null) //not in HDRP or in a Player
    ///                 return null;
    ///             return gs.volumeProfile;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
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
                //For some reason, when the one in the HDRP template is modified, all its components are nullified instead of replaced.
                //Removing it fully and creating it solve the issue.
                string path = VolumeUtils.BuildDefaultNameForVolumeProfile(rpgs.lookDevVolumeProfile);
                if (UnityEditor.AssetDatabase.AssetPathExists(path))
                    UnityEditor.AssetDatabase.DeleteAsset(path);

                volumeProfile = VolumeUtils.CopyVolumeProfileFromResourcesToAssets(rpgs.lookDevVolumeProfile);
            }
#endif
        }
    }
}

