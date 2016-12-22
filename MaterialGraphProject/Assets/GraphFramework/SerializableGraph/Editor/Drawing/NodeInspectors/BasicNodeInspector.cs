using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class BasicNodeInspector : AbstractNodeInspector
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Label(node.name, EditorStyles.boldLabel);
        }
    }
}