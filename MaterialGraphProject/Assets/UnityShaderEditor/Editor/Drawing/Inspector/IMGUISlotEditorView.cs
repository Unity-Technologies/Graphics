using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
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
            var modified = SlotField(m_Slot);
            EditorGUIUtility.wideMode = previousWideMode;

            if (modified)
                m_Slot.owner.onModified(m_Slot.owner, ModificationScope.Node);
        }

        public static bool SlotField(MaterialSlot slot)
        {
            EditorGUI.BeginChangeCheck();
            if (slot is DynamicVectorMaterialSlot)
            {
                var dynSlot = slot as DynamicVectorMaterialSlot;
                dynSlot.value = EditorGUILayout.Vector4Field(slot.displayName, dynSlot.value);
            }

            if (slot is Vector1MaterialSlot)
            {
                var dynSlot = slot as Vector1MaterialSlot;
                dynSlot.value = EditorGUILayout.FloatField(slot.displayName, dynSlot.value);
            }

            if (slot is Vector2MaterialSlot)
            {
                var dynSlot = slot as Vector2MaterialSlot;
                dynSlot.value = EditorGUILayout.Vector2Field(slot.displayName, dynSlot.value);
            }

            if (slot is Vector3MaterialSlot)
            {
                var dynSlot = slot as Vector3MaterialSlot;
                dynSlot.value = EditorGUILayout.Vector3Field(slot.displayName, dynSlot.value);
            }

            if (slot is Vector4MaterialSlot)
            {
                var dynSlot = slot as Vector4MaterialSlot;
                dynSlot.value = EditorGUILayout.Vector4Field(slot.displayName, dynSlot.value);
            }

            if (slot is Texture2DInputMaterialSlot)
            {
                var dynslot = slot as Texture2DInputMaterialSlot;
                dynslot.texture = EditorGUILayout.MiniThumbnailObjectField(new GUIContent("Texture"), dynslot.texture, typeof(Texture), null) as Texture;
            }
            return EditorGUI.EndChangeCheck();
        }
    }
}
