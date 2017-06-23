using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.VFX.UIElements
{
    abstract class ValueControl<T> : VisualContainer
    {
        protected VisualElement m_Label;

        protected ValueControl(VisualElement existingLabel)
        {
            m_Label = existingLabel;
        }

        protected ValueControl(string label)
        {
            if (!string.IsNullOrEmpty(label))
            {
                m_Label = new VisualElement() { text = label };
                m_Label.AddToClassList("label");

                AddChild(m_Label);
            }
            flexDirection = FlexDirection.Row;
        }

        public T GetValue()
        {
            return m_Value;
        }

        public void SetValue(T value)
        {
            m_Value = value;
            ValueToGUI();
        }

        protected T m_Value;

        public System.Action OnValueChanged;

        protected abstract void ValueToGUI();
    }

    class FloatField : ValueControl<float>, IValueChangeListener<float>
    {
        EditorTextField m_TextField;


        void CreateFields()
        {
            m_TextField = new EditorTextField(30, false, false, '*');
            m_TextField.AddToClassList("textfield");
            m_TextField.OnTextChanged = OnTextChanged;
        }

        public FloatField(string label) : base(label)
        {
            CreateFields();
            m_Label.AddManipulator(new DragValueManipulator<float>(this, null));
            AddChild(m_TextField);
        }

        void OnTextChanged(string str)
        {
            m_Value = 0;
            float.TryParse(m_TextField.text, out m_Value);

            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }

        public FloatField(VisualElement existingLabel) : base(existingLabel)
        {
            CreateFields();
            AddChild(m_TextField);

            m_Label.AddManipulator(new DragValueManipulator<float>(this, null));
        }

        float IValueChangeListener<float>.GetValue(object userData)
        {
            float newValue = 0;

            float.TryParse(m_TextField.text, out newValue);

            return newValue;
        }

        void IValueChangeListener<float>.SetValue(float value, object userData)
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

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                if (m_TextField != null)
                    m_TextField.enabled = value;
            }
        }
    }
}
