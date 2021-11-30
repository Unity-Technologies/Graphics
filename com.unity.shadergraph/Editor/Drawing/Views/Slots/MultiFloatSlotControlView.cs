using System;
using System.Globalization;
using UnityEditor.Graphing;
using UnityEngine;

using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    class MultiFloatSlotControlView : VisualElement
    {
        readonly AbstractMaterialNode m_Node;
        readonly Func<Vector4> m_Get;
        readonly Action<Vector4> m_Set;
        int m_UndoGroup = -1;

        public MultiFloatSlotControlView(AbstractMaterialNode node, string[] labels, Func<Vector4> get, Action<Vector4> set)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/MultiFloatSlotControlView"));
            m_Node = node;
            m_Get = get;
            m_Set = set;
            var initialValue = get();
            for (var i = 0; i < labels.Length; i++)
                AddField(initialValue, i, labels[i]);
        }

        void AddField(Vector4 initialValue, int index, string subLabel)
        {
            var dummy = new VisualElement { name = "dummy" };
            var label = new Label(subLabel);
            dummy.Add(label);
            Add(dummy);
            var field = new FloatField { userData = index, value = initialValue[index] };
            var dragger = new FieldMouseDragger<double>(field);
            dragger.SetDragZone(label);
            field.Q("unity-text-input").RegisterCallback<KeyDownEvent>(evt =>
            {
                // Record Undo for input field edit
                if (m_UndoGroup == -1)
                {
                    m_UndoGroup = Undo.GetCurrentGroup();
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                }
                // Handle scaping input field edit
                if (evt.keyCode == KeyCode.Escape && m_UndoGroup > -1)
                {
                    Undo.RevertAllDownToGroup(m_UndoGroup);
                    m_UndoGroup = -1;
                    evt.StopPropagation();
                }
                // Dont record Undo again until input field is unfocused
                m_UndoGroup++;
                this.MarkDirtyRepaint();
            });
            // Called after KeyDownEvent
            field.RegisterValueChangedCallback(evt =>
            {
                // Only true when setting value via FieldMouseDragger
                // Undo recorded once per dragger release
                if (m_UndoGroup == -1)
                {
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                }
                var value = m_Get();
                if (value[index] != (float)evt.newValue)
                {
                    value[index] = (float)evt.newValue;
                    m_Set(value);
                    m_Node.Dirty(ModificationScope.Node);
                }
            });
            // Reset UndoGroup when done editing input field & update title
            field.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
            {
                m_Node.owner.owner.isDirty = true;
                m_UndoGroup = -1;
            });
            Add(field);
        }
    }
}
