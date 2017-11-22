using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.VFX.UIElements
{
    class Vector4Field : VFXControl<Vector4>
    {
        LabeledField<FloatField, float> m_X;
        LabeledField<FloatField, float> m_Y;
        LabeledField<FloatField, float> m_Z;
        LabeledField<FloatField, float> m_W;


        public bool dynamicUpdate
        {
            get
            {
                return m_X.control.dynamicUpdate;
            }
            set
            {
                m_X.control.dynamicUpdate = value;
                m_Y.control.dynamicUpdate = value;
                m_Z.control.dynamicUpdate = value;
                m_W.control.dynamicUpdate = value;
            }
        }

        void CreateTextField()
        {
            m_X = new LabeledField<FloatField, float>("X");
            m_Y = new LabeledField<FloatField, float>("Y");
            m_Z = new LabeledField<FloatField, float>("Z");
            m_W = new LabeledField<FloatField, float>("W");

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

        public Vector4Field()
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
            m_X.value = value.x;
            m_Y.value = value.y;
            m_Z.value = value.z;
            m_W.value = value.w;
        }
    }
}
