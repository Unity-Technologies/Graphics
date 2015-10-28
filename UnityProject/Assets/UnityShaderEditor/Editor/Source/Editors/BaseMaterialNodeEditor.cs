using System;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [CustomEditor(typeof(BaseMaterialNode), true)]
    class BaseMaterialNodeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var node = target as BaseMaterialNode;
            if (node == null)
                return;

            var slots = node.slots.ToArray();

            EditorGUILayout.LabelField("Preview Mode: " + node.previewMode);

            foreach (var slot in slots)
                DoSlotUI(node, slot);
        }

        /*private bool DoDefaultInspectorUI ()
        {
            EditorGUIUtility.LookLikeInspector();

            EditorGUI.BeginChangeCheck ();
            serializedObject.Update();

            bool materialSlotsFound = serializedObject.FindProperty("m_SlotDefaultValues") != null;
            SerializedProperty property = serializedObject.GetIterator ();
            bool expanded = true;

            while (property.NextVisible (expanded))
            {
                expanded = false;
                if (materialSlotsFound && (property.type == "Slot" || property.name == "m_SlotPropertiesIndexes" || property.name == "m_SlotProperties"))
                    continue;
                EditorGUILayout.PropertyField (property, true);
            }

            serializedObject.ApplyModifiedProperties ();
            EditorGUI.EndChangeCheck ();

            return materialSlotsFound;
        }
        */
        private void DoSlotUI(BaseMaterialNode node, Slot slot)
        {
            GUILayout.BeginHorizontal(/*EditorStyles.inspectorBig*/);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Slot " + slot.title, EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            DoMaterialSlotUIBody(node, slot);
        }

        private static void DoMaterialSlotUIBody(BaseMaterialNode node, Slot slot)
        {
            SlotDefaultValue defaultValue = node.GetSlotDefaultValue(slot.name);
            if (defaultValue == null)
                return;
            
            var def = node.GetSlotDefaultValue(slot.name);
            if (def != null && def.OnGUI())
            {
                node.UpdatePreviewProperties();
                node.ForwardPreviewMaterialPropertyUpdate();
            }
        }

        private static void RemoveSlot(BaseMaterialNode node, Slot slot)
        {
            node.RemoveSlot(slot);
        }
    }
}
