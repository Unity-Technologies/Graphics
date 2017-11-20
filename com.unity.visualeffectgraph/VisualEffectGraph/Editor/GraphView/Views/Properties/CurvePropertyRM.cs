using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;

using CurveField = UnityEditor.VFX.UIElements.LabeledField<UnityEditor.Experimental.UIElements.CurveField, UnityEngine.AnimationCurve>;

namespace UnityEditor.VFX.UI
{
    class CurvePropertyRM : PropertyRM<AnimationCurve>
    {
        public CurvePropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
            m_CurveField = new CurveField(m_Label);
            m_CurveField.RegisterCallback<ChangeEvent<AnimationCurve>>(OnValueChanged);

            m_CurveField.style.flexDirection = FlexDirection.Column;
            m_CurveField.style.alignItems = Align.Stretch;
            m_CurveField.style.flex = 1;

            Add(m_CurveField);
        }

        public override float GetPreferredControlWidth()
        {
            return 160;
        }

        public void OnValueChanged(ChangeEvent<AnimationCurve> e)
        {
            AnimationCurve newValue = m_CurveField.value;
            m_Value = newValue;
            NotifyValueChanged();
        }

        CurveField m_CurveField;

        protected override void UpdateEnabled()
        {
            m_CurveField.SetEnabled(propertyEnabled);
        }

        public override void UpdateGUI()
        {
            m_CurveField.value = m_Value;
        }

        public override bool showsEverything { get { return true; } }
    }
}
