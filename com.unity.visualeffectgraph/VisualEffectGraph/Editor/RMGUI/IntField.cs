using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;


namespace UnityEditor.VFX.UIElements
{
    class IntField : ValueControl<int>, IValueChangeListener<int>
    {
        EditorTextField m_TextField;


        void CreateTextField()
        {
            m_TextField = new EditorTextField(30, false, false, '*');
            m_TextField.AddToClassList("textfield");
            m_TextField.OnTextChanged = OnTextChanged;
        }

        public IntField(string label) : base(label)
        {
            CreateTextField();
            m_Label.AddManipulator(new DragValueManipulator<int>(this, null));

            flexDirection = FlexDirection.Row;
            AddChild(m_TextField);
        }

        public IntField(VisualElement existingLabel) : base(existingLabel)
        {
            CreateTextField();
            AddChild(m_TextField);

            if (m_Label != null)
                m_Label.AddManipulator(new DragValueManipulator<int>(this, null));
        }

        void OnTextChanged(string str)
        {
            m_Value = 0;
            int.TryParse(m_TextField.text, out m_Value);

            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }

        int IValueChangeListener<int>.GetValue(object userData)
        {
            int newValue = 0;

            int.TryParse(m_TextField.text, out newValue);

            return newValue;
        }

        void IValueChangeListener<int>.SetValue(int value, object userData)
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
            m_TextField.text = m_Value.ToString("0.###");
        }
    }
}
