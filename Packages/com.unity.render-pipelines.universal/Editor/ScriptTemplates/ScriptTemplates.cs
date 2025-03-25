namespace UnityEditor.Rendering.Universal
{
    internal static class ScriptTemplates
    {
        internal const string ScriptTemplatePath = "Packages/com.unity.render-pipelines.universal/Editor/ScriptTemplates/";

        [MenuItem("Assets/Create/Shader/URP Unlit Shader", priority = 0)]
        static void CreateUnlitURPShader()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile($"{ScriptTemplatePath}UnlitURP.txt", "NewUnlitUniversalRenderPipelineShader.shader");
        }

        [MenuItem("Assets/Create/Scripting/URP Renderer Feature Script", priority = UnityEngine.Rendering.CoreUtils.Priorities.scriptingPriority)]
        internal static void CreateNewRendererFeature()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile($"{ScriptTemplatePath}ScriptableRendererFeature.txt", "NewURPRenderFeature.cs");
        }
    }
}
