using System;
using System.Collections.Generic;
using System.ComponentModel; //needed for list of Custom Post Processes injections
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// High Definition Render Pipeline's Global Settings.
    /// Global settings are unique per Render Pipeline type. In HD, Global Settings contain:
    /// - a default Volume (Global) combined with its Default Profile (defines which components are active by default)
    /// - the default Volume's profile
    /// - the LookDev Volume Profile
    /// - Frame Settings applied by default to Camera, ReflectionProbe
    /// - Various resources (such as Shaders) for runtime, editor-only, and raytracing
    /// </summary>
    [HDRPHelpURL("Default-Settings-Window")]
    [DisplayInfo(name = "HDRP Global Settings Asset", order = CoreUtils.Sections.section4 + 2)]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [DisplayName("HDRP")]
    partial class HDRenderPipelineGlobalSettings : RenderPipelineGlobalSettings<HDRenderPipelineGlobalSettings, HDRenderPipeline>
    {
        [SerializeField] RenderPipelineGraphicsSettingsContainer m_Settings = new();
        protected override List<IRenderPipelineGraphicsSettings> settingsList => m_Settings.settingsList;
        internal bool HasSetting(Type type)
        {
            if (settingsList == null)
                return false;

            foreach (var setting in settingsList)
            {
                if (setting.GetType() == type)
                    return true;
            }

            return false;
        }

        internal bool ContainsSetting(Type type) => Contains(type);

#if UNITY_EDITOR
        internal static string defaultPath => $"Assets/{HDProjectSettingsReadOnlyBase.projectSettingsFolderPath}/HDRenderPipelineGlobalSettings.asset";

        //Making sure there is at least one HDRenderPipelineGlobalSettings instance in the project
        internal static HDRenderPipelineGlobalSettings Ensure(bool canCreateNewAsset = true)
        {
            HDRenderPipelineGlobalSettings currentInstance = GraphicsSettings.
                GetSettingsForRenderPipeline<HDRenderPipeline>() as HDRenderPipelineGlobalSettings;

            if (currentInstance == null || currentInstance.Equals(null) || currentInstance.m_Version == Version.First)
            {
                // Try to migrate HDRPAsset in Graphics. It can produce a HDRenderPipelineGlobalSettings
                // with data from former HDRPAsset if it is at a version allowing this.
                if (GraphicsSettings.defaultRenderPipeline is HDRenderPipelineAsset hdrpAsset &&
                    hdrpAsset is IMigratableAsset migratableAsset &&
                    hdrpAsset.IsVersionBelowAddedHDRenderPipelineGlobalSettings())
                {
                    // if possible we need to finish migration of hdrpAsset in order to grab value from it
                    migratableAsset.Migrate();

                    // the migration of the HDRP asset has updated the current instance
                    currentInstance =
                        GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>() as
                            HDRenderPipelineGlobalSettings;
                }
            }

            if (RenderPipelineGlobalSettingsUtils.TryEnsure<HDRenderPipelineGlobalSettings, HDRenderPipeline>(ref currentInstance, defaultPath, canCreateNewAsset))
            {
                if (currentInstance is IMigratableAsset migratableAsset && !migratableAsset.IsAtLastVersion())
                {
                    migratableAsset.Migrate();
                    EditorUtility.SetDirty(currentInstance);
                    AssetDatabase.SaveAssetIfDirty(currentInstance);
                }

                return currentInstance;
            }

            return null;
        }

        public override void Initialize(RenderPipelineGlobalSettings source = null)
        {
            SetUpRPAssetIncluded();

            TryGet(typeof(HDRenderPipelineEditorAssets), out var editorAssets);
            var assets = editorAssets as HDRenderPipelineEditorAssets;

            if (TryGet(typeof(HDRPDefaultVolumeProfileSettings), out var defaultSettings) &&
                defaultSettings is HDRPDefaultVolumeProfileSettings defaultVolumeProfileSettings)
            {
                if (defaultVolumeProfileSettings.volumeProfile == null && assets != null)
                    defaultVolumeProfileSettings.volumeProfile = VolumeUtils.CopyVolumeProfileFromResourcesToAssets(assets.defaultVolumeProfile);

                // Initialize the Volume Profile with the default diffusion profiles
                var diffusionProfileList = VolumeUtils.GetOrCreateDiffusionProfileList(defaultVolumeProfileSettings.volumeProfile);

                if (diffusionProfileList.diffusionProfiles.value.Length == 0)
                {
                    diffusionProfileList.diffusionProfiles.value = VolumeUtils.CreateArrayWithDefaultDiffusionProfileSettingsList(assets);
                    EditorUtility.SetDirty(diffusionProfileList);
                }
            }

            if (TryGet(typeof(LookDevVolumeProfileSettings), out var lookDevSettings) &&
                lookDevSettings is LookDevVolumeProfileSettings lookDevVolumeProfileSettings &&
                assets != null)
            {
                lookDevVolumeProfileSettings.volumeProfile ??= VolumeUtils.CopyVolumeProfileFromResourcesToAssets(assets.lookDevVolumeProfile);
            }
        }

        void SetUpRPAssetIncluded()
        {
            if (!TryGet(typeof(IncludeAdditionalRPAssets), out var rpgs) || rpgs is not IncludeAdditionalRPAssets includer)
            {
                Debug.Log($"Missing {nameof(IncludeAdditionalRPAssets)} set up for HDRP.");
                return;
            }

            includer.includeReferencedInScenes = true;
            includer.includeAssetsByLabel = true;
            includer.labelToInclude = HDUtils.k_HdrpAssetBuildLabel;
        }

#endif // UNITY_EDITOR

        #region Rendering Layer Mask
        [SerializeField]
        [Obsolete ("Kept For Migration")]
        internal string[] renderingLayerNames = { "Default" };

        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName0;
        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName1;
        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName2;
        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName3;
        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName4;
        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName5;
        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName6;
        [SerializeField, Obsolete("Kept For Migration")] string lightLayerName7;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName0;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName1;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName2;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName3;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName4;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName5;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName6;
        [SerializeField, Obsolete("Kept For Migration")] string decalLayerName7;

        #endregion

        #region Misc.

        #endregion

        #region APV

#pragma warning disable 618
#pragma warning disable 612
        // This is temporarily here until we have a core place to put it shared between pipelines.
        [SerializeField]
        internal ProbeVolumeSceneData apvScenesData;

        internal ProbeVolumeSceneData GetOrCreateAPVSceneData()
        {
            if (apvScenesData == null)
                apvScenesData = new ProbeVolumeSceneData(this);

            apvScenesData.SetParentObject(this);
            return apvScenesData;
        }
#pragma warning restore 612
#pragma warning restore 618

        #endregion
    }
}
