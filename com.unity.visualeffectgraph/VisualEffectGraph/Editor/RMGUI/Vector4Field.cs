using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.VFX.UIElements
{
    class Vector4Field : VFXControl<Vector4>
    {
        LabeledField<DoubleField, double> m_X;
        LabeledField<DoubleField, double> m_Y;
        LabeledField<DoubleField, double> m_Z;
        LabeledField<DoubleField, double> m_W;


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
            m_X = new LabeledField<DoubleField, double>("X");
            m_Y = new LabeledField<DoubleField, double>("Y");
            m_Z = new LabeledField<DoubleField, double>("Z");
            m_W = new LabeledField<DoubleField, double>("W");

            m_X.control.AddToClassList("fieldContainer");
            m_Y.control.AddToClassList("fieldContainer");
            m_Z.control.AddToClassList("fieldContainer");
            m_W.control.AddToClassList("fieldContainer");
            m_X.AddToClassList("fieldContainer");
            m_Y.AddToClassList("fieldContainer");
            m_Z.AddToClassList("fieldContainer");
            m_W.AddToClassList("fieldContainer");

            m_X.RegisterCallback<ChangeEvent<double>>(OnXValueChanged);
            m_Y.RegisterCallback<ChangeEvent<double>>(OnYValueChanged);
            m_Z.RegisterCallback<ChangeEvent<double>>(OnZValueChanged);
            m_W.RegisterCallback<ChangeEvent<double>>(OnWValueChanged);
        }

        void OnXValueChanged(ChangeEvent<double> e)
        {
            Vector4 newValue = value;
            newValue.x = (float)m_X.value;
            SetValueAndNotify(newValue);
        }

        void OnYValueChanged(ChangeEvent<double> e)
        {
            Vector4 newValue = value;
            newValue.y = (float)m_Y.value;
            SetValueAndNotify(newValue);
        }

        void OnZValueChanged(ChangeEvent<double> e)
        {
            Vector4 newValue = value;
            newValue.z = (float)m_Z.value;
            SetValueAndNotify(newValue);
        }

        void OnWValueChanged(ChangeEvent<double> e)
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
