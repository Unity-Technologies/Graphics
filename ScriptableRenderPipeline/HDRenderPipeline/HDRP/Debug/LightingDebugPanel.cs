using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class DebugItemHandlerShadowAtlasIndex
        : DebugItemHandlerUIntMinMax
    {
        public DebugItemHandlerShadowAtlasIndex(uint max)
            : base(0, max)
        {

        }

        public override void ValidateValues(Func<object> getter, Action<object> setter)
        {
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
            {
                m_Max = (uint)hdPipeline.GetShadowAtlasCount() - 1;
                setter(Math.Min(m_Max, Math.Max(m_Min, (uint)getter())));
            }
        }
    }

    public class DebugItemHandlerShadowIndex
    : DebugItemHandlerUIntMinMax
    {
        public DebugItemHandlerShadowIndex(uint max)
            : base(0, max)
        {

        }

        public override void ValidateValues(Func<object> getter, Action<object> setter)
        {
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
            {
                m_Max = (uint)hdPipeline.GetCurrentShadowCount() - 1;
                setter(Math.Min(m_Max, Math.Max(m_Min, (uint)getter())));
            }
        }
    }

    public class LightingDebugPanelUI
        : DebugPanelUI
    {
#if UNITY_EDITOR
        public override void OnEditorGUI()
        {
            using (new UnityEditor.EditorGUILayout.VerticalScope())
            {
                DebugItem shadowDebug = m_DebugPanel.GetDebugItem(DebugDisplaySettings.kShadowDebugMode);
                shadowDebug.handler.OnEditorGUI();
                if ((ShadowMapDebugMode)shadowDebug.GetValue() == ShadowMapDebugMode.VisualizeShadowMap)
                {
                    EditorGUI.indentLevel++;
                    DebugItem shadowSelectionDebug = m_DebugPanel.GetDebugItem(DebugDisplaySettings.kShadowSelectionDebug);
                    shadowSelectionDebug.handler.OnEditorGUI();
                    if(!(bool)shadowSelectionDebug.GetValue())
                        m_DebugPanel.GetDebugItem(DebugDisplaySettings.kShadowMapIndexDebug).handler.OnEditorGUI();
                    EditorGUI.indentLevel--;
                }
                if ((ShadowMapDebugMode)shadowDebug.GetValue() == ShadowMapDebugMode.VisualizeAtlas)
                {
                    EditorGUI.indentLevel++;
                    m_DebugPanel.GetDebugItem(DebugDisplaySettings.kShadowAtlasIndexDebug).handler.OnEditorGUI();
                    EditorGUI.indentLevel--;
                }
                DebugItem shadowMinValue = m_DebugPanel.GetDebugItem(DebugDisplaySettings.kShadowMinValueDebug);
                shadowMinValue.handler.OnEditorGUI();
                DebugItem shadowMaxValue = m_DebugPanel.GetDebugItem(DebugDisplaySettings.kShadowMaxValueDebug);
                shadowMaxValue.handler.OnEditorGUI();

                DebugItem lightingDebugModeItem = m_DebugPanel.GetDebugItem(DebugDisplaySettings.kLightingDebugMode);
                lightingDebugModeItem.handler.OnEditorGUI();
                switch ((DebugLightingMode)lightingDebugModeItem.GetValue())
                {
                    case DebugLightingMode.SpecularLighting:
                        {
                            EditorGUI.indentLevel++;
                            DebugItem overrideSmoothnessItem = m_DebugPanel.GetDebugItem(DebugDisplaySettings.kOverrideSmoothnessDebug);
                            overrideSmoothnessItem.handler.OnEditorGUI();
                            if ((bool)overrideSmoothnessItem.GetValue())
                            {
                                m_DebugPanel.GetDebugItem(DebugDisplaySettings.kOverrideSmoothnessValueDebug).handler.OnEditorGUI();
                            }
                            EditorGUI.indentLevel--;
                            break;
                        }
                    case DebugLightingMode.DiffuseLighting:
                        {
                            EditorGUI.indentLevel++;
                            m_DebugPanel.GetDebugItem(DebugDisplaySettings.kDebugLightingAlbedo).handler.OnEditorGUI();
                            EditorGUI.indentLevel--;
                            break;
                        }
                    case DebugLightingMode.EnvironmentProxyVolume:
                        {
                            ++EditorGUI.indentLevel;
                            m_DebugPanel.GetDebugItem(DebugDisplaySettings.kDebugEnvironmentProxyDepthScale).handler.OnEditorGUI();
                            --EditorGUI.indentLevel;
                            break;
                        }
                }

                var fullScreenDebugModeHandler = m_DebugPanel.GetDebugItem(DebugDisplaySettings.kFullScreenDebugMode);
                fullScreenDebugModeHandler.handler.OnEditorGUI();

                var fullScreenDebugMipHandler = m_DebugPanel.GetDebugItem(DebugDisplaySettings.kFullScreenDebugMip);
                var fullScreenDebugModeValue = (FullScreenDebugMode)fullScreenDebugModeHandler.GetValue();
                switch (fullScreenDebugModeValue)
                {
                    case FullScreenDebugMode.PreRefractionColorPyramid:
                    case FullScreenDebugMode.FinalColorPyramid:
                    case FullScreenDebugMode.DepthPyramid:
                    {
                        EditorGUI.indentLevel++;
                        fullScreenDebugMipHandler.handler.OnEditorGUI();
                        EditorGUI.indentLevel--;
                        break;
                        }
                    default:
                        fullScreenDebugMipHandler.SetValue(0f);
                        break;
                }

                DebugItem tileClusterDebug = m_DebugPanel.GetDebugItem(DebugDisplaySettings.kTileClusterDebug);
                tileClusterDebug.handler.OnEditorGUI();
                if ((int)tileClusterDebug.GetValue() != 0 && (int)tileClusterDebug.GetValue() != 3) // None and FeatureVariant
                {
                    EditorGUI.indentLevel++;
                    m_DebugPanel.GetDebugItem(DebugDisplaySettings.kTileClusterCategoryDebug).handler.OnEditorGUI();
                    EditorGUI.indentLevel--;
                }

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
