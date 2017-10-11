using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;


namespace UnityEditor.VFX.UIElements
{
    class StringField : ValueControl<string>, IValueChangeListener<string>
    {
        TextField m_TextField;

        void CreateTextField()
        {
            m_TextField = new TextField(30, false, false, '*');
            m_TextField.AddToClassList("textfield");
            m_TextField.RegisterCallback<ChangeEvent<string>>(OnTextChanged);
        }

        public StringField(string label) : base(label)
        {
            CreateTextField();
            m_Label.AddManipulator(new DragValueManipulator<string>(this, null));

            style.flexDirection = FlexDirection.Row;
            Add(m_TextField);
        }

        public StringField(VisualElement existingLabel) : base(existingLabel)
        {
            CreateTextField();
            Add(m_TextField);

            if (m_Label != null)
                m_Label.AddManipulator(new DragValueManipulator<string>(this, null));
        }

        void OnTextChanged(ChangeEvent<string> e)
        {
            m_Value = m_TextField.text;
            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }

        string IValueChangeListener<string>.GetValue(object userData)
        {
            return m_TextField.text;
        }

        void IValueChangeListener<string>.SetValue(string value, object userData)
        {
            m_Value = value;
            ValueToGUI();

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
