using UnityEditor.Graphing;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    [CustomNodeUI(typeof(SubGraphNode))]
    public class SubgraphNodeUI : AbstractMaterialNodeUI
    {
        public override float GetNodeUiHeight(float width)
        {
            return base.GetNodeUiHeight(width) + EditorGUIUtility.singleLineHeight;
        }

        public override GUIModificationType Render(Rect area)
        {
            var localNode = node as SubGraphNode;
            if (localNode == null)
                return base.Render(area);

            EditorGUI.BeginChangeCheck();
            localNode.subGraphAsset = (MaterialSubGraphAsset)EditorGUI.ObjectField(new Rect(area.x, area.y, area.width, EditorGUIUtility.singleLineHeight),
                    new GUIContent("SubGraph"),
                    localNode.subGraphAsset,
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
