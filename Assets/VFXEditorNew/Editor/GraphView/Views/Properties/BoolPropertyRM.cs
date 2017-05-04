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
        public BoolPropertyRM(IPropertyRMProvider presenter) : base(presenter)
        {
            m_Toggle =  new Toggle(OnValueChanged);
            AddChild(m_Toggle);


            m_Toggle.enabled = enabled;
        }

        void OnValueChanged()
        {
            m_Value = m_Toggle.on;
            NotifyValueChanged();
        }

        public override void UpdateGUI()
        {
            m_Toggle.on = m_Value;
        }

        public override bool enabled
        {
            set
            {
                base.enabled = value;

                if (m_Toggle != null)
                    m_Toggle.enabled = value;
            }
        }

        Toggle m_Toggle;
    }
}
