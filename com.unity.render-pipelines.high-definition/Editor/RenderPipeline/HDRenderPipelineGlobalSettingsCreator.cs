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
                HDRenderPipelineGlobalSettings.UpdateGraphicsSettings(newAsset);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }

            static HDRenderPipelineGlobalSettings settings;
            public static void Clone(HDRenderPipelineGlobalSettings src)
            {
                settings = src;
                var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
                var assetCreator = ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettingsCreator>();
                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(assetCreator.GetInstanceID(), assetCreator, $"Assets/{HDProjectSettings.projectSettingsFolderPath}/{src.name}.asset", icon, null);
            }
        }

        [MenuItem("Assets/Create/Rendering/HDRP Global Settings Asset", priority = CoreUtils.Sections.section1 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 1)]
        internal static void CreateHDRenderPipelineGlobalSettings()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettingsCreator>(), $"Assets/{HDProjectSettings.projectSettingsFolderPath}/HDRenderPipelineGlobalSettings.asset", icon, null);
        }
    }
}
