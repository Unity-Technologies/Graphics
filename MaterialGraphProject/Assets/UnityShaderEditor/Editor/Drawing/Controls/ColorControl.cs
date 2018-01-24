using System;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    public enum ColorMode
    {
        Default,
        HDR
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ColorControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        ColorMode m_ColorMode;

        public ColorControlAttribute(string label = null, ColorMode colorMode = ColorMode.Default)
        {
            m_Label = label;
            m_ColorMode = colorMode;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ColorControlView(m_Label, m_ColorMode, node, propertyInfo);
        }
    }

    public class ColorControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        public ColorControlView(string label, ColorMode colorMode, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            if (propertyInfo.PropertyType != typeof(Color))
                throw new ArgumentException("Property must be of type Color.", "propertyInfo");
            label = label ?? ObjectNames.NicifyVariableName(propertyInfo.Name);

            if (!string.IsNullOrEmpty(label))
                Add(new Label(label));

            ColorField colorField;
            switch(colorMode)
            {
                case ColorMode.HDR:
                    colorField = new ColorField { value = (Color)m_PropertyInfo.GetValue(m_Node, null), hdr = true, showEyeDropper = false };
                    break;
                default:
                    colorField = new ColorField { value = (Color)m_PropertyInfo.GetValue(m_Node, null), showEyeDropper = false };
                    break;
            }
            colorField.OnValueChanged(OnChange);
            Add(colorField);
        }

        void OnChange(ChangeEvent<Color> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Color Change");
            m_PropertyInfo.SetValue(m_Node, evt.newValue, null);
            Dirty(ChangeType.Repaint);
        }
    }
}
