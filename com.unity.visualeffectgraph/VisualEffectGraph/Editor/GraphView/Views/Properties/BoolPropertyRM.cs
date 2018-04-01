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

namespace UnityEditor.VFX.UI
{
    class BoolPropertyRM : PropertyRM<bool>
    {
        public BoolPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_Toggle =  new Toggle(OnValueChanged);
            Add(m_Toggle);
        }

        void OnValueChanged()
        {
            m_Value = m_Toggle.on;
            NotifyValueChanged();
        }

        public override float GetPreferredControlWidth()
        {
            return 20;
        }

        public override void UpdateGUI(bool force)
        {
            m_Toggle.on = m_Value;
        }

        Toggle m_Toggle;

        protected override void UpdateEnabled()
        {
            m_Toggle.SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            m_Toggle.visible = !indeterminate;
        }

        public override bool showsEverything { get { return true; } }
    }
}
