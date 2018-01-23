using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.VFX.UIElements
{
    class VFXVector4Field : VFXControl<Vector4>
    {
        VFXLabeledField<FloatField, float> m_X;
        VFXLabeledField<FloatField, float> m_Y;
        VFXLabeledField<FloatField, float> m_Z;
        VFXLabeledField<FloatField, float> m_W;

        void CreateTextField()
        {
            m_X = new VFXLabeledField<FloatField, float>("X");
            m_Y = new VFXLabeledField<FloatField, float>("Y");
            m_Z = new VFXLabeledField<FloatField, float>("Z");
            m_W = new VFXLabeledField<FloatField, float>("W");

            m_X.label.AddToClassList("first");
            m_X.control.AddToClassList("fieldContainer");
            m_Y.control.AddToClassList("fieldContainer");
            m_Z.control.AddToClassList("fieldContainer");
            m_W.control.AddToClassList("fieldContainer");
            m_X.AddToClassList("fieldContainer");
            m_Y.AddToClassList("fieldContainer");
            m_Z.AddToClassList("fieldContainer");
            m_W.AddToClassList("fieldContainer");

            m_X.RegisterCallback<ChangeEvent<float>>(OnXValueChanged);
            m_Y.RegisterCallback<ChangeEvent<float>>(OnYValueChanged);
            m_Z.RegisterCallback<ChangeEvent<float>>(OnZValueChanged);
            m_W.RegisterCallback<ChangeEvent<float>>(OnWValueChanged);
        }

        void OnXValueChanged(ChangeEvent<float> e)
        {
            Vector4 newValue = value;
            newValue.x = (float)m_X.value;
            SetValueAndNotify(newValue);
        }

        void OnYValueChanged(ChangeEvent<float> e)
        {
            Vector4 newValue = value;
            newValue.y = (float)m_Y.value;
            SetValueAndNotify(newValue);
        }

        void OnZValueChanged(ChangeEvent<float> e)
        {
            Vector4 newValue = value;
            newValue.z = (float)m_Z.value;
            SetValueAndNotify(newValue);
        }

        void OnWValueChanged(ChangeEvent<float> e)
        {
            Vector4 newValue = value;
            newValue.w = (float)m_W.value;
            SetValueAndNotify(newValue);
        }

        public VFXVector4Field()
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Row;
            Add(m_X);
            Add(m_Y);
            Add(m_Z);
            Add(m_W);
        }

        protected override void ValueToGUI()
        {
            if (!m_X.control.hasFocus)
                m_X.value = value.x;

            if (!m_Y.control.hasFocus)
                m_Y.value = value.y;

            if (!m_Z.control.hasFocus)
                m_Z.value = value.z;

            if (!m_W.control.hasFocus)
                m_W.value = value.w;
        }
    }
}
