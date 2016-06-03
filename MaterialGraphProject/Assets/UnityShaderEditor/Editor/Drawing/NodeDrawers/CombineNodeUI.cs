using UnityEditor.Graphing;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    [CustomNodeUI(typeof(CombineNode))]
    public class CombineNodeUI : AbstractMaterialNodeUI
    {   
        public float GetNodeUiHeight(float width)
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
                toReturn = GUIModificationType.Repaint;
            }

            area.y += 2 * EditorGUIUtility.singleLineHeight;
            area.height -= 2 * EditorGUIUtility.singleLineHeight;
            toReturn |= base.Render(area);
            return toReturn;
        }
    }
}
