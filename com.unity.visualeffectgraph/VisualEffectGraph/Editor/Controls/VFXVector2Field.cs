using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using System.Collections.Generic;

using FloatField = UnityEditor.VFX.UIElements.VFXLabeledField<UnityEditor.VFX.UIElements.VFXFloatField, float>;

namespace UnityEditor.VFX.UIElements
{
    class VFXVector2Field : VFXControl<Vector2>
    {
        FloatField m_X;
        FloatField m_Y;
        void CreateTextField()
        {
            m_X = new FloatField("X");
            m_Y = new FloatField("Y");

            m_X.label.AddToClassList("first");
            m_X.control.AddToClassList("fieldContainer");
            m_Y.control.AddToClassList("fieldContainer");
            m_X.AddToClassList("fieldContainer");
            m_Y.AddToClassList("fieldContainer");

            m_X.RegisterCallback<ChangeEvent<float>>(OnXValueChanged);
            m_Y.RegisterCallback<ChangeEvent<float>>(OnYValueChanged);
        }

        void OnXValueChanged(ChangeEvent<float> e)
        {
            Vector2 newValue = value;
            newValue.x = (float)m_X.value;
            SetValueAndNotify(newValue);
        }

        void OnYValueChanged(ChangeEvent<float> e)
        {
            Vector2 newValue = value;
            newValue.y = (float)m_Y.value;
            SetValueAndNotify(newValue);
        }

        public VFXVector2Field()
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
