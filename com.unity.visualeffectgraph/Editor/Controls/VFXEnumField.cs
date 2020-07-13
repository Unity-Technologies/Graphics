using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.VFX.UI;


namespace UnityEditor.VFX.UIElements
{
    class VFXEnumField : ValueControl<int>
    {
        Label m_DropDownButton;
        TextElement m_ValueText;
        System.Type m_EnumType;

        public IEnumerable<int> filteredOutValues { get; set; }

        public Action<VFXEnumField> OnDisplayMenu;

        void CreateHierarchy()
        {
            AddToClassList("unity-enum-field");
            m_DropDownButton = new Label();
            m_DropDownButton.AddToClassList("unity-enum-field__input");
            m_DropDownButton.AddManipulator(new DownClickable(OnClick));

            m_ValueText = new TextElement();
            m_ValueText.AddToClassList("unity-enum-field__text");

            var icon = new VisualElement() { name = "icon" };
            icon.AddToClassList("unity-enum-field__arrow");
            m_DropDownButton.Add(m_ValueText);
            m_DropDownButton.Add(icon);
        }

        void OnClick()
        {
            if (OnDisplayMenu != null)
                OnDisplayMenu(this);
            GenericMenu menu = new GenericMenu();

            foreach (string val in System.Enum.GetNames(m_EnumType))
            {
                int valueInt = (int)System.Enum.Parse(m_EnumType, val);
                if(filteredOutValues == null || !filteredOutValues.Any(t=>t == valueInt))
                    menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(val)), valueInt == m_Value, ChangeValue, valueInt);
            }
            menu.DropDown(m_DropDownButton.worldBound);
        }

        void ChangeValue(object val)
        {
            SetValue((int)val);
            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }

        public VFXEnumField(string label, System.Type enumType) : base(label)
        {
            CreateHierarchy();

            if (!enumType.IsEnum)
            {
                Debug.LogError("The type passed To enumfield must be an enumType");
            }
            m_EnumType = enumType;

            style.flexDirection = FlexDirection.Row;
            Add(m_DropDownButton);

            var icon = new VisualElement() { name = "icon" };
            icon.AddToClassList("unity-enum-field__arrow");

            m_DropDownButton.Add(icon);
        }

        public VFXEnumField(Label existingLabel, System.Type enumType) : base(existingLabel)
        {
            CreateHierarchy();
            if (!enumType.IsEnum)
            {
                Debug.LogError("The type passed To enum field must be an enumType");
            }
            m_EnumType = enumType;
            Add(m_DropDownButton);
        }

        protected override void ValueToGUI(bool force)
        {
            m_ValueText.text = ObjectNames.NicifyVariableName(System.Enum.GetName(m_EnumType, m_Value));
        }
    }
}
