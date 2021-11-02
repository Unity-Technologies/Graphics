using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    static class CoreMenuItems
    {
        [MenuItem("Assets/Create/Shader/Custom Render Texture", priority = CoreUtils.Priorities.assetsCreateShaderMenuPriority + 2)]
        static void MenuCreateCustomRenderTextureShader()
        {
            string templatePath = $"{CoreUtils.GetCorePath()}/Editor/CustomRenderTexture/CustomRenderTextureShader.template";
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "New Custom Render Texture.shader");
        }
    }
}
