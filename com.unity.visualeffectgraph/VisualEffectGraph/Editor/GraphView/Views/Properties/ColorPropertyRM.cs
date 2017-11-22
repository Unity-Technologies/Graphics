#define USE_MY_COLOR_FIELD

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
using FloatField = UnityEditor.VFX.UIElements.OldFloatField;


namespace UnityEditor.VFX.UI
{
    class ColorPropertyRM : PropertyRM<Color>
    {
        VisualElement m_MainContainer;
        public ColorPropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
            m_MainContainer = new VisualElement();

#if USE_MY_COLOR_FIELD
            m_ColorField = new UnityEditor.VFX.UIElements.ColorField(m_Label);
            m_ColorField.OnValueChanged = OnValueChanged;
#else
            m_ColorField = new LabeledField<UnityEditor.Experimental.UIElements.ColorField, Color>(m_Label);
            m_ColorField.RegisterCallback<ChangeEvent<Color>>(OnValueChanged);
            // todo : get it from a slot attribute
            //m_ColorField.control.hdrConfig = new ColorPickerHDRConfig(-1, 5, 0, 3);
#endif


            m_MainContainer.Add(m_ColorField);
            m_MainContainer.AddToClassList("maincontainer");

            VisualElement fieldContainer = new VisualElement();
            fieldContainer.AddToClassList("fieldContainer");

            m_RFloatField = new FloatField("R");
            m_RFloatField.OnValueChanged = OnValueChanged;

            m_GFloatField = new FloatField("G");
            m_GFloatField.OnValueChanged = OnValueChanged;

            m_BFloatField = new FloatField("B");
            m_BFloatField.OnValueChanged = OnValueChanged;

            m_AFloatField = new FloatField("A");
            m_AFloatField.OnValueChanged = OnValueChanged;

            fieldContainer.Add(m_RFloatField);
            fieldContainer.Add(m_GFloatField);
            fieldContainer.Add(m_BFloatField);
            fieldContainer.Add(m_AFloatField);

            m_MainContainer.Add(fieldContainer);

            m_MainContainer.style.flexDirection = FlexDirection.Column;
            m_MainContainer.style.alignItems = Align.Stretch;
            Add(m_MainContainer);
        }

        public override float GetPreferredControlWidth()
        {
            return 200;
        }

        protected override void UpdateEnabled()
        {
            m_MainContainer.SetEnabled(propertyEnabled);
        }

        public void OnValueChanged(ChangeEvent<Color> e)
        {
            OnValueChanged();
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
                newValue = m_ColorField.value;
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

#if USE_MY_COLOR_FIELD
        UnityEditor.VFX.UIElements.ColorField m_ColorField;
#else
        LabeledField<UnityEditor.Experimental.UIElements.ColorField, Color> m_ColorField;
#endif

        public override void UpdateGUI()
        {
            m_ColorField.value = m_Value;
            m_RFloatField.SetValue(m_Value.r);
            m_GFloatField.SetValue(m_Value.g);
            m_BFloatField.SetValue(m_Value.b);
            m_AFloatField.SetValue(m_Value.a);
        }

        public override bool showsEverything { get { return true; } }
    }
}
