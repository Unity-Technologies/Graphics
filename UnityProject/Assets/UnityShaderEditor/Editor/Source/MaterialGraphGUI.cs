using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

namespace UnityEditor.MaterialGraph
{
    class MaterialGraphGUI : BaseMaterialGraphGUI
    {
        enum SelectedOptions
        {
            properties,
            options
        }

        private PixelGraph GetGraph() {return graph as PixelGraph; }
        public MaterialGraph materialGraph;

        private SelectedOptions m_SelectedGUI = SelectedOptions.properties;

        private static void DrawSpacer()
        {
            var spacerLine = GUILayoutUtility.GetRect(GUIContent.none,
                    GUIStyle.none,
                    GUILayout.ExpandWidth(true),
                    GUILayout.Height(1));
            var oldBgColor = GUI.backgroundColor;
            if (EditorGUIUtility.isProSkin)
                GUI.backgroundColor = oldBgColor * 0.7058f;
            else
                GUI.backgroundColor = Color.black;

            //if (Event.current.type == EventType.Repaint)
            //  EditorGUIUtility.whiteTextureStyle.Draw(spacerLine, GUIContent.none, false, false, false, false);

            GUI.backgroundColor = oldBgColor;
        }

        public void RenderOptions(Rect rect, MaterialGraph graph)
        {
            GUILayout.BeginArea(rect);

            m_SelectedGUI = (SelectedOptions)EditorGUILayout.EnumPopup("Options?", m_SelectedGUI);

            DrawSpacer();

            if (m_SelectedGUI == SelectedOptions.properties)
                graph.materialProperties.DoGUI(GetGraph().nodes);
            else if (m_SelectedGUI == SelectedOptions.options)
                graph.materialOptions.DoGUI();

            GUILayout.EndArea();
        }
    }
}
