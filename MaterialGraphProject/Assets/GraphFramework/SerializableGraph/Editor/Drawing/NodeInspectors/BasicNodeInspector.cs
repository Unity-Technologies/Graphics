using System.Linq;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.Graphing.Drawing
{
    public class BasicNodeInspector : AbstractNodeInspector
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Label(node.name, EditorStyles.boldLabel);
            
            GUILayout.Space(10);

            var slots = node.GetInputSlots<MaterialSlot>().Where(x => x.showValue);
            if (!slots.Any())
                return;

            GUILayout.Label("Default Slot Values", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            foreach (var slot in node.GetInputSlots<MaterialSlot>().Where(x => x.showValue))
                slot.currentValue = EditorGUILayout.Vector4Field(slot.displayName, slot.currentValue);

            if (EditorGUI.EndChangeCheck())
                node.onModified(node, ModificationScope.Node);

            GUILayout.Space(10);
        }
    }
}
