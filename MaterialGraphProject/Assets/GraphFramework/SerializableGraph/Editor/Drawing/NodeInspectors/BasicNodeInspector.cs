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

            var scope = DoSlotsUI();

            if (scope == ModificationScope.Graph || scope == ModificationScope.Topological)
                node.owner.ValidateGraph();

            if (node.onModified != null)
                node.onModified(node, scope);
        }

        protected virtual ModificationScope DoSlotsUI()
        {
            var slots = node.GetSlots<MaterialSlot>().Where(x => x.showValue);
            if (!slots.Any())
                return ModificationScope.Nothing;

            EditorGUI.BeginChangeCheck();
            GUILayout.Label("Default Slot Values", EditorStyles.boldLabel);

            foreach (var slot in node.GetSlots<MaterialSlot>().Where(x => x.showValue))
                slot.currentValue = EditorGUILayout.Vector4Field(slot.displayName, slot.currentValue);

            GUILayout.Space(10);

            return EditorGUI.EndChangeCheck() ? ModificationScope.Node : ModificationScope.Nothing;
        }
    }
}
