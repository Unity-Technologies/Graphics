using System;
using UnityEngine;

namespace UnityEditor.ShaderAnalysis.Internal
{
    class ShaderAnalysisMenu
    {
        [MenuItem("Window/Analysis/Shader Analysis Inspector", false, 11)]
        static void OpenPerformanceReportInspector()
        {
            var w = EditorWindow.GetWindow<ShaderAnalysisInspectorWindow>();
            w.titleContent = new GUIContent("Shader Analysis Inspector");
        }
    }
}
