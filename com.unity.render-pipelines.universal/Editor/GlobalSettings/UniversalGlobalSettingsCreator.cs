using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;

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

        static UniversalRenderPipelineGlobalSettings settings;
        static bool updateGraphicsSettings = false;
        public static void Clone(UniversalRenderPipelineGlobalSettings src, bool activateAsset)
        {
            settings = src;
            updateGraphicsSettings = activateAsset;
            var path = AssetDatabase.GetAssetPath(src);

            var assetCreator = ScriptableObject.CreateInstance<UniversalGlobalSettingsCreator>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(assetCreator.GetInstanceID(), assetCreator, path, CoreEditorStyles.globalSettingsIcon, null);
        }

        public static void Create(bool useProjectSettingsFolder, bool activateAsset)
        {
            settings = null;
            updateGraphicsSettings = activateAsset;

            var path = $"{UniversalRenderPipelineGlobalSettings.defaultAssetName}.asset";
            if (useProjectSettingsFolder)
            {
                path = $"Assets/{path}";
                CoreUtils.EnsureFolderTreeInAssetFilePath(path);
            }
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<UniversalGlobalSettingsCreator>(), path, CoreEditorStyles.globalSettingsIcon, null);
        }

        [MenuItem("Assets/Create/Rendering/URP Global Settings Asset", priority = CoreUtils.Sections.section4 + 1)]
        internal static void CreateUniversalRenderPipelineGlobalSettings()
        {
            UniversalGlobalSettingsCreator.Create(useProjectSettingsFolder: false, activateAsset: false);
        }
    }
}
