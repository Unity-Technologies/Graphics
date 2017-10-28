using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Drawing
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
