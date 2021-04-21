using System.IO;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDAssetFactory
    {
        internal class HDRenderPipelineGlobalSettingsCreator : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var newAsset = HDRenderPipelineGlobalSettings.Create(pathName, settings);
                if (updateGraphicsSettings)
                    HDRenderPipelineGlobalSettings.UpdateGraphicsSettings(newAsset);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }

            static HDRenderPipelineGlobalSettings settings;
            static bool updateGraphicsSettings = false;
            public static void Clone(HDRenderPipelineGlobalSettings src, bool activateAsset)
            {
                settings = src;
                updateGraphicsSettings = activateAsset;
                var assetCreator = ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettingsCreator>();

                if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                    AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);
                var path = "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/" + src.name + ".asset";

                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(assetCreator.GetInstanceID(), assetCreator, path, CoreEditorStyles.globalSettingsIcon, null);
            }

            public static void Create(bool useProjectSettingsFolder, bool activateAsset)
            {
                settings = null;
                updateGraphicsSettings = activateAsset;

                var path = "HDRenderPipelineGlobalSettings.asset";
                if (useProjectSettingsFolder)
                {
                    if (!AssetDatabase.IsValidFolder("Assets/" + HDProjectSettings.projectSettingsFolderPath))
                        AssetDatabase.CreateFolder("Assets", HDProjectSettings.projectSettingsFolderPath);
                    path = "Assets/" + HDProjectSettings.projectSettingsFolderPath + "/HDRenderPipelineGlobalSettings.asset";
                }
                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettingsCreator>(), path, CoreEditorStyles.globalSettingsIcon, null);
            }
        }

        [MenuItem("Assets/Create/Rendering/HDRP Global Settings Asset", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 1)]
        internal static void CreateHDRenderPipelineGlobalSettings()
        {
            HDRenderPipelineGlobalSettingsCreator.Create(useProjectSettingsFolder: false, activateAsset: false);
        }
    }
}
