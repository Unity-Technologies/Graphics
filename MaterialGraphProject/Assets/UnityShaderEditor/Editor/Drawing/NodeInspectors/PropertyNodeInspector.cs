using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    public class PropertyNodeInspector : BasicNodeInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var propertyNode = node as PropertyNode;
            if (propertyNode == null)
                return;

            GUILayout.Label("Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            //propertyNode.name = EditorGUILayout.TextField ("Name", propertyNode.name);
            //propertyNode.description = EditorGUILayout.TextField("Description", propertyNode.description);

            if (EditorGUI.EndChangeCheck())
                node.onModified(node, ModificationScope.Node);
        }
    }
}
