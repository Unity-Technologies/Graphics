using System;
using System.Collections.Generic;

using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXEnumValuePopup : VisualElement, INotifyValueChanged<long>
    {
        DropdownField m_DropDownButton;
        long m_Value;

        public IEnumerable<string> choices => m_DropDownButton.choices;

        public VFXEnumValuePopup(string label, List<string> values)
        {
            m_DropDownButton = new DropdownField(label);
            m_DropDownButton.choices = values;
            m_DropDownButton.value = values[0];
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

        public void SetValueAndNotify(long newValue)
        {
            if (!EqualityComparer<long>.Default.Equals(value, newValue))
            {
                using (ChangeEvent<long> evt = ChangeEvent<long>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    SetValueWithoutNotify(newValue);
                    SendEvent(evt);
                }
            }
        }

        public void SetValueWithoutNotify(long newValue)
        {
            if (newValue >= 0 && newValue < m_DropDownButton.choices.Count)
            {
                m_Value = newValue;
            }

            m_Value = Math.Clamp(newValue, 0, m_DropDownButton.choices.Count - 1);
        }
    }
}
