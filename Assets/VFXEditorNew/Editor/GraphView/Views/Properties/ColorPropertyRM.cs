using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{
    class ColorPropertyRM : PropertyRM<Color>
    {
        public ColorPropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter,labelWidth)
        {
            VisualContainer mainContainer = new VisualContainer();

            m_ColorField = new ColorField(m_Label);
            m_ColorField.onValueChanged = OnValueChanged;

            mainContainer.AddChild(m_ColorField);
            mainContainer.AddToClassList("maincontainer");

            VisualContainer fieldContainer = new VisualContainer();
            fieldContainer.AddToClassList("fieldContainer");

            m_RFloatField = new FloatField("R");
            m_RFloatField.onValueChanged = OnValueChanged;

            m_GFloatField = new FloatField("G");
            m_GFloatField.onValueChanged = OnValueChanged;

            m_BFloatField = new FloatField("B");
            m_BFloatField.onValueChanged = OnValueChanged;

            m_AFloatField = new FloatField("A");
            m_AFloatField.onValueChanged = OnValueChanged;

            fieldContainer.AddChild(m_RFloatField);
            fieldContainer.AddChild(m_GFloatField);
            fieldContainer.AddChild(m_BFloatField);
            fieldContainer.AddChild(m_AFloatField);

            mainContainer.AddChild(fieldContainer);

            mainContainer.flexDirection = FlexDirection.Column;
            mainContainer.alignItems = Align.Stretch;
            AddChild(mainContainer);
        }

        public void OnValueChanged()
        {
            Color newValue = new Color(m_RFloatField.GetValue(), m_GFloatField.GetValue(), m_BFloatField.GetValue(), m_AFloatField.GetValue());
            if (newValue != m_Value)
            {
                m_Value = newValue;
                NotifyValueChanged();
            }
            else
            {
                newValue = m_ColorField.GetValue();
                if (newValue != m_Value)
                {
                    m_Value = newValue;
                    NotifyValueChanged();
                }
            }
        }

        FloatField m_RFloatField;
        FloatField m_GFloatField;
        FloatField m_BFloatField;
        FloatField m_AFloatField;
        ColorField m_ColorField;

        public override void UpdateGUI()
        {
            m_ColorField.SetValue(m_Value);
            m_RFloatField.SetValue(m_Value.r);
            m_GFloatField.SetValue(m_Value.g);
            m_BFloatField.SetValue(m_Value.b);
            m_AFloatField.SetValue(m_Value.a);
        }

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                if (m_RFloatField != null)
                    m_RFloatField.enabled = value;
                if (m_GFloatField != null)
                    m_GFloatField.enabled = value;
                if (m_BFloatField != null)
                    m_BFloatField.enabled = value;
                if (m_AFloatField != null)
                    m_AFloatField.enabled = value;
                if (m_ColorField != null)
                    m_ColorField.enabled = value;
            }
        }
    }
}
