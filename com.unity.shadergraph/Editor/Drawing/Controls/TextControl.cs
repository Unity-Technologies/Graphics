using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
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
        int m_row;
        string m_defaultValue;

        public TextControlAttribute(string defaultValue, int row, string label = null, string subLabel1 = "X", string subLabel2 = "Y", string subLabel3 = "Z", string subLabel4 = "W")
        {
            m_SubLabel1 = subLabel1;
            m_SubLabel2 = subLabel2;
            m_SubLabel3 = subLabel3;
            m_SubLabel4 = subLabel4;
            m_Label = label;
            m_row = row;
            m_defaultValue = defaultValue;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (!TextControlView.validTypes.Contains(propertyInfo.PropertyType))
                return null;
            return new TextControlView(m_defaultValue, m_row, m_Label, m_SubLabel1, m_SubLabel2, m_SubLabel3, m_SubLabel4, node, propertyInfo);
        }
    }

    class TextControlView : VisualElement
    {
        public static Type[] validTypes = { typeof(string) };
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        string m_Value;
        int m_row;
        int m_UndoGroup = -1;

        public TextControlView(string defaultValue, int row, string label, string subLabel1, string subLabel2, string subLabel3, string subLabel4, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/TextControlView"));
            m_Node = node;
            var MatrixSwizzleNode = m_Node as MatrixSwizzleNode;
            if (MatrixSwizzleNode != null)
            {
                MatrixSwizzleNode.OnSizeChange += Callback;
            }
            m_PropertyInfo = propertyInfo;
            m_row = row;
            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);
            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));
            m_Value = GetValue();
            if (m_Value == null)
            {
                m_Value = defaultValue;
            }
            SetValue(m_Value);
            AddField(0, subLabel1);
            AddField(1, subLabel2);
            AddField(2, subLabel3);
            AddField(3, subLabel4);
        }

        //Set visibility of indices boxes 
        private void Callback(string OutputSize)
        {
            int size;
            bool IsMatrix;
            switch (OutputSize)
            {
                default:
                    size = 4;
                    IsMatrix = true;
                    SetVisibility(size, IsMatrix);
                    break;
                case "Matrix3x3":
                    size = 3;
                    IsMatrix = true;
                    SetVisibility(size, IsMatrix);
                    break;
                case "Matrix2x2":
                    size = 2;
                    IsMatrix = true;
                    SetVisibility(size, IsMatrix);
                    break;
                case "Vector4":
                    size = 4;
                    IsMatrix = false;
                    SetVisibility(size, IsMatrix);
                    break;
                case "Vector3":
                    size = 3;
                    IsMatrix = false;
                    SetVisibility(size, IsMatrix);
                    break;
                case "Vector2":
                    size = 2;
                    IsMatrix = false;
                    SetVisibility(size, IsMatrix);
                    break;
                case "Vector1":
                    size = 1;
                    IsMatrix = false;
                    SetVisibility(size, IsMatrix);
                    break;
            }
        }

        private void SetVisibility(int size, bool IsMatrix)
        {
            var childern = this.Children();
            this.SetEnabled(true);
            if (IsMatrix)
            {
                for (int i = 0; i< this.childCount; i++)
                {
                    childern.ElementAt(i).SetEnabled(true);
                    if (i>= 2 * size)
                    {
                        childern.ElementAt(i).SetEnabled(false);
                    }
                }

                if (this.m_row >= size)
                {
                    this.SetEnabled(false);
                }
            }
            else
            {
                for (int i = 0; i < this.childCount; i++)
                {
                    childern.ElementAt(i).SetEnabled(true);
                    if (i >= 2 )
                    {
                        childern.ElementAt(i).SetEnabled(false);
                    }
                }

                if (this.m_row >= size)
                {
                    this.SetEnabled(false);
                }
            }
        }

        private char[] value_char = { '0', '0', '0', '0', '0', '0', '0', '0' };
 
        void AddField(int index, string subLabel)
        {
            var label = new Label(subLabel);
            label.style.alignSelf = Align.FlexEnd;
            Add(label);
            string field_value = m_Value;

            if (m_Value.Length>= 2 * index + 1)
            {
                field_value = m_Value[2 * index].ToString();
                field_value += m_Value[2 * index + 1].ToString();
            }

            var field = new TextField { userData = index, value = field_value, maxLength = 2 };
            field.RegisterCallback<MouseDownEvent>(Repaint);
            field.RegisterCallback<MouseMoveEvent>(Repaint);
            field.RegisterValueChangedCallback(evt =>
            {
                var value = GetValue();
                value_char = value.ToCharArray();
                if (evt.newValue.Length != 0)
                {
                    value_char[2 * index] = evt.newValue[0];
                    if (evt.newValue.Length == 2)
                        value_char[2 * index + 1] = evt.newValue[1];
                }


                if (evt.newValue.Equals(""))
                {
                    value = GetValue();
                    value_char = value.ToCharArray();

                    value_char[2 * index] = 'x';
                    value_char[2 * index + 1] = 'x';
                }

                for (int i = 0; i < evt.newValue.Length; i++)
                {
                    if ((evt.newValue[i] < '0' || evt.newValue[i] > '9') && !evt.newValue.Equals(""))
                    {
                        value = GetValue();
                        value_char = value.ToCharArray();
                        if (value.Contains('x'))
                        {
                            field.SetValueWithoutNotify("00");
                            value_char[2 * index] = '0';
                            value_char[2 * index + 1] = '0';
                        }
                        else
                        {
                            field.SetValueWithoutNotify(value_char[2 * index].ToString() + value_char[2 * index + 1].ToString());
                        }
                        
                        value = new string(value_char);
                        SetValue(value);
                        m_UndoGroup = -1;
                        this.MarkDirtyRepaint();
                    }
                }

                value = new string(value_char);
                SetValue(value);
                //Debug.Log("value: "+value);
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

                string newValue = "";
                var value = GetValue();
                value_char = value.ToCharArray();
                value_char[2 * index] = newValue[0];

                if (newValue.Length >= 2)
                   value_char[2 * index + 1] = newValue[1];

                value = new string(value_char);
                SetValue(value);
                this.MarkDirtyRepaint();
            });
            field.Q("unity-text-input").RegisterCallback<KeyDownEvent>(evt =>
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
            Add(field);
        }

        object ValueToPropertyType(string value)
        {
            return value;
        }

        string GetValue()
        {
            var value = m_PropertyInfo.GetValue(m_Node, null);
            return (string)value;
        }

        void SetValue(string value)
        {
            m_PropertyInfo.SetValue(m_Node, ValueToPropertyType(value), null);
        }

        void Repaint<T>(MouseEventBase<T> evt) where T : MouseEventBase<T>, new()
        {
            evt.StopPropagation();
            this.MarkDirtyRepaint();
        }
    }
}
