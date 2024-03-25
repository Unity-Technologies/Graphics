#define USE_MY_COLOR_FIELD

using UnityEngine;
using UnityEngine.UIElements;


namespace UnityEditor.VFX.UI
{
    class ColorPropertyRM : PropertyRM<Color>
    {
        VisualElement m_MainContainer;
        public ColorPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_MainContainer = new VisualElement();

#if USE_MY_COLOR_FIELD
            var label = new Label(ObjectNames.NicifyVariableName(controller.name));
            m_ColorField = new VFXColorField(label);
            m_ColorField.OnValueChanged = OnValueChanged;
            Add(label);
#else
            m_ColorField = new LabeledField<UnityEditor.UIElements.ColorField, Color>(m_Label);
            m_ColorField.RegisterCallback<ChangeEvent<Color>>(OnValueChanged);
#endif


            m_MainContainer.Add(m_ColorField);
            m_MainContainer.AddToClassList("maincontainer");

            VisualElement fieldContainer = new VisualElement();
            fieldContainer.AddToClassList("fieldContainer");

            m_FloatFields = new FloatField[4];
            for (int i = 0; i < 4; ++i)
            {
                m_FloatFields[i] = new FloatField(names[i]);
                m_FloatFields[i].RegisterCallback<ChangeEvent<float>>(OnValueChanged);
                fieldContainer.Add(m_FloatFields[i]);
            }

            m_MainContainer.Add(fieldContainer);

            m_FloatFields[0].AddToClassList("first");

            Add(m_MainContainer);
        }

        public override float GetPreferredControlWidth() => 224;

        protected override void UpdateEnabled()
        {
            bool enabled = propertyEnabled;
            m_ColorField.SetEnabled(enabled);
            for (int i = 0; i < 4; ++i)
            {
                m_FloatFields[i].SetEnabled(enabled);
            }
        }

        protected override void UpdateIndeterminate()
        {
            m_ColorField.indeterminate = indeterminate;
            for (int i = 0; i < 4; ++i)
                m_FloatFields[i].showMixedValue = indeterminate;
        }

        public void OnValueChanged(ChangeEvent<Color> e)
        {
            OnValueChanged(false);
        }

        void OnValueChanged(ChangeEvent<float> e)
        {
            OnValueChanged(true);
        }

        void OnValueChanged()
        {
            OnValueChanged(false);
        }

        void OnValueChanged(bool fromField)
        {
            if (fromField)
            {
                Color newValue = new Color(m_FloatFields[0].value, m_FloatFields[1].value, m_FloatFields[2].value, m_FloatFields[3].value);
                if (newValue != m_Value)
                {
                    m_Value = newValue;
                    NotifyValueChanged();
                }
            }
            else
            {
                Color newValue = m_ColorField.value;
                if (newValue != m_Value)
                {
                    m_Value = newValue;
                    NotifyValueChanged();
                }
            }
        }

        FloatField[] m_FloatFields;

        readonly string[] names = new string[]
        {
            "R",
            "G",
            "B",
            "A"
        };


#if USE_MY_COLOR_FIELD
        UnityEditor.VFX.UI.VFXColorField m_ColorField;
#else
        LabeledField<UnityEditor.UIElements.ColorField, Color> m_ColorField;
#endif

        public override void UpdateGUI(bool force)
        {
            m_ColorField.value = m_Value;
            for (int i = 0; i < 4; ++i)
            {
                m_FloatFields[i].SetValueWithoutNotify(m_Value[i]);
            }
        }

        public override object FilterValue(object value)
        {

            Color colorValue = (Color)value;
            value =  new Color(Mathf.Max(colorValue.r, 0),
                Mathf.Max(colorValue.g, 0),
                Mathf.Max(colorValue.b, 0),
                Mathf.Max(colorValue.a, 0));

            return value;
        }

        public override bool showsEverything => true;
    }
}
