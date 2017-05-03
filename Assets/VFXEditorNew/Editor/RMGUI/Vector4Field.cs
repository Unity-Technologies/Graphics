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

            m_X.onValueChanged = OnXValueChanged;
            m_Y.onValueChanged = OnYValueChanged;
            m_Z.onValueChanged = OnZValueChanged;
            m_W.onValueChanged = OnWValueChanged;
        }

        void OnXValueChanged()
        {
            m_Value.x = m_X.GetValue();
            if (onValueChanged != null)
            {
                onValueChanged();
            }
        }

        void OnYValueChanged()
        {
            m_Value.y = m_Y.GetValue();
            if (onValueChanged != null)
            {
                onValueChanged();
            }
        }

        void OnZValueChanged()
        {
            m_Value.z = m_Z.GetValue();
            if (onValueChanged != null)
            {
                onValueChanged();
            }
        }

        void OnWValueChanged()
        {
            m_Value.w = m_W.GetValue();
            if (onValueChanged != null)
            {
                onValueChanged();
            }
        }

        public Vector4Field(string label) : base(label)
        {
            CreateTextField();

            flexDirection = FlexDirection.Row;
            m_Label = new VisualElement { text = label };
            AddChild(m_Label);
            AddChild(m_X);
            AddChild(m_Y);
            AddChild(m_Z);
            AddChild(m_W);
        }

        public Vector4Field(VisualElement existingLabel) : base(existingLabel)
        {
            CreateTextField();
            AddChild(m_X);
            AddChild(m_Y);
            AddChild(m_Z);
            AddChild(m_W);

            m_Label = existingLabel;
        }

        protected override void ValueToGUI()
        {
            m_X.SetValue(m_Value.x);
            m_Y.SetValue(m_Value.y);
            m_Z.SetValue(m_Value.z);
            m_W.SetValue(m_Value.w);
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
                    m_Z.enabled = value;
                    m_W.enabled = value;
                }
            }
        }
    }
}
