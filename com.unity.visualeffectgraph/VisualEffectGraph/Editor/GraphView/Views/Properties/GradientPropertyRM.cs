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
    class GradientPropertyRM : PropertyRM<Gradient>
    {
        public GradientPropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
            VisualElement mainContainer = new VisualElement();

            m_GradientField = new GradientField(m_Label);
            m_GradientField.OnValueChanged = OnValueChanged;

            m_GradientField.style.flexDirection = FlexDirection.Column;
            m_GradientField.style.alignItems = Align.Stretch;
            m_GradientField.style.flex = 1;

            Add(m_GradientField);
        }

        public void OnValueChanged()
        {
            Gradient newValue = m_GradientField.GetValue();
            m_Value = newValue;
            NotifyValueChanged();
        }

        GradientField m_GradientField;

        public override void UpdateGUI()
        {
            m_GradientField.SetValue(m_Value);
        }

        public override bool showsEverything { get { return true; } }
    }
}
