using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.VFX.UIElements
{
    class Vector2Field : ValueControl<Vector2>
    {
        FloatField m_X;
        FloatField m_Y;


        void CreateTextField()
        {
            m_X = new FloatField("X");
            m_Y = new FloatField("Y");

            m_X.OnValueChanged = OnXValueChanged;
            m_Y.OnValueChanged = OnYValueChanged;
        }

        void OnXValueChanged()
        {
            m_Value.x = m_X.GetValue();
            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }

        void OnYValueChanged()
        {
            m_Value.y = m_Y.GetValue();
            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }

        public Vector2Field(string label) : base(label)
        {
            CreateTextField();

            flexDirection = FlexDirection.Row;
            m_Label = new VisualElement { text = label };
            AddChild(m_Label);
            AddChild(m_X);
            AddChild(m_Y);
        }

        public Vector2Field(VisualElement existingLabel) : base(existingLabel)
        {
            CreateTextField();
            AddChild(m_X);
            AddChild(m_Y);

            m_Label = existingLabel;
        }

        protected override void ValueToGUI()
        {
            m_X.SetValue(m_Value.x);
            m_Y.SetValue(m_Value.y);
        }

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                if (m_X != null)
                {
                    m_X.enabled = value;
                    m_Y.enabled = value;
                }
            }
        }
    }
}
