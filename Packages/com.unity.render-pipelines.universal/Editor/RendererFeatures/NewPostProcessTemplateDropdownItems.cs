using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Universal
{
    internal static class NewPostProcessTemplateDropdownItems
    {
        const string k_FeatureTemplatePath =
            "Packages/com.unity.render-pipelines.universal/Editor/RendererFeatures/NewPostProcessRendererFeature.cs.txt";

        const string k_VolumeTemplatePath =
            "Packages/com.unity.render-pipelines.universal/Editor/RendererFeatures/NewPostProcessVolumeComponent.cs.txt";

        static string PreprocessScriptTemplate(string content, string featureType = null, string volumeType = null, string displayName = null)
        {
            if(featureType != null)
                content = content.Replace("#FEATURE_TYPE#", featureType);

            if(volumeType != null)
                content = content.Replace("#VOLUME_TYPE#", volumeType);

            if(displayName != null)
                content = content.Replace("#DISPLAY_NAME#", displayName);

            return content;
        }

        static Object CreateScriptAssetFromTemplate(string templatePath, string targetPath, string featureType = null, string volumeType = null, string displayName = null)
        {
            string content = File.ReadAllText(templatePath);
            return ProjectWindowUtil.CreateScriptAssetWithContent(targetPath, PreprocessScriptTemplate(content, featureType, volumeType, displayName));
        }

        internal class CreateCombinedScriptTemplateAssetsAction : ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string userPath, string resourceFile)
            {
                string directoryPath = Path.GetDirectoryName(userPath);
                string enteredName = Path.GetFileNameWithoutExtension(userPath);
                string cleanedEnteredNamed = enteredName.Replace(" ", "");

                string featureTypeName = cleanedEnteredNamed + "RendererFeature";
                string volumeTypeName = cleanedEnteredNamed + nameof(VolumeComponent);

                try
                {
                    AssetDatabase.StartAssetEditing();

                    Object o = CreateScriptAssetFromTemplate(k_FeatureTemplatePath,
                        Path.Combine(directoryPath, featureTypeName + ".cs"), featureTypeName, volumeTypeName, null);
                    CreateScriptAssetFromTemplate(k_VolumeTemplatePath,
                        Path.Combine(directoryPath, volumeTypeName + ".cs"), featureTypeName, volumeTypeName,
                        enteredName);
                    ProjectWindowUtil.ShowCreatedAsset(o);
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }

        [MenuItem("Assets/Create/Scripting/URP Post-process Volume Scripts", priority = UnityEngine.Rendering.CoreUtils.Priorities.scriptingPriority + 1)]
        static void MenuCreateCustomPostProcessVolumeRendererFeature()
        {
            Texture2D icon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
                ScriptableObject.CreateInstance<CreateCombinedScriptTemplateAssetsAction>(), "NewPostProcessEffect.cs", icon, k_FeatureTemplatePath);
        }
    }
}
