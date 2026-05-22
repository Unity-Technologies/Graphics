using UnityEngine.UIElements;


namespace UnityEditor.VFX.UI
{
    class VFXStringField : ValueControl<string>
    {
        protected TextField m_TextField;

        public TextField textfield => m_TextField;

        void CreateTextField()
        {
            m_TextField = new TextField(128, false, false, '*');
            m_TextField.AddToClassList("textfield");
            m_TextField.RegisterCallback<ChangeEvent<string>>(OnTextChanged);
            m_TextField.value = "";
        }

        protected VFXStringField(string label) : base(label)
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Row;
            Add(m_TextField);
        }

        public VFXStringField(Label existingLabel) : base(existingLabel)
        {
            CreateTextField();
            Add(m_TextField);
        }

        void OnTextChanged(ChangeEvent<string> e)
        {
            if (m_Value != m_TextField.text)
            {
                m_Value = m_TextField.text;
                if (OnValueChanged != null)
                {
                    OnValueChanged();
                }
            }
        }

        bool m_Indeterminate;

        public bool indeterminate
        {
            get
            {
                return m_Indeterminate;
            }
            set
            {
                m_Indeterminate = value;
                ValueToGUI(true);
            }
        }

        protected override void ValueToGUI(bool force)
        {
            if (indeterminate)
            {
                m_TextField.value = "_";
                m_TextField.SetEnabled(false);
            }
            m_TextField.value = m_Value != null ? m_Value : "";
        }
    }
}
