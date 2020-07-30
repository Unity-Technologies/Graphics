using UnityEditor.Rendering.Universal.Internal;

namespace UnityEditor.Rendering.Universal
{
    internal static class NewRendererFeatureDropdownItem
    {
        static readonly string defaultNewClassName = "CustomRenderPassFeature.cs";
        static readonly string defaultNewScriptableRenderPassName = "CustomRenderPass.cs";

        [MenuItem("Assets/Create/Rendering/Universal Render Pipeline/Renderer Feature", priority = EditorUtils.lwrpAssetCreateMenuPriorityGroup2)]
        internal static void CreateNewRendererFeature()
        {
            string templatePath = AssetDatabase.GUIDToAssetPath(ResourceGuid.rendererTemplate);
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, defaultNewClassName);
        }

        [MenuItem("Assets/Create/Rendering/Universal Render Pipeline/Render Pass", priority = EditorUtils.lwrpAssetCreateMenuPriorityGroup2)]
        internal static void CreateNewScriptableRenderPass()
        {
            string templatePath = AssetDatabase.GUIDToAssetPath(ResourceGuid.renderPassTemplate);
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, defaultNewScriptableRenderPassName);
        }
    }
}
