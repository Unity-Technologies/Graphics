using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    class TextControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        string m_SubLabel1;
        string m_SubLabel2;
        string m_SubLabel3;
        string m_SubLabel4;

        public TextControlAttribute(string label = null, string subLabel1 = "X", string subLabel2 = "Y", string subLabel3 = "Z", string subLabel4 = "W")
        {
            m_SubLabel1 = subLabel1;
            m_SubLabel2 = subLabel2;
            m_SubLabel3 = subLabel3;
            m_SubLabel4 = subLabel4;
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (!TextControlView.validTypes.Contains(propertyInfo.PropertyType))
                return null;
            return new TextControlView(m_Label, m_SubLabel1, m_SubLabel2, m_SubLabel3, m_SubLabel4, node, propertyInfo);
        }
    }

    class TextControlView : VisualElement
    {
        public static Type[] validTypes = { typeof(Vector4) };

        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        Vector4 m_Value;
        int m_UndoGroup = -1;

        public TextControlView(string label, string subLabel1, string subLabel2, string subLabel3, string subLabel4, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {

            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/TextControlView"));
            m_Node = node;
            m_PropertyInfo = propertyInfo;

            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);
            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));

            m_Value = GetValue();
            AddField(0, subLabel1);
            AddField(1, subLabel2);
            AddField(2, subLabel3);
            AddField(3, subLabel4);
        }

        void AddField(int index, string subLabel)
        {
            //var dummy = new VisualElement { name = "dummy" };
            var label = new Label(subLabel);
            label.style.alignSelf = Align.FlexEnd;
            //dummy.Add(label);
            Add(label);
            var field_x = new FloatField { userData = index, value = getIndex(m_Value[index])[0] };
            var field_y = new FloatField { userData = index, value = getIndex(m_Value[index])[1] };
            float x = 0;
            float y = 0;

            field_x.RegisterCallback<MouseDownEvent>(Repaint);
            field_x.RegisterCallback<MouseMoveEvent>(Repaint);
            field_x.RegisterValueChangedCallback(evt =>
            {
                var value = GetValue();
                Debug.Log(index+"x: " +evt.newValue);
                x = (float)evt.newValue;
                value[index] = x + y * 0.1f;
                SetValue(value);
                m_UndoGroup = -1;
                this.MarkDirtyRepaint();
            });
            field_x.Q("unity-text-input").RegisterCallback<InputEvent>(evt =>
            {
                if (m_UndoGroup == -1)
                {
                    m_UndoGroup = Undo.GetCurrentGroup();
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                }
                float newValue;
                if (!float.TryParse(evt.newData, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out newValue))
                    newValue = 0f;
                var value = GetValue();
                x = newValue;
                value[index] = x + y * 0.1f;
                SetValue(value);
                this.MarkDirtyRepaint();
            });
            field_x.Q("unity-text-input").RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape && m_UndoGroup > -1)
                {
                    Undo.RevertAllDownToGroup(m_UndoGroup);
                    m_UndoGroup = -1;
                    m_Value = GetValue();
                    evt.StopPropagation();
                }
                this.MarkDirtyRepaint();
            });
            Add(field_x);

            field_y.RegisterCallback<MouseDownEvent>(Repaint);
            field_y.RegisterCallback<MouseMoveEvent>(Repaint);
            field_y.RegisterValueChangedCallback(evt =>
            {
                var value = GetValue();
                Debug.Log(index+"y: " + evt.newValue);
                y= (float)evt.newValue;
                value[index] = x + y*0.1f;
                SetValue(value);
                m_UndoGroup = -1;
                this.MarkDirtyRepaint();
            });
            field_y.Q("unity-text-input").RegisterCallback<InputEvent>(evt =>
            {
                if (m_UndoGroup == -1)
                {
                    m_UndoGroup = Undo.GetCurrentGroup();
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                }
                float newValue;
                if (!float.TryParse(evt.newData, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out newValue))
                    newValue = 0f;
                var value = GetValue();
                y = newValue;
                value[index] = x + y * 0.1f;
                SetValue(value);
                this.MarkDirtyRepaint();
            });
            field_y.Q("unity-text-input").RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape && m_UndoGroup > -1)
                {
                    Undo.RevertAllDownToGroup(m_UndoGroup);
                    m_UndoGroup = -1;
                    m_Value = GetValue();
                    evt.StopPropagation();
                }
                this.MarkDirtyRepaint();
            });
            Add(field_y);
        }

        object ValueToPropertyType(Vector4 value)
        {
            return value;
        }

        Vector4 GetValue()
        {
            var value = m_PropertyInfo.GetValue(m_Node, null);
            return (Vector4)value;
        }

        void SetValue(Vector4 value)
        {
            m_PropertyInfo.SetValue(m_Node, ValueToPropertyType(value), null);
        }
        float[] getIndex(float input)
        {
            var row = (int)input;
            float temp_col = (float)(input - Math.Truncate(input)) * 10;
            float col = 0;

            if (temp_col > 2.5)
            {
                col = 3;
            }
            else if (temp_col > 1.5 && temp_col < 2.5)
            {
                col = 2;
            }
            else if (temp_col > 0.5 && temp_col < 1.5)
            {
                col = 1;
            }
            else
            {
                col = 0;
            }

            float[] index = { row, col };

            return index;

        }

        void Repaint<T>(MouseEventBase<T> evt) where T : MouseEventBase<T>, new()
        {
            evt.StopPropagation();
            this.MarkDirtyRepaint();
        }
    }
}
