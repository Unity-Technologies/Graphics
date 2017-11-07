using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.VFX.UIElements
{
    class Vector3Field : VFXControl<Vector3>
    {
        LabeledField<DoubleField, double> m_X;
        LabeledField<DoubleField, double> m_Y;
        LabeledField<DoubleField, double> m_Z;

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
            }
        }
        void CreateTextField()
        {
            m_X = new LabeledField<DoubleField, double>("X");
            m_Y = new LabeledField<DoubleField, double>("Y");
            m_Z = new LabeledField<DoubleField, double>("Z");

            m_X.control.AddToClassList("fieldContainer");
            m_Y.control.AddToClassList("fieldContainer");
            m_Z.control.AddToClassList("fieldContainer");
            m_X.AddToClassList("fieldContainer");
            m_Y.AddToClassList("fieldContainer");
            m_Z.AddToClassList("fieldContainer");

            m_X.RegisterCallback<ChangeEvent<double>>(OnXValueChanged);
            m_Y.RegisterCallback<ChangeEvent<double>>(OnYValueChanged);
            m_Z.RegisterCallback<ChangeEvent<double>>(OnZValueChanged);
        }

        void OnXValueChanged(ChangeEvent<double> e)
        {
            Vector3 newValue = value;
            newValue.x = (float)m_X.value;
            SetValueAndNotify(newValue);
        }

        void OnYValueChanged(ChangeEvent<double> e)
        {
            Vector3 newValue = value;
            newValue.y = (float)m_Y.value;
            SetValueAndNotify(newValue);
        }

        void OnZValueChanged(ChangeEvent<double> e)
        {
            Vector3 newValue = value;
            newValue.z = (float)m_Z.value;
            SetValueAndNotify(newValue);
        }

        public Vector3Field()
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Row;
            Add(m_X);
            Add(m_Y);
            Add(m_Z);
        }

        protected override void ValueToGUI()
        {
            m_X.value = value.x;
            m_Y.value = value.y;
            m_Z.value = value.z;
        }
    }
}
