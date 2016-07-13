using UnityEditor.Graphing;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    [CustomNodeUI(typeof(SubGraphNode))]
    public class SubgraphNodeUI : AbstractMaterialNodeUI
    {
        public float GetNodeUiHeight(float width)
        {
            return base.GetNodeUiHeight(width) + 2 * EditorGUIUtility.singleLineHeight;
        }

        public override GUIModificationType Render(Rect area)
        {
            var node = m_Node as SubGraphNode;
            if (node == null)
                return base.Render(area);

            EditorGUI.BeginChangeCheck();
            node.subGraphAsset = (MaterialSubGraphAsset) EditorGUI.ObjectField(new Rect(area.x, area.y, area.width, EditorGUIUtility.singleLineHeight),
                new GUIContent("SubGraph"),
                node.subGraphAsset,
                typeof(MaterialSubGraphAsset), false);

            var toReturn = GUIModificationType.None;

            if (EditorGUI.EndChangeCheck())
                toReturn |= GUIModificationType.ModelChanged;

            area.y += EditorGUIUtility.singleLineHeight;
            area.height -= EditorGUIUtility.singleLineHeight;
            toReturn |= base.Render(area);
            return toReturn;
        }
    }
}
