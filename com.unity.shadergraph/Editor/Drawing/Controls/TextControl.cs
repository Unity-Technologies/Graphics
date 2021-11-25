using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    class TextControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        public TextControlAttribute(string label = null)
        {
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (!TextControlView.validTypes.Contains(propertyInfo.PropertyType))
                return null;
            return new TextControlView(m_Label, node, propertyInfo);
        }
    }

    class TextControlView : VisualElement
    {
        public static Type[] validTypes = { typeof(string) };
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        string m_Value;
        int m_UndoGroup = -1;
        public TextControlView(string label, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/Controls/TextControlView"));
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);
            var container = new VisualElement { name = "container" };
            var thisLabel = new Label(label);
            container.Add(thisLabel);
            m_Value = GetValue();
            string value = null;
            var field = new TextField { value = m_Value };
            field.RegisterCallback<MouseDownEvent>(Repaint);
            field.RegisterCallback<MouseMoveEvent>(Repaint);
            field.RegisterValueChangedCallback(evt =>
            {
                value = GetValue();
                value = evt.newValue;
                if (m_Node.GetType() != typeof(SwizzleNode))
                {
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change" + m_Node.name);
                    m_PropertyInfo.SetValue(m_Node, value, null);
                    m_UndoGroup = -1;
                }
            });

            // Pressing escape while we are editing causes it to revert to the original value when we gained focus
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
            field.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
            {
                if (m_Node.GetType() == typeof(SwizzleNode))
                {
                    //Only set node value when mouse clicked away
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change" + m_Node.name);
                    m_PropertyInfo.SetValue(m_Node, value, null);
                    m_UndoGroup = -1;
                    //Validate graph to update downstream input slot
                    m_Node.owner.ValidateGraph();
                    m_Node.Dirty(ModificationScope.Topological);
                }
                this.MarkDirtyRepaint();
            });
            container.Add(field);
            Add(container);
        }

        string GetValue()
        {
            var value = m_PropertyInfo.GetValue(m_Node, null);
            Assert.IsNotNull(value);
            return (string)value;
        }

        void Repaint<T>(MouseEventBase<T> evt) where T : MouseEventBase<T>, new()
        {
            evt.StopPropagation();
            this.MarkDirtyRepaint();
        }
    }
}
