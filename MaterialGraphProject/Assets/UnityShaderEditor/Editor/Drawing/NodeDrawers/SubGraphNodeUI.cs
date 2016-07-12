using UnityEditor.Graphing;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    [CustomNodeUI(typeof(SubGraphNode))]
    public class SubgraphNodeUI : ICustomNodeUi
    {
        private SubGraphNode m_Node;
        
        public float GetNodeUiHeight(float width)
        {
            return 1 * EditorGUIUtility.singleLineHeight;
        }

        public GUIModificationType Render(Rect area)
        {
            if (m_Node == null)
                return GUIModificationType.None;

            EditorGUI.BeginChangeCheck();
            m_Node.subGraphAsset = (MaterialSubGraphAsset) EditorGUI.ObjectField(new Rect(area.x, area.y, area.width, EditorGUIUtility.singleLineHeight),
                new GUIContent("SubGraph"),
                m_Node.subGraphAsset,
                typeof(MaterialSubGraphAsset), false);

            if (EditorGUI.EndChangeCheck())
            {
                m_Node.UpdateNodeAfterDeserialization();
                return GUIModificationType.ModelChanged;
            }

            return GUIModificationType.None;
        }

        public void SetNode(INode node)
        {
            if (node is SubGraphNode)
                m_Node = (SubGraphNode) node;
        }

        public float GetNodeWidth()
        {
            return 200;
        }
    }
}
