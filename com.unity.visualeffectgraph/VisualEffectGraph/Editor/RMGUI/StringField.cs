using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;


namespace UnityEditor.VFX.UIElements
{
    class StringField : ValueControl<string>, IValueChangeListener<string>
    {
        EditorTextField m_TextField;

        void CreateTextField()
        {
            m_TextField = new EditorTextField(30, false, false, '*');
            m_TextField.AddToClassList("textfield");
            m_TextField.OnTextChanged = OnTextChanged;
        }

        public StringField(string label) : base(label)
        {
            CreateTextField();
            m_Label.AddManipulator(new DragValueManipulator<string>(this, null));

            flexDirection = FlexDirection.Row;
            AddChild(m_TextField);
        }

        public StringField(VisualElement existingLabel) : base(existingLabel)
        {
            CreateTextField();
            AddChild(m_TextField);

            if (m_Label != null)
                m_Label.AddManipulator(new DragValueManipulator<string>(this, null));
        }

        void OnTextChanged(string str)
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
