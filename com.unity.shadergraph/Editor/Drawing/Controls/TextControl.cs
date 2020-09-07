using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;

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

        public TextControlAttribute(int row, string label = null, string subLabel1 = "X", string subLabel2 = "Y", string subLabel3 = "Z", string subLabel4 = "W")
        {
            m_SubLabel1 = subLabel1;
            m_SubLabel2 = subLabel2;
            m_SubLabel3 = subLabel3;
            m_SubLabel4 = subLabel4;
            m_Label = label;
            m_row = row;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            var matrixSwizzleNode = node as MatrixSwizzleNode;
            if (matrixSwizzleNode == null)
                return null;
            if (!TextControlView.validTypes.Contains(propertyInfo.PropertyType))
                return null;
            return new TextControlView(m_row, m_Label, m_SubLabel1, m_SubLabel2, m_SubLabel3, m_SubLabel4, matrixSwizzleNode, propertyInfo);
        }
    }

    class TextControlView : VisualElement
    {
        public static Type[] validTypes = { typeof(MatrixSwizzleRow) };
        MatrixSwizzleNode m_Node;
        PropertyInfo m_PropertyInfo;
        int m_row;
        int m_UndoGroup = -1;

        public TextControlView(int row, string label, string subLabel1, string subLabel2, string subLabel3, string subLabel4, MatrixSwizzleNode node, PropertyInfo propertyInfo)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/TextControlView"));
            m_Node = node;
            Assert.IsNotNull(m_Node);
            m_Node.OnSizeChange += OnSizeChangeCallback;

            m_PropertyInfo = propertyInfo;
            m_row = row;
            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);
            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));

            MatrixSwizzleRow swizzleRow = GetValue();
            AddField(0, subLabel1, swizzleRow);
            AddField(1, subLabel2, swizzleRow);
            AddField(2, subLabel3, swizzleRow);
            AddField(3, subLabel4, swizzleRow);
        }

        //Set visibility of indices boxes 
        private void OnSizeChangeCallback(SwizzleOutputSize outputSize)
        {
            int size;
            bool IsMatrix;
            switch (outputSize)
            {
                default:
                    size = 4;
                    IsMatrix = true;
                    SetVisibility(size, IsMatrix);
                    break;
                case SwizzleOutputSize.Matrix3x3:
                    size = 3;
                    IsMatrix = true;
                    SetVisibility(size, IsMatrix);
                    break;
                case SwizzleOutputSize.Matrix2x2:
                    size = 2;
                    IsMatrix = true;
                    SetVisibility(size, IsMatrix);
                    break;
                case SwizzleOutputSize.Vector4:
                    size = 4;
                    IsMatrix = false;
                    SetVisibility(size, IsMatrix);
                    break;
                case SwizzleOutputSize.Vector3:
                    size = 3;
                    IsMatrix = false;
                    SetVisibility(size, IsMatrix);
                    break;
                case SwizzleOutputSize.Vector2:
                    size = 2;
                    IsMatrix = false;
                    SetVisibility(size, IsMatrix);
                    break;
                case SwizzleOutputSize.Vector1:
                    size = 1;
                    IsMatrix = false;
                    SetVisibility(size, IsMatrix);
                    break;
            }
        }

        private void SetVisibility(int size, bool IsMatrix)
        {
            var children = this.Children();
            this.SetEnabled(true);
            if (IsMatrix)
            {
                for (int i = 0; i< this.childCount; i++)
                {
                    children.ElementAt(i).SetEnabled(i < 2 * size);
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
                    children.ElementAt(i).SetEnabled(true);
                    if (i >= 2 )
                    {
                        children.ElementAt(i).SetEnabled(false);
                    }
                }

                if (this.m_row >= size)
                {
                    this.SetEnabled(false);
                }
            }
        }

        private char[] value_char = { '0', '0', '0', '0', '0', '0', '0', '0' };
 
        void AddField(int columnIndex, string subLabel, MatrixSwizzleRow swizzleRow)
        {
            var label = new Label(subLabel);
            label.style.alignSelf = Align.FlexEnd;
            Add(label);

            string fieldValue = swizzleRow.GetColumn(columnIndex);

            var field = new TextField { userData = columnIndex, value = fieldValue, maxLength = 2 };
            field.RegisterCallback<MouseDownEvent>(Repaint);
            field.RegisterCallback<MouseMoveEvent>(Repaint);
            field.RegisterValueChangedCallback(evt =>
            {
                MatrixSwizzleRow row = GetValue();
                string value = row.GetColumn(columnIndex);
                Assert.IsTrue(evt.newValue.Length <= 2);
                value = evt.newValue;
                field.SetValueWithoutNotify(value);
                row.SetColumn(columnIndex, value);
                m_UndoGroup = -1;
                this.MarkDirtyRepaint();
                m_Node.OnSwizzleChange();
            });

            // TODO: I don't think this event is getting called / doing anything useful
            field.Q("unity-text-input").RegisterCallback<InputEvent>(evt =>
            {
                if (m_UndoGroup == -1)
                {
                    m_UndoGroup = Undo.GetCurrentGroup();
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                }
                MatrixSwizzleRow row = GetValue();
                row.SetColumns(columnIndex, "kk");       // not sure when this event runs... or what it is trying to do
                this.MarkDirtyRepaint();
            });
            field.Q("unity-text-input").RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape && m_UndoGroup > -1)
                {
                    Undo.RevertAllDownToGroup(m_UndoGroup);
                    m_UndoGroup = -1;
                    evt.StopPropagation();
                }
                this.MarkDirtyRepaint();
            });
            Add(field);
        }

        MatrixSwizzleRow GetValue()
        {
            MatrixSwizzleRow value = m_PropertyInfo.GetValue(m_Node, null) as MatrixSwizzleRow;
            Assert.IsNotNull(value);
            return value;
        }
/*
        void SetValue(MatrixSwizzleRow value)
        {
            Assert.IsNotNull(value);
            m_PropertyInfo.SetValue(m_Node, value, null);
        }
*/
        void Repaint<T>(MouseEventBase<T> evt) where T : MouseEventBase<T>, new()
        {
            evt.StopPropagation();
            this.MarkDirtyRepaint();
        }
    }
}
