using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.VFX.UIElements
{
    class LabeledField<T, U> : VisualElement, INotifyValueChanged<U> where T : VisualElement, INotifyValueChanged<U>, new()
    {
        protected VisualElement m_Label;
        protected T m_Control;
        public LabeledField(VisualElement existingLabel)
        {
            m_Label = existingLabel;

            CreateControl();
            SetupLabel();
        }

        void SetupLabel()
        {
            if (typeof(U) == typeof(double))
            {
                m_Label.AddManipulator(new UIDragValueManipulator<double>((INotifyValueChanged<double> )m_Control));
            }/*
            else if (typeof(U) == typeof(float))
            {
                m_Label.AddManipulator(new UIDragValueManipulator<float>(m_Control));
            }*/
            else if (typeof(U) == typeof(long))
            {
                m_Label.AddManipulator(new UIDragValueManipulator<long>((INotifyValueChanged<long>)m_Control));
            }/*
            else if (typeof(U) == typeof(int))
            {
                m_Label.AddManipulator(new UIDragValueManipulator<int>(m_Control));
            }*/
        }

        void CreateControl()
        {
            m_Control = new T();
            Add(m_Control);
        }

        public T control
        {
            get { return m_Control; }
        }

        LabeledField(string label)
        {
            if (!string.IsNullOrEmpty(label))
            {
                m_Label = new VisualElement() { text = label };
                m_Label.AddToClassList("label");

                Add(m_Label);
            }
            style.flexDirection = FlexDirection.Row;

            CreateControl();
            SetupLabel();
        }

        public void OnValueChanged(EventCallback<ChangeEvent<U>> callback)
        {
            (m_Control as INotifyValueChanged<U> ).OnValueChanged(callback);
        }

        public void SetValueAndNotify(U newValue)
        {
            (m_Control as INotifyValueChanged<U>).SetValueAndNotify(newValue);
        }

        public U value
        {
            get { return m_Control.value; }
            set { m_Control.value = value; }
        }
    }

    abstract class ValueControl<T> : VisualElement
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
            ValueToGUI();
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

        protected abstract void ValueToGUI();
    }

    class FloatField : ValueControl<float>, IValueChangeListener<float>
    {
        TextField m_TextField;


        void CreateFields()
        {
            m_TextField = new TextField(30, false, false, '*');
            m_TextField.AddToClassList("textfield");
            m_TextField.dynamicUpdate = true;
            m_TextField.RegisterCallback<ChangeEvent<string>>(OnTextChanged);
            m_TextField.RegisterCallback<BlurEvent>(OnLostFocus);
        }

        public FloatField(string label) : base(label)
        {
            CreateFields();
            m_Label.AddManipulator(new DragValueManipulator<float>(this, null));
            Add(m_TextField);

            m_Multiplier = 1.0f;
        }

        void OnLostFocus(BlurEvent evt)
        {
            // Since we block the control updates when we have the focus we must update once when we loose focus.
            ValueToGUI();
        }

        void OnTextChanged(ChangeEvent<string> e)
        {
            m_Value = 0;
            float.TryParse(m_TextField.text, out m_Value);
            m_Value *= m_Multiplier;

            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }

        public FloatField(VisualElement existingLabel) : base(existingLabel)
        {
            CreateFields();
            Add(m_TextField);

            m_Label.AddManipulator(new DragValueManipulator<float>(this, null));

            m_Multiplier = 1.0f;
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
            if (!m_TextField.hasFocus)
            {
                float value = m_Value / m_Multiplier;
                m_TextField.text = value.ToString("0.###");
            }
        }
    }
}
