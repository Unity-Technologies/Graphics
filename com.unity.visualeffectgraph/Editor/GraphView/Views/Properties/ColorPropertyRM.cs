#define USE_MY_COLOR_FIELD

using UnityEngine;
using UnityEngine.UIElements;
using FloatField = UnityEditor.VFX.UI.VFXLabeledField<UnityEditor.UIElements.FloatField, float>;


namespace UnityEditor.VFX.UI
{
    class ColorPropertyRM : PropertyRM<Color>
    {
        VisualElement m_MainContainer;
        public ColorPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_MainContainer = new VisualElement();

#if USE_MY_COLOR_FIELD
            m_ColorField = new UnityEditor.VFX.UI.VFXColorField(m_Label);
            m_ColorField.OnValueChanged = OnValueChanged;
#else
            m_ColorField = new LabeledField<UnityEditor.UIElements.ColorField, Color>(m_Label);
            m_ColorField.RegisterCallback<ChangeEvent<Color>>(OnValueChanged);
#endif


            m_MainContainer.Add(m_ColorField);
            m_MainContainer.AddToClassList("maincontainer");

            VisualElement fieldContainer = new VisualElement();
            fieldContainer.AddToClassList("fieldContainer");

            m_FloatFields = new FloatField[4];
            m_TooltipHolders = new VisualElement[4];
            m_FieldParents = new VisualElement[4];
            for (int i = 0; i < 4; ++i)
            {
                m_FloatFields[i] = new FloatField(names[i]);
                m_FloatFields[i].RegisterCallback<ChangeEvent<float>>(OnValueChanged);

                m_FieldParents[i] = new VisualElement();
                m_FieldParents[i].Add(m_FloatFields[i]);
                m_FieldParents[i].style.flexGrow = 1;
                m_TooltipHolders[i] = new VisualElement();
                m_TooltipHolders[i].style.position = UnityEngine.UIElements.Position.Absolute;
                m_TooltipHolders[i].style.top = 0;
                m_TooltipHolders[i].style.left = 0;
                m_TooltipHolders[i].style.right = 0;
                m_TooltipHolders[i].style.bottom = 0;
                fieldContainer.Add(m_FieldParents[i]);
            }

            m_MainContainer.Add(fieldContainer);

            m_FloatFields[0].label.AddToClassList("first");

            Add(m_MainContainer);
        }

        public override float GetPreferredControlWidth()
        {
            return 224;
        }

        protected override void UpdateEnabled()
        {
            bool enabled = propertyEnabled;
            m_ColorField.SetEnabled(enabled);
            for (int i = 0; i < 4; ++i)
            {
                m_FloatFields[i].SetEnabled(enabled);
                if (enabled)
                {
                    if (m_TooltipHolders[i].parent != null)
                        m_TooltipHolders[i].RemoveFromHierarchy();
                }
                else
                {
                    if (m_TooltipHolders[i].parent == null)
                        m_FieldParents[i].Add(m_TooltipHolders[i]);
                }
            }
        }

        protected override void UpdateIndeterminate()
        {
            m_ColorField.indeterminate = indeterminate;
            for (int i = 0; i < 4; ++i)
                m_FloatFields[i].indeterminate = indeterminate;
        }

        public void OnValueChanged(ChangeEvent<Color> e)
        {
            OnValueChanged(false);
        }

        public void OnValueChanged(ChangeEvent<float> e)
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

        VisualElement[] m_FieldParents;
        VisualElement[] m_TooltipHolders;

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
                m_TooltipHolders[i].tooltip = m_Value[i].ToString();
            }
        }

        public override bool showsEverything { get { return true; } }
    }
}
