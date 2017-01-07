using System.Linq;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.Graphing.Drawing
{
    public class SubgraphOutputNodeInspector : BasicNodeInspector
    {
        protected enum UISlotValueType
        {
            Vector4,
            Vector3,
            Vector2,
            Vector1
        }

        private static UISlotValueType ToUISlot(SlotValueType slot)
        {
            switch (slot)
            {
                case SlotValueType.Vector1:
                    return UISlotValueType.Vector1;
                case SlotValueType.Vector2:
                    return UISlotValueType.Vector2;
                case SlotValueType.Vector3:
                    return UISlotValueType.Vector3;
                case SlotValueType.Vector4:
                    return UISlotValueType.Vector4;
            }
            return UISlotValueType.Vector4;
        }

        private static SlotValueType ToSlotValueType(UISlotValueType slot)
        {
            switch (slot)
            {
                case UISlotValueType.Vector1:
                    return SlotValueType.Vector1;
                case UISlotValueType.Vector2:
                    return SlotValueType.Vector2;
                case UISlotValueType.Vector3:
                    return SlotValueType.Vector3;
                case UISlotValueType.Vector4:
                    return SlotValueType.Vector4;
            }
            return SlotValueType.Vector4;
        }

        protected override ModificationScope DoSlotsUI()
        {
            var slots = node.GetSlots<MaterialSlot>().Where(x => x.showValue);
            if (!slots.Any())
                return ModificationScope.Node;

            GUILayout.Label("Default Slot Values", EditorStyles.boldLabel);

            bool typeChanged = false;
            foreach (var slot in node.GetSlots<MaterialSlot>().Where(x => x.showValue))
            {
                EditorGUI.BeginChangeCheck();
                var result = (UISlotValueType)EditorGUILayout.EnumPopup(ToUISlot(slot.valueType));
                slot.valueType = ToSlotValueType(result);
                if (EditorGUI.EndChangeCheck())
                    typeChanged |= true;
            }

            GUILayout.Space(10);

            if (typeChanged)
                return ModificationScope.Topological;

            return ModificationScope.Node;
        }
    }
}
