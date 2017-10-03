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
using ObjectField = UnityEditor.VFX.UIElements.ObjectField;

namespace UnityEditor.VFX.UI
{
    class ObjectPropertyRM : PropertyRM<Object>
    {
        public ObjectPropertyRM(IPropertyRMProvider presenter, float labelWidth) : base(presenter, labelWidth)
        {
            m_ObjectField = new ObjectField(m_Label);
            m_ObjectField.editedType = presenter.anchorType;
            m_ObjectField.OnValueChanged = OnValueChanged;

            m_ObjectField.style.flex = 1;

            Add(m_ObjectField);
        }

        public void OnValueChanged()
        {
            Object newValue = m_ObjectField.GetValue();
            m_Value = newValue;
            NotifyValueChanged();
        }

        ObjectField m_ObjectField;

        protected override void UpdateEnabled()
        {
            m_ObjectField.SetEnabled(propertyEnabled);
        }

        public override void UpdateGUI()
        {
            m_ObjectField.SetValue(m_Value);
        }

        public override bool showsEverything { get { return true; } }
    }
}
