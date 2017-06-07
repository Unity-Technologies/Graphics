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
                m_DebugPanel.GetDebugItem(DebugDisplaySettings.kEnableShadowDebug).handler.OnEditorGUI();

                DebugItem shadowDebug = m_DebugPanel.GetDebugItem(DebugDisplaySettings.kShadowDebugMode);
                shadowDebug.handler.OnEditorGUI();
                if ((ShadowMapDebugMode)shadowDebug.GetValue() == ShadowMapDebugMode.VisualizeShadowMap)
                {
                    EditorGUI.indentLevel++;
                    m_DebugPanel.GetDebugItem(DebugDisplaySettings.kShadowMapIndexDebug).handler.OnEditorGUI();
                    EditorGUI.indentLevel--;
                }
                DebugItem lightingDebugModeItem = m_DebugPanel.GetDebugItem(DebugDisplaySettings.kLightingDebugMode);
                lightingDebugModeItem.handler.OnEditorGUI();
                if ((DebugLightingMode)lightingDebugModeItem.GetValue() == DebugLightingMode.SpecularLighting)
                {
                    EditorGUI.indentLevel++;
                    DebugItem overrideSmoothnessItem = m_DebugPanel.GetDebugItem(DebugDisplaySettings.kOverrideSmoothnessDebug);
                    overrideSmoothnessItem.handler.OnEditorGUI();
                    if ((bool)overrideSmoothnessItem.GetValue())
                    {
                        m_DebugPanel.GetDebugItem(DebugDisplaySettings.kOverrideSmoothnessValueDebug).handler.OnEditorGUI();
                    }
                    EditorGUI.indentLevel--;
                }
                else if ((DebugLightingMode)lightingDebugModeItem.GetValue() == DebugLightingMode.DiffuseLighting)
                {
                    EditorGUI.indentLevel++;
                    m_DebugPanel.GetDebugItem(DebugDisplaySettings.kDebugLightingAlbedo).handler.OnEditorGUI();
                    EditorGUI.indentLevel--;
                }

                m_DebugPanel.GetDebugItem(DebugDisplaySettings.kFullScreenDebugMode).handler.OnEditorGUI();

                DebugItem displaySkyReflecItem = m_DebugPanel.GetDebugItem(DebugDisplaySettings.kDisplaySkyReflectionDebug);
                displaySkyReflecItem.handler.OnEditorGUI();
                if ((bool)displaySkyReflecItem.GetValue())
                {
                    EditorGUI.indentLevel++;
                    m_DebugPanel.GetDebugItem(DebugDisplaySettings.kSkyReflectionMipmapDebug).handler.OnEditorGUI();
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
