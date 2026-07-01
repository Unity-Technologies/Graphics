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
            gradientPanel.Add(m_Field);

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
                // Register undo on the temporary GradientObject rather than the graph so the open picker isn't torn down on undo.
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

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }
    }
}
