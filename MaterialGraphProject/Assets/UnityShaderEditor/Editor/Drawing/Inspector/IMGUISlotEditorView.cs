using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class IMGUISlotEditorView : VisualElement
    {
        MaterialSlot m_Slot;
        int m_SlotsHash;

        public IMGUISlotEditorView(MaterialSlot slot)
        {
            m_Slot = slot;
            Add(new IMGUIContainer(OnGUIHandler));
        }

        void OnGUIHandler()
        {
            if (m_Slot == null)
                return;
            var previousWideMode = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            var newValue = SlotField(m_Slot);
            if (newValue != m_Slot.currentValue)
            {
                m_Slot.currentValue = newValue;
                m_Slot.owner.onModified(m_Slot.owner, ModificationScope.Node);
            }
            EditorGUIUtility.wideMode = previousWideMode;
        }

        static Vector4 SlotField(MaterialSlot slot)
        {
            if (slot.concreteValueType == ConcreteSlotValueType.Vector1)
                return new Vector4(EditorGUILayout.FloatField(slot.displayName, slot.currentValue.x), 0, 0, 0);
            if (slot.concreteValueType == ConcreteSlotValueType.Vector2)
                return EditorGUILayout.Vector2Field(slot.displayName, slot.currentValue);
            if (slot.concreteValueType == ConcreteSlotValueType.Vector3)
                return EditorGUILayout.Vector3Field(slot.displayName, slot.currentValue);
            if (slot.concreteValueType == ConcreteSlotValueType.Vector4)
                return EditorGUILayout.Vector4Field(slot.displayName, slot.currentValue);
            return Vector4.zero;
        }
    }
}
