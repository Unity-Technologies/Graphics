using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.VFX.UIElements
{
    class Vector4Field : ValueControl<Vector4>
    {
        FloatField m_X;
        FloatField m_Y;
        FloatField m_Z;
        FloatField m_W;


        void CreateTextField()
        {
            m_X = new FloatField("X");
            m_Y = new FloatField("Y");
            m_Z = new FloatField("Z");
            m_W = new FloatField("W");

            m_X.OnValueChanged = OnXValueChanged;
            m_Y.OnValueChanged = OnYValueChanged;
            m_Z.OnValueChanged = OnZValueChanged;
            m_W.OnValueChanged = OnWValueChanged;
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

        void OnZValueChanged()
        {
            m_Value.z = m_Z.GetValue();
            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }

        void OnWValueChanged()
        {
            m_Value.w = m_W.GetValue();
            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }

        public Vector4Field(string label) : base(label)
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Row;
            m_Label = new VisualElement { text = label };
            Add(m_Label);
            Add(m_X);
            Add(m_Y);
            Add(m_Z);
            Add(m_W);
        }

        public Vector4Field(VisualElement existingLabel) : base(existingLabel)
        {
            CreateTextField();
            Add(m_X);
            Add(m_Y);
            Add(m_Z);
            Add(m_W);

            m_Label = existingLabel;
        }

        protected override void ValueToGUI()
        {
            m_X.SetValue(m_Value.x);
            m_Y.SetValue(m_Value.y);
            m_Z.SetValue(m_Value.z);
            m_W.SetValue(m_Value.w);
        }
    }
}
