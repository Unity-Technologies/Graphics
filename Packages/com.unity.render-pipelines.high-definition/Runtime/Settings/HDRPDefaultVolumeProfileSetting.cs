using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Graphics Settings container for the default <see cref="VolumeProfile"/> used by the <see cref="HDRenderPipeline"/>.
    /// </summary>
    /// <remarks>
    /// To change those settings, go to Editor > Project Settings in the Graphics tab (HDRP).
    /// Changing this through API is only allowed in the Editor. In the Player, this raises an error.
    /// </remarks>
    /// <seealso cref="IRenderPipelineGraphicsSettings"/>
    /// <example>
    /// <para> Here is an example of how to get the default volume profile used by HDRP. </para>
    /// <code>
    /// using UnityEngine.Rendering;
    /// using UnityEngine.Rendering.HighDefinition;
    /// 
    /// public static class URPDefaultVolumeProfileHelper
    /// {
    ///     public static VolumeProfile volumeProfile
    ///     {
    ///         get
    ///         {
    ///             var gs = GraphicsSettings.GetRenderPipelineSettings&lt;URPDefaultVolumeProfileSettings&gt;();
    ///             if (gs == null) //not in HDRP
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

        /// <summary>Current version of this settings container. Used only for upgrading the project.</summary>
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
    
#if UNITY_EDITOR
    //Overriding "Reset" in menu that is not called at HDRPDefaultVolumeProfileSettings creation such Reset()
    struct ResetImplementation : IRenderPipelineGraphicsSettingsContextMenu2<HDRPDefaultVolumeProfileSettings>
    {
        public void PopulateContextMenu(HDRPDefaultVolumeProfileSettings setting, SerializedProperty _, ref GenericMenu menu)
        {
            void Reset()
            {
                if (EditorGraphicsSettings.TryGetRenderPipelineSettingsForPipeline<HDRenderPipelineEditorAssets, HDRenderPipeline>(out var rpgs))
                {
                    RenderPipelineGraphicsSettingsEditorUtility.Rebind(
                        new HDRPDefaultVolumeProfileSettings() { volumeProfile = VolumeUtils.CopyVolumeProfileFromResourcesToAssets(rpgs.defaultVolumeProfile) },
                        typeof(HDRenderPipeline)
                    );
                }
            }

            menu.AddItem(new GUIContent("Reset"), false, Reset);
        }
    }
#endif
}
