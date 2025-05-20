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

