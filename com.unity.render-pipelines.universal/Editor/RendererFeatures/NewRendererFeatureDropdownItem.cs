using UnityEditor.Rendering.Universal.Internal;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    internal static class NewRendererFeatureDropdownItem
    {
        static readonly string defaultNewClassName = "CustomRenderPassFeature.cs";

        [MenuItem("Assets/Create/Rendering/URP Renderer Feature", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
        internal static void CreateNewRendererFeature()
        {
            string templatePath = AssetDatabase.GUIDToAssetPath(ResourceGuid.rendererTemplate);
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, defaultNewClassName);
        }
    }
}
