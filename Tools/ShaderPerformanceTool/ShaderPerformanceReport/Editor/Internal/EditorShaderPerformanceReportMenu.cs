using UnityEngine;

namespace UnityEditor.Experimental.ShaderTools.Internal
{ 
    class EditorShaderPerformanceReportMenu
    {
        [MenuItem("Internal/Shader Tools/Performance Report Inspector")]
        static void OpenPerformanceReportInspector()
        {
            var w = EditorWindow.GetWindow<EditorShaderPerformanceReportWindow>();
            w.titleContent = new GUIContent("Shader Perf Inspector");
        }
    }
}
