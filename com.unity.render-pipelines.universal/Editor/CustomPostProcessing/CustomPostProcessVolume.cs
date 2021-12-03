using UnityEditor;

namespace UnityEditor.Rendering.Universal
{
    public class CustomPostProcessVolume
    {
        [MenuItem("Assets/Create/Custom Post Processing/Custom Post Process Volume")]
        static void MenuCreateCustomPostProcessVolume()
        {
            string templatePath = $"/Users/tomaszi/dev/srp/com.unity.render-pipelines.universal/Editor/CustomPostProcessing/CustomPostProcessingVolume.template";
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "New Post Process Volume.cs");
        }
    }
}
