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
            var newAsset = UniversalGlobalSettings.Create(pathName, settings);
            if (updateGraphicsSettings)
                UniversalGlobalSettings.UpdateGraphicsSettings(newAsset);
            ProjectWindowUtil.ShowCreatedAsset(newAsset);
        }

        static UniversalGlobalSettings settings;
        static bool updateGraphicsSettings = false;
        public static void Clone(UniversalGlobalSettings src, bool activateAsset)
        {
            settings = src;
            updateGraphicsSettings = activateAsset;
            var path = "Assets/" + src.name + ".asset";

            var assetCreator = ScriptableObject.CreateInstance<UniversalGlobalSettingsCreator>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(assetCreator.GetInstanceID(), assetCreator, path, CoreEditorStyles.globalSettingsIcon, null);
        }

        public static void Create(bool activateAsset)
        {
            settings = null;
            updateGraphicsSettings = activateAsset;

            var path = "Assets/UniversalGlobalSettings.asset";
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<UniversalGlobalSettingsCreator>(), path, CoreEditorStyles.globalSettingsIcon, null);
        }

        [MenuItem("Assets/Create/Rendering/URP Global Settings Asset", priority = CoreUtils.Sections.section2 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 3)]
        internal static void CreateUniversalRenderPipelineGlobalSettings()
        {
            UniversalGlobalSettingsCreator.Create(activateAsset: false);
        }
    }
}
