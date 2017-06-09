using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;


namespace UnityEditor.VFX.UIElements
{
    class EnumField : ValueControl<int>
    {
        Button m_DropDownButton;
        System.Type m_EnumType;

        void CreateButton()
        {
            m_DropDownButton = new Button();
            m_DropDownButton.AddManipulator(new DownClickable(OnClick));
        }

        void OnClick()
        {
            GenericMenu menu = new GenericMenu();

            foreach (string val in System.Enum.GetNames(m_EnumType))
            {
                int valueInt = (int)System.Enum.Parse(m_EnumType, val);

                menu.AddItem(new GUIContent(val), valueInt == m_Value, ChangeValue, valueInt);
            }
            menu.DropDown(m_DropDownButton.globalBound);
        }

        void ChangeValue(object val)
        {
            SetValue((int)val);
            if (onValueChanged != null)
            {
                onValueChanged();
            }
        }

        public EnumField(string label, System.Type enumType) : base(label)
        {
            CreateButton();

            if (!enumType.IsEnum)
            {
                Debug.LogError("The type passed To enumfield must be an enumType");
            }
            m_EnumType = enumType;

            flexDirection = FlexDirection.Row;
            AddChild(m_DropDownButton);
        }

        public EnumField(VisualElement existingLabel, System.Type enumType) : base(existingLabel)
        {
            CreateButton();
            if (!enumType.IsEnum)
            {
                Debug.LogError("The type passed To enum field must be an enumType");
            }
            m_EnumType = enumType;
            AddChild(m_DropDownButton);
        }

        protected override void ValueToGUI()
        {
            m_DropDownButton.text = System.Enum.GetName(m_EnumType, m_Value);
        }
    }
}
