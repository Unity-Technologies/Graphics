using System;
using System.Text;
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
        public static Type[] validTypes = { typeof(string) };

        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        string m_Value;
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
            
            if (m_Value == "")
            {
                m_Value = "00000000";
            }
            SetValue(m_Value);
            Debug.Log("m_value: "+m_Value);
            AddField(0, subLabel1);
            AddField(1, subLabel2);
            AddField(2, subLabel3);
            AddField(3, subLabel4);
        }
        private char[] value_char = { '0', '0', '0', '0', '0', '0', '0', '0' };
        void AddField(int index, string subLabel)
        {
            //var dummy = new VisualElement { name = "dummy" };
            var label = new Label(subLabel);
            label.style.alignSelf = Align.FlexEnd;
            //dummy.Add(label);
            Add(label);
            string field_value = m_Value;
            if (m_Value.Length>= 2 * index + 1)
            {
                field_value = m_Value[2 * index].ToString();
                field_value += m_Value[2 * index + 1].ToString();
  
            }
 
                
                var field_x = new TextField { userData = index, value = field_value };
            //char[] value_char = {'0', '0', '0', '0', '0', '0', '0', '0' };

            field_x.RegisterCallback<MouseDownEvent>(Repaint);
            field_x.RegisterCallback<MouseMoveEvent>(Repaint);
            field_x.RegisterValueChangedCallback(evt =>
            {
                var value = GetValue();
                

                Debug.Log(index + "value_char: " + value_char);
                value_char[2 * index] = evt.newValue[0];

                if (evt.newValue.Length>=2)
                value_char[2 * index + 1] = evt.newValue[1];

                //for (int i = 0; i< value_char.Length; i++)
                //{
                //    Debug.Log("value_char[" + i + "]: " + value_char[i]);
                //}


                value = new string(value_char);
                Debug.Log(index+ " evt.newValue: " + evt.newValue);
                Debug.Log(index + "value: " + value);
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
                string newValue = "";
               // if (!float.TryParse(evt.newData, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out newValue))
                //    newValue = 0f;
                var value = GetValue();


                value_char[2 * index] = newValue[0];

                if (newValue.Length >= 2)
                   value_char[2 * index + 1] = newValue[1];
                value = new string(value_char);

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
        }



        object ValueToPropertyType(string value)
        {
            return value;
        }

        string GetValue()
        {
            var value = m_PropertyInfo.GetValue(m_Node, null);
            Debug.Log("GetValue():" + value);
            return (string)value;
        }

        void SetValue(string value)
        {
            Debug.Log("SetValue():" + value);
            m_PropertyInfo.SetValue(m_Node, ValueToPropertyType(value), null);
        }

        void Repaint<T>(MouseEventBase<T> evt) where T : MouseEventBase<T>, new()
        {
            evt.StopPropagation();
            this.MarkDirtyRepaint();
        }
    }
}
