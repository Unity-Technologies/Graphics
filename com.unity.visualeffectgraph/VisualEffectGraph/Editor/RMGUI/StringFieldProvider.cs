using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using System;

namespace UnityEditor.VFX.UIElements
{
    class StringFieldProvider : ValueControl<string>, IValueChangeListener<string>
    {
        Button m_DropDownButton;
        Func<string[]> m_fnStringProvider;

        void CreateButton()
        {
            m_DropDownButton = new Button();
            m_DropDownButton.AddManipulator(new DownClickable(OnClick));
        }

        void OnClick()
        {
            var menu = new GenericMenu();
            var allString = m_fnStringProvider();
            foreach (var val in allString)
            {
                menu.AddItem(new GUIContent(val), val == m_Value, ChangeValue, val);
            }
            menu.DropDown(m_DropDownButton.globalBound);
        }

        void ChangeValue(object val)
        {
            SetValue((string)val);
            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }

        public StringFieldProvider(string label, Func<string[]> stringProvider) : base(label)
        {
            m_fnStringProvider = stringProvider;
            CreateButton();
            flexDirection = FlexDirection.Row;
            AddChild(m_DropDownButton);
        }

        public StringFieldProvider(VisualElement existingLabel, Func<string[]> stringProvider) : base(existingLabel)
        {
            m_fnStringProvider = stringProvider;
            CreateButton();
            AddChild(m_DropDownButton);
        }

        protected override void ValueToGUI()
        {
            m_DropDownButton.text = m_Value;
        }

        string IValueChangeListener<string>.GetValue(object userData)
        {
            return m_DropDownButton.text;
        }

        void IValueChangeListener<string>.SetValue(string value, object userData)
        {
            m_Value = value;

            ValueToGUI();

            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }
    }
}
