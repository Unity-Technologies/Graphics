using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    class GradientControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;

        public GradientControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new GradientControlView(m_Label, node, propertyInfo);
        }
    }

    [Serializable]
    class GradientObject : ScriptableObject
    {
        public Gradient gradient = new Gradient();
    }

    class GradientUndoSession
    {
        readonly Func<Gradient> m_ReadValue;
        readonly Action<Gradient> m_WriteValue;
        readonly Action m_RegisterGraphUndo;
        readonly Func<bool> m_CanRecord;

        Gradient m_PreSessionValue;

        public GradientUndoSession(Func<Gradient> readValue, Action<Gradient> writeValue, Action registerGraphUndo,
            Func<bool> canRecord)
        {
            m_ReadValue = readValue;
            m_WriteValue = writeValue;
            m_RegisterGraphUndo = registerGraphUndo;
            m_CanRecord = canRecord;
        }

        public void BeginChange()
        {
            if (m_PreSessionValue == null)
                m_PreSessionValue = Copy(m_ReadValue());
        }

        public void RecordNetChange()
        {
            var preSessionValue = m_PreSessionValue;
            m_PreSessionValue = null;
            if (preSessionValue == null || !m_CanRecord())
                return;

            var finalValue = Copy(m_ReadValue());
            if (!GradientUtil.CheckEquivalency(preSessionValue, finalValue))
            {
                m_WriteValue(preSessionValue);
                m_RegisterGraphUndo();
                m_WriteValue(finalValue);
            }
        }

        static Gradient Copy(Gradient other)
        {
            var copy = new Gradient();
            copy.SetKeys(other.colorKeys, other.alphaKeys);
            copy.mode = other.mode;
            return copy;
        }
    }

    class GradientControlView : VisualElement
    {
        GUIContent m_Label;

        AbstractMaterialNode m_Node;

        PropertyInfo m_PropertyInfo;

        [SerializeField]
        GradientObject m_GradientObject;

        [SerializeField]
        SerializedObject m_SerializedObject;

        GradientField m_Field;

        GradientUndoSession m_UndoSession;

        public GradientControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/GradientControlView"));

            if (propertyInfo.PropertyType != typeof(Gradient))
                throw new ArgumentException("Property must be of type Gradient.", "propertyInfo");
            new GUIContent(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name));

            m_GradientObject = ScriptableObject.CreateInstance<GradientObject>();
            m_GradientObject.gradient = new Gradient();
            m_SerializedObject = new SerializedObject(m_GradientObject);

            var gradient = (Gradient)m_PropertyInfo.GetValue(m_Node, null);
            m_GradientObject.gradient.SetKeys(gradient.colorKeys, gradient.alphaKeys);
            m_GradientObject.gradient.mode = gradient.mode;

            var gradientPanel = new VisualElement { name = "gradientPanel" };
            if (!string.IsNullOrEmpty(label))
                gradientPanel.Add(new Label(label));

            m_Field = new GradientField() { value = m_GradientObject.gradient, colorSpace = ColorSpace.Linear, hdr = true };
            m_Field.RegisterValueChangedCallback(OnValueChanged);
            m_Field.pickerClosed += OnPickerClosed;
            gradientPanel.Add(m_Field);

            m_UndoSession = new GradientUndoSession(
                () => (Gradient)m_PropertyInfo.GetValue(m_Node, null),
                value => m_PropertyInfo.SetValue(m_Node, value, null),
                () => m_Node.owner.owner.RegisterCompleteObjectUndo("Modify Gradient"),
                () => m_Node?.owner?.owner != null && !m_Node.owner.replaceInProgress);

            Add(gradientPanel);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnValueChanged(ChangeEvent<Gradient> evt)
        {
            m_SerializedObject.Update();
            var value = (Gradient)m_PropertyInfo.GetValue(m_Node, null);
            if (!evt.newValue.Equals(value))
            {
                m_UndoSession.BeginChange();
                Undo.RegisterCompleteObjectUndo(m_GradientObject, "Modify Gradient Stop");

                m_GradientObject.gradient.SetKeys(evt.newValue.colorKeys, evt.newValue.alphaKeys);
                m_GradientObject.gradient.mode = evt.newValue.mode;
                m_SerializedObject.ApplyModifiedProperties();

                m_PropertyInfo.SetValue(m_Node, m_GradientObject.gradient, null);
                m_Node.owner.owner.isDirty = true;
                this.MarkDirtyRepaint();
            }
        }

        void OnUndoRedoPerformed()
        {
            if (m_GradientObject == null || m_Node == null)
                return;

            m_SerializedObject.Update();
            m_PropertyInfo.SetValue(m_Node, m_GradientObject.gradient, null);
            var graphObject = m_Node.owner?.owner;
            if (graphObject != null)
                graphObject.isDirty = true;
            m_Field.SetValueWithoutNotify(m_GradientObject.gradient);
            this.MarkDirtyRepaint();
        }

        void OnPickerClosed()
        {
            m_UndoSession.RecordNetChange();
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_UndoSession.RecordNetChange();
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            m_Field.pickerClosed -= OnPickerClosed;
        }
    }
}
