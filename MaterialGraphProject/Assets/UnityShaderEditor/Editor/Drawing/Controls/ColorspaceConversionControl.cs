using System;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColorspaceConversionControlAttribute : Attribute, IControlAttribute
    {
        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new ColorspaceConversionControlView(node, propertyInfo);
        }
    }

    public class ColorspaceConversionControlView : VisualElement
    {
        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;

        ColorspaceConversion conversion
        {
            get { return (ColorspaceConversion)m_PropertyInfo.GetValue(m_Node, null); }
            set { m_PropertyInfo.SetValue(m_Node, value, null); }
        }

        public ColorspaceConversionControlView(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            var value = conversion;

            var fromField = new EnumField(value.from);
            fromField.OnValueChanged(OnFromChanged);
            Add(fromField);

            var arrowLabel = new Label("➔");
            Add(arrowLabel);

            var toField = new EnumField(value.to);
            toField.OnValueChanged(OnToChanged);
            Add(toField);
        }

        void OnFromChanged(ChangeEvent<Enum> evt)
        {
            var value = conversion;
            var newValue = (Colorspace)evt.newValue;
            if (value.from != newValue)
            {
                m_Node.owner.owner.RegisterCompleteObjectUndo("Change Colorspace From");
                value.from = newValue;
                conversion = value;
            }
        }

        void OnToChanged(ChangeEvent<Enum> evt)
        {
            var value = conversion;
            var newValue = (Colorspace)evt.newValue;
            if (value.to != newValue)
            {
                m_Node.owner.owner.RegisterCompleteObjectUndo("Change Colorspace To");
                value.to = newValue;
                conversion = value;
            }
        }
    }
}
