using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Settings class that stores the default volume profile for Volume Framework.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Volume", Order = 0)]
    public class URPDefaultVolumeProfileSettings : IDefaultVolumeProfileSettings
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
    
#if UNITY_EDITOR
    //Overriding "Reset" in menu that is not called at URPDefaultVolumeProfileSettings creation such Reset()
    struct ResetImplementation : IRenderPipelineGraphicsSettingsContextMenu2<URPDefaultVolumeProfileSettings>
    {
        public void PopulateContextMenu(URPDefaultVolumeProfileSettings setting, SerializedProperty _, ref GenericMenu menu)
        {
            void Reset()
            {
                static VolumeProfile CopyVolumeProfileFromResourcesToAssets(VolumeProfile profileInResourcesFolder)
                {
                    if (profileInResourcesFolder == null)
                        return null;

                    const string k_DefaultVolumeProfileName = "DefaultVolumeProfile";
                    const string k_DefaultVolumeProfilePath = "Assets/" + k_DefaultVolumeProfileName + ".asset";

                    var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(k_DefaultVolumeProfilePath);
                    if (profile == null)
                    {
                        CoreUtils.EnsureFolderTreeInAssetFilePath(k_DefaultVolumeProfilePath);
                        AssetDatabase.CopyAsset(UnityEditor.AssetDatabase.GetAssetPath(profileInResourcesFolder), k_DefaultVolumeProfilePath);
                        profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(k_DefaultVolumeProfilePath);
                    }
                    else
                    {
                        profile.components.Clear();
                        foreach (var resourceComponent in profileInResourcesFolder.components)
                        {
                            var c = profile.Add(resourceComponent.GetType());
                            for (int i = 0; i < c.parameters.Count; i++)
                            {
                                c.parameters[i].SetValue(resourceComponent.parameters[i]);
                            }
                        }

                    }

                    return profile;
                }

                if (EditorGraphicsSettings.TryGetRenderPipelineSettingsForPipeline<UniversalRenderPipelineEditorAssets, UniversalRenderPipeline>(out var rpgs))
                {
                    RenderPipelineGraphicsSettingsEditorUtility.Rebind(
                        new URPDefaultVolumeProfileSettings() { volumeProfile = CopyVolumeProfileFromResourcesToAssets(rpgs.defaultVolumeProfile) },
                        typeof(UniversalRenderPipeline)
                    );
                }
            }

            menu.AddItem(new GUIContent("Reset"), false, Reset);
        }
    }
#endif
}
