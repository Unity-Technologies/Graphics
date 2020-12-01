using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

namespace UnityEditor.VFX.UI
{
    class VFXEnumValuePopup : VisualElement, INotifyValueChanged<long>
    {
        protected Label m_DropDownButton;
        TextElement m_ValueText;

        public string[] enumValues { get; set; }

        public VFXEnumValuePopup()
        {
            AddToClassList("unity-enum-field");
            AddToClassList("VFXEnumValuePopup");
            m_DropDownButton = new Label();
            m_DropDownButton.AddToClassList("unity-enum-field__input");
            m_DropDownButton.AddManipulator(new DownClickable(OnClick));
            Add(m_DropDownButton);
            m_ValueText = new TextElement();
            m_ValueText.AddToClassList("unity-enum-field__text");

            var icon = new VisualElement() { name = "icon" };
            icon.AddToClassList("unity-enum-field__arrow");
            m_DropDownButton.Add(m_ValueText);
            m_DropDownButton.Add(icon);
        }

        private void OnClick()
        {
            GenericMenu menu = new GenericMenu();

            for (long i = 0; i < enumValues.Length; ++i)
            {
                menu.AddItem(new GUIContent(enumValues[i]), i == m_Value, ChangeValue, i);
            }
            menu.DropDown(m_DropDownButton.worldBound);
        }

        void ChangeValue(object value)
        {
            SetValueAndNotify((long)value);
        }

        public long m_Value;

        public long value
        {
            get
            {
                return m_Value;
            }

            set
            {
                SetValueAndNotify(value);
            }
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
            m_Value = newValue;
            bool found = false;
            for (uint i = 0; i < enumValues.Length; ++i)
            {
                if (newValue == i)
                {
                    found = true;
                    m_ValueText.text = enumValues[i];
                    break;
                }
            }
            if (!found)
                m_ValueText.text = enumValues[enumValues.Length - 1];
        }
    }
}
