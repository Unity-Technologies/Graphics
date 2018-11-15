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

    class SliderControlView : VisualElement, INodeModificationListener
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        bool m_DisplayMinMax;
        int m_UndoGroup = -1;
        Vector3 m_Value;

        VisualElement m_SliderPanel;
        Slider m_Slider;
        FloatField m_SliderInput;

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

            m_SliderPanel = new VisualElement { name = "SliderPanel" };
            if (!string.IsNullOrEmpty(label))
                m_SliderPanel.Add(new Label(label));
            m_Slider = new Slider(m_Value.y, m_Value.z);
            m_Slider.RegisterValueChangedCallback((evt) => OnChangeSlider(evt.newValue));

            m_Slider.value = m_Value.x;
            m_SliderPanel.Add(m_Slider);
            m_SliderInput = AddField(m_SliderPanel, "", 0, m_Value);
            Add(m_SliderPanel);

            if (m_DisplayMinMax)
            {
                var fieldsPanel = new VisualElement { name = "FieldsPanel" };
                AddField(fieldsPanel, "Min", 1, m_Value);
                AddField(fieldsPanel, "Max", 2, m_Value);
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
            this.MarkDirtyRepaint();
        }

        FloatField AddField(VisualElement panel, string label, int index, Vector3 initValiue)
        {
            var field = new FloatField { userData = index, value = initValiue[index] };

            if (label != "")
            {
                var l = new Label(label);
                panel.Add(l);
                var dragger = new FieldMouseDragger<double>(field);
                dragger.SetDragZone(l);
            }

            field.RegisterCallback<MouseDownEvent>(Repaint);
            field.RegisterCallback<MouseMoveEvent>(Repaint);
            field.RegisterValueChangedCallback(evt =>
                {
                    var value = (Vector3)m_PropertyInfo.GetValue(m_Node, null);
                    value[index] = (float)evt.newValue;
                    m_PropertyInfo.SetValue(m_Node, value, null);
                    m_UndoGroup = -1;
                    this.MarkDirtyRepaint();
                });
            field.Q("unity-text-input").RegisterCallback<InputEvent>(evt =>
                {
                    if (m_UndoGroup == -1)
                    {
                        m_UndoGroup = Undo.GetCurrentGroup();
                        m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                    }
                    float newValue;
                    if (!float.TryParse(evt.newData, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out newValue))
                        newValue = 0f;
                    var value = (Vector3)m_PropertyInfo.GetValue(m_Node, null);
                    value[index] = newValue;
                    m_PropertyInfo.SetValue(m_Node, value, null);
                    if(evt.newData.Length != 0 
                        && evt.newData[evt.newData.Length-1] != '.' 
                        && evt.newData[evt.newData.Length-1] != ',')
                        UpdateSlider(m_SliderPanel, index, value);
                    this.MarkDirtyRepaint();
                });
            field.Q("unity-text-input").RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Escape && m_UndoGroup > -1)
                    {
                        Undo.RevertAllDownToGroup(m_UndoGroup);
                        m_UndoGroup = -1;
                        m_Value = (Vector3)m_PropertyInfo.GetValue(m_Node, null);
                        UpdateSlider(m_SliderPanel, index, m_Value);
                        evt.StopPropagation();
                    }
                    this.MarkDirtyRepaint();
                });
            panel.Add(field);
            return field;
        }

        void UpdateSlider(VisualElement panel, int index, Vector3 value)
        {
            value.x = Mathf.Max(Mathf.Min(value.x, value.z), value.y);
            panel.Remove(m_Slider);
            m_Slider = new Slider(value.y, value.z);
            m_Slider.RegisterValueChangedCallback((evt) => OnChangeSlider(evt.newValue));

            m_Slider.lowValue = value.y;
            m_Slider.highValue = value.z;
            m_Slider.value = value.x;
            panel.Add(m_Slider);

            panel.Remove(m_SliderInput);
            if (index != 0)
                m_SliderInput = AddField(panel, "", 0, value);
            m_SliderInput.value = value.x;
            panel.Add(m_SliderInput);
        }

        void Repaint<T>(MouseEventBase<T> evt) where T : MouseEventBase<T>, new()
        {
            evt.StopPropagation();
            this.MarkDirtyRepaint();
        }
    }
}
