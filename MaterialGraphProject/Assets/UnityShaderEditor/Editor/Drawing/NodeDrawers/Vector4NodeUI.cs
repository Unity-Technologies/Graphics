using UnityEditor.Graphing;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    [CustomNodeUI(typeof(Vector4Node))]
    public class Vector4NodeUI : ICustomNodeUi
    {
        private Vector4Node m_Node;
        
        public float GetNodeUiHeight(float width)
        {
            return 2 * EditorGUIUtility.singleLineHeight;
        }

        public GUIModificationType Render(Rect area)
        {
            if (m_Node == null)
                return GUIModificationType.None;

            EditorGUI.BeginChangeCheck();
            m_Node.value = EditorGUI.Vector4Field(new Rect(area.x, area.y, area.width, EditorGUIUtility.singleLineHeight), "Value", m_Node.value);
            if (EditorGUI.EndChangeCheck())
            {
                //TODO:tidy this shit.
                //EditorUtility.SetDirty(materialGraphOwner.owner);
                return GUIModificationType.Repaint;
            }
            return GUIModificationType.None;
        }

        public void SetNode(INode node)
        {
            if (node is Vector4Node)
                m_Node = (Vector4Node) node;
        }
    }
}
