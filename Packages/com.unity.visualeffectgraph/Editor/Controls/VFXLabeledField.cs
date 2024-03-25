using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    abstract class ValueControl<T> : VisualElement
    {
        protected Label m_Label;

        protected ValueControl(Label existingLabel)
        {
            m_Label = existingLabel;
            if (m_Label != null)
            {
                m_Label.AddToClassList("label");
                Add(m_Label);
            }
        }

        protected ValueControl(string label)
        {
            if (!string.IsNullOrEmpty(label))
            {
                m_Label = new Label() { text = label };
                m_Label.AddToClassList("label");

                Add(m_Label);
            }
            style.flexDirection = FlexDirection.Row;
        }

        public T GetValue()
        {
            return m_Value;
        }

        public void SetValue(T value)
        {
            m_Value = value;
            ValueToGUI(false);
        }

        public T value
        {
            get { return GetValue(); }
            set { SetValue(value); }
        }

        public void SetMultiplier(T multiplier)
        {
            m_Multiplier = multiplier;
        }

        protected T m_Value;
        protected T m_Multiplier;

        public System.Action OnValueChanged;

        protected abstract void ValueToGUI(bool force);
    }
}
