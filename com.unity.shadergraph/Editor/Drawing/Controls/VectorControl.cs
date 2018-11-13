using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MultiFloatControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        string m_SubLabel1;
        string m_SubLabel2;
        string m_SubLabel3;
        string m_SubLabel4;

        public MultiFloatControlAttribute(string label = null, string subLabel1 = "X", string subLabel2 = "Y", string subLabel3 = "Z", string subLabel4 = "W")
        {
            m_SubLabel1 = subLabel1;
            m_SubLabel2 = subLabel2;
            m_SubLabel3 = subLabel3;
            m_SubLabel4 = subLabel4;
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (!MultiFloatControlView.validTypes.Contains(propertyInfo.PropertyType))
                return null;

            Vector4 Getter()
            {
                var value = propertyInfo.GetValue(node, null);
                if (propertyInfo.PropertyType == typeof(float))
                    return new Vector4((float)value, 0f, 0f, 0f);
                if (propertyInfo.PropertyType == typeof(Vector2))
                    return (Vector2)value;
                if (propertyInfo.PropertyType == typeof(Vector3))
                    return (Vector3)value;
                return (Vector4)value;
            }

            void Setter(Vector4 value)
            {
                object castedValue;
                if (propertyInfo.PropertyType == typeof(float))
                    castedValue = value.x;
                if (propertyInfo.PropertyType == typeof(Vector2))
                    castedValue = (Vector2)value;
                if (propertyInfo.PropertyType == typeof(Vector3))
                    castedValue = (Vector3)value;
                else
                    castedValue = value;
                propertyInfo.SetValue(node, castedValue, null);
            }

            var label = m_Label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);
            return new MultiFloatControlView(label, m_SubLabel1, m_SubLabel2, m_SubLabel3, m_SubLabel4, node, propertyInfo.PropertyType, Getter, Setter);
        }

        public static Func<Vector4> Getter(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return () =>
            {
                var value = propertyInfo.GetValue(node, null);
                if (propertyInfo.PropertyType == typeof(float))
                    return new Vector4((float)value, 0f, 0f, 0f);
                if (propertyInfo.PropertyType == typeof(Vector2))
                    return (Vector2)value;
                if (propertyInfo.PropertyType == typeof(Vector3))
                    return (Vector3)value;
                return (Vector4)value;
            };
        }

        public static Action<Vector4> Setter(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return (Vector4 value) =>
            {
                object castedValue;
                if (propertyInfo.PropertyType == typeof(float))
                    castedValue = value.x;
                if (propertyInfo.PropertyType == typeof(Vector2))
                    castedValue = (Vector2)value;
                if (propertyInfo.PropertyType == typeof(Vector3))
                    castedValue = (Vector3)value;
                else
                    castedValue = value;
                propertyInfo.SetValue(node, castedValue, null);
            };
        }
    }

    public class MultiFloatControlView : VisualElement
    {
        public static Type[] validTypes = { typeof(float), typeof(Vector2), typeof(Vector3), typeof(Vector4) };

        AbstractMaterialNode m_Node;
        Func<Vector4> m_Getter;
        Action<Vector4> m_Setter;
        Vector4 m_Value;
        int m_UndoGroup = -1;

        public MultiFloatControlView(string label, string subLabel1, string subLabel2, string subLabel3, string subLabel4, AbstractMaterialNode node, Type type, Func<Vector4> getter, Action<Vector4> setter)
        {
            var components = Array.IndexOf(validTypes, type) + 1;
            if (components == -1)
                throw new ArgumentException("Property must be of type float, Vector2, Vector3 or Vector4.", "propertyInfo");

            AddStyleSheetPath("Styles/Controls/MultiFloatControlView");
            m_Node = node;
            m_Getter = getter;
            m_Setter = setter;

            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));

            m_Value = m_Getter();
            AddField(0, subLabel1);
            if (components > 1)
                AddField(1, subLabel2);
            if (components > 2)
                AddField(2, subLabel3);
            if (components > 3)
                AddField(3, subLabel4);
        }

        void AddField(int index, string subLabel)
        {
            var dummy = new VisualElement { name = "dummy" };
            var label = new Label(subLabel);
            dummy.Add(label);
            Add(dummy);
            var field = new FloatField { userData = index, value = m_Value[index] };
            var dragger = new FieldMouseDragger<double>(field);
            dragger.SetDragZone(label);
            field.RegisterCallback<MouseDownEvent>(Repaint);
            field.RegisterCallback<MouseMoveEvent>(Repaint);
            field.OnValueChanged(evt =>
                {
                    var value = m_Getter();
                    value[index] = (float)evt.newValue;
                    m_Setter(value);
                    m_UndoGroup = -1;
                    this.MarkDirtyRepaint();
                });
            field.RegisterCallback<InputEvent>(evt =>
                {
                    if (m_UndoGroup == -1)
                    {
                        m_UndoGroup = Undo.GetCurrentGroup();
                        m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                    }
                    float newValue;
                    if (!float.TryParse(evt.newData, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out newValue))
                        newValue = 0f;
                    var value = m_Getter();
                    value[index] = newValue;
                    m_Setter(value);
                    this.MarkDirtyRepaint();
                });
            field.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Escape && m_UndoGroup > -1)
                    {
                        Undo.RevertAllDownToGroup(m_UndoGroup);
                        m_UndoGroup = -1;
                        m_Value = m_Getter();
                        evt.StopPropagation();
                    }
                    this.MarkDirtyRepaint();
                });
            Add(field);
        }

        void Repaint<T>(MouseEventBase<T> evt) where T : MouseEventBase<T>, new()
        {
            evt.StopPropagation();
            this.MarkDirtyRepaint();
        }
    }
}
