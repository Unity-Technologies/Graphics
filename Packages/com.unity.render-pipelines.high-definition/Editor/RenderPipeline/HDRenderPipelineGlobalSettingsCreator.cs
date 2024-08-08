using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using System.Reflection;
using System;

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

            private static string GetCurrentOpenedPath()
            {
                Type projectWindowUtilType = typeof(ProjectWindowUtil);
                MethodInfo getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
                object obj = getActiveFolderPath.Invoke(null, new object[0]);
                return obj.ToString();
            }

            public static void Clone(HDRenderPipelineGlobalSettings src, bool assignToActiveAsset)
            {
                settings = src;
                updateGraphicsSettings = assignToActiveAsset;
                var assetCreator = ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettingsCreator>();

                var path = GetCurrentOpenedPath() + $"/{src.name}.asset";
                CoreUtils.EnsureFolderTreeInAssetFilePath(path);

                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(assetCreator.GetInstanceID(), assetCreator, path, CoreEditorStyles.globalSettingsIcon, null);
            }

            public static void Create(bool useProjectSettingsFolder, bool assignToActiveAsset)
            {
                settings = null;
                updateGraphicsSettings = assignToActiveAsset;

                string path = (useProjectSettingsFolder) ?
                    $"Assets/{HDProjectSettings.projectSettingsFolderPath}/HDRenderPipelineGlobalSettings.asset" :
                    GetCurrentOpenedPath() + "/HDRenderPipelineGlobalSettings.asset";
                CoreUtils.EnsureFolderTreeInAssetFilePath(path);

                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettingsCreator>(), path, CoreEditorStyles.globalSettingsIcon, null);
            }
        }

        [MenuItem("Assets/Create/Rendering/HDRP Global Settings Asset", priority = CoreUtils.Sections.section4 + 2)]
        internal static void CreateHDRenderPipelineGlobalSettings()
        {
            HDRenderPipelineGlobalSettingsCreator.Create(useProjectSettingsFolder: false, assignToActiveAsset: false);
        }
    }
}
