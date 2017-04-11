using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.VFX.UIElements
{
    abstract class ValueControl<T> : VisualContainer
    {
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

        public System.Action onValueChanged;


        protected abstract void ValueToGUI();

    }

    class FloatField : ValueControl<float>, IValueChangeListener<float>
    {
        EditorTextField m_TextField;
        VisualElement m_Label;


        void CreateTextField()
        {
            m_TextField = new EditorTextField(30,false,false,'*');
            m_TextField.AddToClassList("textfield");
            m_TextField.onTextChanged = OnTextChanged;
            m_TextField.useStylePainter = true;
        }

        public FloatField(string label) 
        {
            CreateTextField();            

            if( !string.IsNullOrEmpty(label) )
            {
                m_Label = new VisualElement(){text = label};
                m_Label.AddToClassList("label");
                m_Label.AddManipulator(new DragValueManipulator<float>(this,null));
                AddChild(m_Label);
            }

            flexDirection = FlexDirection.Row;
            AddChild(m_TextField);
        }

        void OnTextChanged()
        {
            m_Value = 0;
            float.TryParse(m_TextField.text, out m_Value);

            if(onValueChanged != null)
            {
                onValueChanged();
            }
        }

        public FloatField(VisualElement existingLabel)
        {
            CreateTextField();
            AddChild(m_TextField);

            m_Label = existingLabel;

            m_Label.AddManipulator(new DragValueManipulator<float>(this,null));

            //m_TextField.positionType = PositionType.Absolute;
            //m_TextField.positionBottom = m_TextField.positionTop = m_TextField.positionLeft = m_TextField.positionRight = 0;
        }


        float IValueChangeListener<float>.GetValue(object userData)
        {
            float newValue = 0;

            float.TryParse(m_TextField.text,out newValue);

            return newValue;
        }

        void IValueChangeListener<float>.SetValue(float value,object userData)
        {
            m_Value = value;
            ValueToGUI();

            if(onValueChanged != null)
            {
                onValueChanged();
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