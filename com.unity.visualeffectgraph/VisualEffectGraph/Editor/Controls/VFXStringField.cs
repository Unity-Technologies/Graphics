using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;


namespace UnityEditor.VFX.UIElements
{
    class VFXStringField : ValueControl<string>
    {
        protected TextField m_TextField;

        void CreateTextField()
        {
            m_TextField = new TextField(30, false, false, '*');
            m_TextField.AddToClassList("textfield");
            m_TextField.RegisterCallback<ChangeEvent<string>>(OnTextChanged);
        }

        public VFXStringField(string label) : base(label)
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
            m_Value = m_TextField.text;
            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }

        protected override void ValueToGUI()
        {
            m_TextField.text = m_Value;
        }
    }
}
