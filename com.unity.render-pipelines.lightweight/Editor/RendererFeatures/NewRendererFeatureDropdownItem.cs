namespace UnityEditor.Rendering.LWRP
{
    internal static class NewRendererFeatureDropdownItem
    {
        static readonly string defaultNewClassName = "CustomRenderPassFeature.cs";
        
        [MenuItem("Assets/Create/Rendering/Lightweight Render Pipeline/Renderer Feature", priority = EditorUtils.lwrpAssetCreateMenuPriorityGroup2)]
        internal static void CreateNewRendererFeature()
        {
            string templatePath = AssetDatabase.GUIDToAssetPath(ResourceGuid.rendererTemplate);
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, defaultNewClassName);
        }
    }
}
