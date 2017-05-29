using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class LightingDebugPanelUI
        : DebugPanelUI
    {
#if UNITY_EDITOR
        public override void OnEditorGUI()
        {
            using (new UnityEditor.EditorGUILayout.VerticalScope())
            {
                m_DebugPanel.GetDebugItem("Enable Shadows").handler.OnEditorGUI();

                DebugItem shadowDebug = m_DebugPanel.GetDebugItem("Shadow Debug Mode");
                shadowDebug.handler.OnEditorGUI();
                if ((ShadowMapDebugMode)shadowDebug.GetValue() == ShadowMapDebugMode.VisualizeShadowMap)
                {
                    EditorGUI.indentLevel++;
                    m_DebugPanel.GetDebugItem("Shadow Map Index").handler.OnEditorGUI();
                    EditorGUI.indentLevel--;
                }
                DebugItem lightingDebugModeItem = m_DebugPanel.GetDebugItem("Lighting Debug Mode");
                lightingDebugModeItem.handler.OnEditorGUI();
                if ((DebugLightingMode)lightingDebugModeItem.GetValue() == DebugLightingMode.SpecularLighting)
                {
                    EditorGUI.indentLevel++;
                    DebugItem overrideSmoothnessItem = m_DebugPanel.GetDebugItem("Override Smoothness");
                    overrideSmoothnessItem.handler.OnEditorGUI();
                    if ((bool)overrideSmoothnessItem.GetValue())
                    {
                        m_DebugPanel.GetDebugItem("Override Smoothness Value").handler.OnEditorGUI();
                    }
                    EditorGUI.indentLevel--;
                }
                else if ((DebugLightingMode)lightingDebugModeItem.GetValue() == DebugLightingMode.DiffuseLighting)
                {
                    EditorGUI.indentLevel++;
                    m_DebugPanel.GetDebugItem("Debug Lighting Albedo").handler.OnEditorGUI();
                    EditorGUI.indentLevel--;
                }

                DebugItem displaySkyReflecItem = m_DebugPanel.GetDebugItem("Display Sky Reflection");
                displaySkyReflecItem.handler.OnEditorGUI();
                if ((bool)displaySkyReflecItem.GetValue())
                {
                    EditorGUI.indentLevel++;
                    m_DebugPanel.GetDebugItem("Sky Reflection Mipmap").handler.OnEditorGUI();
                    EditorGUI.indentLevel--;
                }
            }
        }
#endif
    }

    public class LightingDebugPanel
        : DebugPanel<LightingDebugPanelUI>
    {
        public LightingDebugPanel()
            : base("Lighting")
        {
        }
    }
}
