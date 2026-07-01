using System;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using Object = UnityEngine.Object;

using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    class GradientSlotControlView : VisualElement
    {
        GradientInputMaterialSlot m_Slot;

        [SerializeField]
        GradientObject m_GradientObject;

        [SerializeField]
        SerializedObject m_SerializedObject;

        GradientField m_Field;
        public bool isShowingGradientEditor => GradientPicker.visible && m_Field.isShowingGradientPicker;

        public GradientSlotControlView(GradientInputMaterialSlot slot, bool showGradientEditor)
        {
            m_Slot = slot;
            if (!slot.hideConnector)
                styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/GradientSlotControlView"));
            else styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/GradientControlView"));

            m_GradientObject = ScriptableObject.CreateInstance<GradientObject>();
            m_GradientObject.gradient = new Gradient();
            m_SerializedObject = new SerializedObject(m_GradientObject);

            m_GradientObject.gradient.SetKeys(m_Slot.value.colorKeys, m_Slot.value.alphaKeys);
            m_GradientObject.gradient.mode = m_Slot.value.mode;

            m_Field = new GradientField() { label = m_Slot.hideConnector ? m_Slot.RawDisplayName() : null, value = m_GradientObject.gradient, colorSpace = ColorSpace.Linear, hdr = true };
            m_Field.RegisterValueChangedCallback(OnValueChanged);
            Add(m_Field);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            if (showGradientEditor)
                m_Field.ShowGradientPicker();
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnValueChanged(ChangeEvent<Gradient> evt)
        {
            m_SerializedObject.Update();
            if (!evt.newValue.Equals(m_Slot.value))
            {
                // Register undo on the temporary GradientObject rather than the graph so the open picker isn't torn down on undo.
                Undo.RegisterCompleteObjectUndo(m_GradientObject, "Modify Gradient Stop");

                m_GradientObject.gradient.SetKeys(evt.newValue.colorKeys, evt.newValue.alphaKeys);
                m_GradientObject.gradient.mode = evt.newValue.mode;
                m_SerializedObject.ApplyModifiedProperties();

                m_Slot.value = m_GradientObject.gradient;
                m_Slot.owner.Dirty(ModificationScope.Node);
                m_Slot.owner.owner.owner.isDirty = true;
            }
        }

        void OnUndoRedoPerformed()
        {
            if (m_GradientObject == null || m_Slot == null)
                return;

            m_SerializedObject.Update();
            m_Slot.value = m_GradientObject.gradient;
            m_Slot.owner?.Dirty(ModificationScope.Node);
            var graphObject = m_Slot.owner?.owner?.owner;
            if (graphObject != null)
                graphObject.isDirty = true;
            m_Field.SetValueWithoutNotify(m_GradientObject.gradient);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }
    }
}
