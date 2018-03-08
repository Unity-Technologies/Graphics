using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

using System.Collections.Generic;

namespace UnityEditor.VFX.UIElements
{
    class VFXFlipBookField : VFXControl<FlipBook>
    {
        VFXLabeledField<IntegerField, long> m_X;
        VFXLabeledField<IntegerField, long> m_Y;

        void CreateTextField()
        {
            m_X = new VFXLabeledField<IntegerField, long>("X");
            m_Y = new VFXLabeledField<IntegerField, long>("Y");

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

        public VFXFlipBookField()
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Row;
            Add(m_X);
            Add(m_Y);
        }

        protected override void ValueToGUI(bool force)
        {
            if (!m_X.control.hasFocus || force)
                m_X.value = value.x;

            if (!m_Y.control.hasFocus || force)
                m_Y.value = value.y;
        }
    }
}
