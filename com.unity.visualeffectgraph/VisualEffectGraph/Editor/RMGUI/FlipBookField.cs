using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

using System.Collections.Generic;

namespace UnityEditor.VFX.UIElements
{
    class FlipBookField : VFXControl<FlipBook>
    {
        LabeledField<IntegerField, long> m_X;
        LabeledField<IntegerField, long> m_Y;

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
            }
        }
        void CreateTextField()
        {
            m_X = new LabeledField<IntegerField, long>("X");
            m_Y = new LabeledField<IntegerField, long>("Y");

            m_X.control.AddToClassList("fieldContainer");
            m_Y.control.AddToClassList("fieldContainer");
            m_X.AddToClassList("fieldContainer");
            m_Y.AddToClassList("fieldContainer");

            m_X.RegisterCallback<ChangeEvent<long>>(OnXValueChanged);
            m_Y.RegisterCallback<ChangeEvent<long>>(OnYValueChanged);
        }

        void OnXValueChanged(ChangeEvent<long> e)
        {
            FlipBook newValue = value;
            newValue.x = (int)m_X.value;
            SetValueAndNotify(newValue);
        }

        void OnYValueChanged(ChangeEvent<long> e)
        {
            FlipBook newValue = value;
            newValue.y = (int)m_Y.value;
            SetValueAndNotify(newValue);
        }

        public FlipBookField()
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Row;
            Add(m_X);
            Add(m_Y);
        }

        protected override void ValueToGUI()
        {
            m_X.value = value.x;
            m_Y.value = value.y;
        }
    }
}
