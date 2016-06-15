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
            return EditorGUIUtility.singleLineHeight;
        }

        public GUIModificationType Render(Rect area)
        {
            return GUIModificationType.None;
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
