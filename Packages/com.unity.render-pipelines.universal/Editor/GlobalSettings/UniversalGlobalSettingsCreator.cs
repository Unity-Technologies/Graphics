using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;
using System.Reflection;
using System;

namespace UnityEngine.Rendering.Universal
{
    internal class UniversalGlobalSettingsCreator : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var newAsset = UniversalRenderPipelineGlobalSettings.Create(pathName, settings);
            if (updateGraphicsSettings)
                UniversalRenderPipelineGlobalSettings.UpdateGraphicsSettings(newAsset);
            ProjectWindowUtil.ShowCreatedAsset(newAsset);
        }

        private static string GetCurrentOpenedPath()
        {
            Type projectWindowUtilType = typeof(ProjectWindowUtil);
            MethodInfo getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            object obj = getActiveFolderPath.Invoke(null, new object[0]);
            return obj.ToString();
        }

        static UniversalRenderPipelineGlobalSettings settings;
        static bool updateGraphicsSettings = false;
        public static void Clone(UniversalRenderPipelineGlobalSettings src, bool activateAsset)
        {
            settings = src;
            updateGraphicsSettings = activateAsset;

            var path = GetCurrentOpenedPath() + $"/{src.name}.asset";
            CoreUtils.EnsureFolderTreeInAssetFilePath(path);

            var assetCreator = ScriptableObject.CreateInstance<UniversalGlobalSettingsCreator>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(assetCreator.GetInstanceID(), assetCreator, path, CoreEditorStyles.globalSettingsIcon, null);
        }

        public static void Create(bool useProjectSettingsFolder, bool activateAsset)
        {
            settings = null;
            updateGraphicsSettings = activateAsset;

            var path = useProjectSettingsFolder ?
                $"Assets/{UniversalRenderPipelineGlobalSettings.defaultAssetName}.asset" :
                GetCurrentOpenedPath() + $"/{UniversalRenderPipelineGlobalSettings.defaultAssetName}.asset";

            CoreUtils.EnsureFolderTreeInAssetFilePath(path);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<UniversalGlobalSettingsCreator>(), path, CoreEditorStyles.globalSettingsIcon, null);
        }

        [MenuItem("Assets/Create/Rendering/URP Global Settings Asset", priority = CoreUtils.Sections.section4 + 1)]
        internal static void CreateUniversalRenderPipelineGlobalSettings()
        {
            UniversalGlobalSettingsCreator.Create(useProjectSettingsFolder: false, activateAsset: false);
        }
    }
}
