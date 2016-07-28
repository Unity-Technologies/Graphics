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
            var localNode = node as CombineNode;
            if (localNode == null)
                return base.Render(area);

            EditorGUI.BeginChangeCheck();
            localNode.operation = (CombineNode.Operation)EditorGUI.EnumPopup(area, localNode.operation);

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
