using UnityEditor.Graphing;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    [CustomNodeUI(typeof(AbstractSubGraphIONode))]
    public class SubGraphIONodeUI : ICustomNodeUi
    {
        private AbstractSubGraphIONode m_Node;
        
        public float GetNodeUiHeight(float width)
        {
            return 2 * EditorGUIUtility.singleLineHeight;
        }

        public GUIModificationType Render(Rect area)
        {
            if (m_Node == null)
                return GUIModificationType.None;

            var modification = GUIModificationType.None;
            if (GUI.Button(new Rect(area.x, area.y, area.width, EditorGUIUtility.singleLineHeight), "Add Slot"))
            {
                m_Node.AddSlot();
                modification |= GUIModificationType.ModelChanged;
            }

            if (GUI.Button(new Rect(area.x, area.y + EditorGUIUtility.singleLineHeight, area.width, EditorGUIUtility.singleLineHeight), "Remove Slot"))
            {
                m_Node.RemoveSlot();
                modification |= GUIModificationType.ModelChanged;
            }
            return modification;
        }

        public void SetNode(INode node)
        {
            if (node is AbstractSubGraphIONode)
                m_Node = (AbstractSubGraphIONode) node;
        }

        public float GetNodeWidth()
        {
            return 100;
        }
    }
}
