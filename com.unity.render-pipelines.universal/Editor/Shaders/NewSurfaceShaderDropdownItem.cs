using UnityEditor.Rendering.Universal.Internal;

namespace UnityEditor.Rendering.Universal
{
    internal static class NewSurfaceShaderDropdownItem
    {
        static readonly string defaultNewClassName = "NewSurfaceShader.surfaceshader";

        [MenuItem("Assets/Create/Shader/Universal Render Pipeline/Surface Shader", priority = EditorUtils.lwrpAssetCreateMenuPriorityGroup2)]
        internal static void CreateNewSurfaceShader()
        {
            string templatePath = AssetDatabase.GUIDToAssetPath(ResourceGuid.surfaceShaderTemplate);
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, defaultNewClassName);
        }
    }
}
