namespace UnityEditor.Rendering
{
    internal static class ScriptTemplates
    {
        internal const string ScriptTemplatePath = "Packages/com.unity.render-pipelines.core/Editor/ScriptTemplates/";

        [MenuItem("Assets/Create/Shader/SRP Blit Shader", priority = 1)]
        static void CreateBlitSRPShader()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile($"{ScriptTemplatePath}BlitSRP.txt", "NewBlitScriptableRenderPipelineShader.shader");
        }
    }
}
