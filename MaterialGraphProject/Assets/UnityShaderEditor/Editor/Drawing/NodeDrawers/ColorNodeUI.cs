using UnityEditor.Graphing;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    [CustomNodeUI(typeof(ColorNode))]
    public class ColorNodeUI : ICustomNodeUi
    {
        private ColorNode m_Node;
        
        public float GetNodeUiHeight(float width)
        {
            return 2 * EditorGUIUtility.singleLineHeight;
        }

        public GUIModificationType Render(Rect area)
        {
            if (m_Node == null)
                return GUIModificationType.None;

            EditorGUI.BeginChangeCheck();
            m_Node.color = EditorGUI.ColorField(new Rect(area.x, area.y, area.width, EditorGUIUtility.singleLineHeight), "Value", m_Node.color);
            if (EditorGUI.EndChangeCheck())
            {
                return GUIModificationType.Repaint;
            }
            return GUIModificationType.None;
        }

        public INode node
        {
            get { return m_Node; }
            set
            {
                var materialNode = value as ColorNode;
                if (materialNode != null)
                    m_Node = materialNode;
            }
        }

        public float GetNodeWidth()
        {
            return 200;
        }
    }
}
