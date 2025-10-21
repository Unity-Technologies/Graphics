using System;
using System.Collections.Generic;

using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXEnumValuePopup : VisualElement, INotifyValueChanged<long>
    {
        readonly DropdownField m_DropDownButton;
        long m_Value;

        public IEnumerable<string> choices => m_DropDownButton.choices;

        public VFXEnumValuePopup(string label, List<string> values)
        {
            m_DropDownButton = new DropdownField(label);
            m_DropDownButton.choices = values;
            m_DropDownButton.value = values.Count > 0 ? values[0] : string.Empty;
            m_DropDownButton.RegisterCallback<ChangeEvent<string>>(OnValueChanged);
            Add(m_DropDownButton);
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            SetValueAndNotify(m_DropDownButton.choices.IndexOf(evt.newValue));
        }

        public long value
        {
            get => m_Value;
            set => SetValueAndNotify(value);
        }

        private void SetValueAndNotify(long newValue)
        {
            if (!EqualityComparer<long>.Default.Equals(value, newValue))
            {
                using var evt = ChangeEvent<long>.GetPooled(value, newValue);
                evt.target = this;
                SetValueWithoutNotify(newValue);
                m_DropDownButton.value = m_DropDownButton.choices[(int)m_Value];
                SendEvent(evt);
            }
        }

        public void SetValueWithoutNotify(long newValue)
        {
            m_Value = Math.Clamp(newValue, 0, m_DropDownButton.choices.Count - 1);
        }
    }
}
