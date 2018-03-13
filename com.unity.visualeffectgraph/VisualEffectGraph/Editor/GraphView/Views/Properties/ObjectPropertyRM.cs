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

#if true
using ObjectField = UnityEditor.VFX.UIElements.VFXLabeledField<UnityEditor.Experimental.UIElements.ObjectField, UnityEngine.Object>;

namespace UnityEditor.VFX.UI
{
    class ObjectPropertyRM : PropertyRM<Object>
    {
        public ObjectPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_ObjectField = new ObjectField(m_Label);
            m_ObjectField.control.objectType = controller.portType;

            m_ObjectField.RegisterCallback<ChangeEvent<Object>>(OnValueChanged);

            m_ObjectField.style.flex = 1;

            Add(m_ObjectField);
        }

        public override float GetPreferredControlWidth()
        {
            return 120;
        }

        public void OnValueChanged(ChangeEvent<Object> onObjectChanged)
        {
            Object newValue = m_ObjectField.value;
            m_Value = newValue;
            NotifyValueChanged();
        }

        ObjectField m_ObjectField;

        protected override void UpdateEnabled()
        {
            m_ObjectField.SetEnabled(propertyEnabled);
        }

        public override void UpdateGUI(bool force)
        {
            m_ObjectField.value = m_Value;
        }

        public override bool showsEverything { get { return true; } }
    }
}
#else
using ObjectField = UnityEditor.VFX.UIElements.VFXObjectField;

namespace UnityEditor.VFX.UI
{
    class ObjectPropertyRM : PropertyRM<Object>
    {
        public ObjectPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_ObjectField = new ObjectField(m_Label);
            m_ObjectField.editedType = controller.portType;
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

#endif
