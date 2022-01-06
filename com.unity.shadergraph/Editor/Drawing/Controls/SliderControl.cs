using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using System.Globalization;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    class SliderControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        bool m_DisplayMinMax;

        public SliderControlAttribute(string label = null, bool displayMinMax = false)
        {
            m_Label = label;
            m_DisplayMinMax = displayMinMax;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new SliderControlView(m_Label, m_DisplayMinMax, node, propertyInfo);
        }
    }

    class SliderControlView : VisualElement, AbstractMaterialNodeModificationListener
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        bool m_DisplayMinMax;
        Vector3 m_Value;

        VisualElement m_SliderPanel;
        Slider m_Slider;
        FloatField m_SliderInput;
        FloatField m_MinField;
        FloatField m_MaxField;

        public SliderControlView(string label, bool displayMinMax, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/SliderControlView"));
            m_DisplayMinMax = displayMinMax;

            if (propertyInfo.PropertyType != typeof(Vector3))
                throw new ArgumentException("Property must be of type Vector3.", "propertyInfo");
            new GUIContent(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name));
            m_Value = (Vector3)m_PropertyInfo.GetValue(m_Node, null);

            m_Slider = new Slider(m_Value.y, m_Value.z) { value = m_Value.x };
            m_Slider.RegisterValueChangedCallback((evt) => OnChangeSlider(evt.newValue));

            m_SliderInput = new FloatField { value = m_Value.x };
            m_SliderInput.RegisterValueChangedCallback(evt =>
            {
                var value = (float)evt.newValue;
                m_Value.x = value;
                m_PropertyInfo.SetValue(m_Node, m_Value, null);
                this.MarkDirtyRepaint();
            });
            m_SliderInput.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
            {
                float minValue = Mathf.Min(m_Value.x, m_Value.y);
                float maxValue = Mathf.Max(m_Value.x, m_Value.z);
                m_Value = new Vector3(m_Value.x, minValue, maxValue);
                m_MinField.value = minValue;
                m_MaxField.value = maxValue;
                UpdateSlider();
                m_PropertyInfo.SetValue(m_Node, m_Value, null);
                this.MarkDirtyRepaint();
            }, TrickleDown.TrickleDown);

            m_SliderPanel = new VisualElement { name = "SliderPanel" };
            if (!string.IsNullOrEmpty(label))
                m_SliderPanel.Add(new Label(label));

            m_SliderPanel.Add(m_Slider);
            m_SliderPanel.Add(m_SliderInput);
            Add(m_SliderPanel);

            if (m_DisplayMinMax)
            {
                var fieldsPanel = new VisualElement { name = "FieldsPanel" };
                m_MinField = AddMinMaxField(fieldsPanel, "Min", 1);
                m_MaxField = AddMinMaxField(fieldsPanel, "Max", 2);
                Add(fieldsPanel);
            }
        }

        public void OnNodeModified(ModificationScope scope)
        {
            if (scope == ModificationScope.Graph)
            {
                this.MarkDirtyRepaint();
            }
        }

        void OnChangeSlider(float newValue)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Slider Change");
            var value = (Vector3)m_PropertyInfo.GetValue(m_Node, null);
            value.x = newValue;
            m_PropertyInfo.SetValue(m_Node, value, null);
            if (m_SliderInput != null)
                m_SliderInput.value = newValue;
            m_PropertyInfo.SetValue(m_Node, m_Value, null);
            this.MarkDirtyRepaint();
        }

        void UpdateSlider()
        {
            m_SliderPanel.Remove(m_Slider);
            m_Slider = new Slider(m_Value.y, m_Value.z) { value = m_Value.x };
            m_Slider.RegisterValueChangedCallback((evt) => OnChangeSlider(evt.newValue));
            m_SliderPanel.Add(m_Slider);
            m_SliderPanel.Add(m_SliderInput);
        }

        FloatField AddMinMaxField(VisualElement panel, string label, int index)
        {
            var floatField = new FloatField { value = m_Value[index] };
            if (label != null)
            {
                var labelField = new Label(label);
                panel.Add(labelField);
            }

            floatField.RegisterValueChangedCallback(evt =>
            {
                m_Value[index] = (float)evt.newValue;
                m_PropertyInfo.SetValue(m_Node, m_Value, null);
                this.MarkDirtyRepaint();
            });
            floatField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
            {
                if (index == 1)
                {
                    m_Value[index] = Mathf.Min(m_Value[index], m_Value.z);
                    m_MinField.value = m_Value[index];
                }
                else
                {
                    m_Value[index] = Mathf.Max(m_Value[index], m_Value.y);
                    m_MaxField.value = m_Value[index];
                }
                float newValue = Mathf.Max(Mathf.Min(m_Value.x, m_Value.z), m_Value.y);
                m_Value.x = newValue;
                m_SliderInput.value = newValue;
                UpdateSlider();
                m_PropertyInfo.SetValue(m_Node, m_Value, null);
                this.MarkDirtyRepaint();
            }, TrickleDown.TrickleDown);

            panel.Add(floatField);
            return floatField;
        }

        void Repaint<T>(MouseEventBase<T> evt) where T : MouseEventBase<T>, new()
        {
            evt.StopPropagation();
            this.MarkDirtyRepaint();
        }
    }
}
