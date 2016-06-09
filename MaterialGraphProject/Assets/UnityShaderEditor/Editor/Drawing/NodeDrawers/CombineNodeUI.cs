using UnityEditor.Graphing;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    [CustomNodeUI(typeof(CombineNode))]
    public class CombineNodeUI : AbstractMaterialNodeUI
    {   
        public override float GetNodeUiHeight(float width)
        {
            return base.GetNodeUiHeight(width) + EditorGUIUtility.singleLineHeight;
        }

        public override GUIModificationType Render(Rect area)
        {
            var node = m_Node as CombineNode;
            if (node == null)
                return base.Render(area);

            if (m_Node == null)
                return GUIModificationType.None;

            EditorGUI.BeginChangeCheck();
            node.operation = (CombineNode.Operation)EditorGUI.EnumPopup(area, node.operation);

            var toReturn = GUIModificationType.None;
            if (EditorGUI.EndChangeCheck())
            {
                toReturn = GUIModificationType.DataChanged;
            }

            area.y += EditorGUIUtility.singleLineHeight;
            area.height -= EditorGUIUtility.singleLineHeight;
            toReturn |= base.Render(area);
            return toReturn;
        }
    }
}
