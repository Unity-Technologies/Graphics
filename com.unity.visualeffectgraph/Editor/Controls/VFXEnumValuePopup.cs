using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

namespace UnityEditor.VFX.UI
{
    class VFXEnumValuePopup<T> : VisualElement, INotifyValueChanged<T>
    {
        protected Label m_DropDownButton;
        TextElement m_ValueText;

        public VFXEnumValue[] enumValues { get; set; }

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

            foreach (var val in enumValues)
            {
                object value = val.value.Get();
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(val.name)), object.Equals(value, m_Value), ChangeValue, value);
            }
            menu.DropDown(m_DropDownButton.worldBound);
        }

        void ChangeValue(object value)
        {
            SetValueAndNotify((T)value);
        }

        public T m_Value;

        public T value
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

        public void SetValueAndNotify(T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(value, newValue))
            {
                using (ChangeEvent<T> evt = ChangeEvent<T>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    SetValueWithoutNotify(newValue);
                    SendEvent(evt);
                }
            }
        }

        public void SetValueWithoutNotify(T newValue)
        {
            m_Value = newValue;
            bool found = false;
            foreach(var val in enumValues)
            {
                if( object.Equals(newValue,val.value.Get()))
                {
                    found = true;
                    m_ValueText.text = val.name;
                    break;
                }
            }
            if (!found)
                m_ValueText.text = "-";
        }
    }
}
