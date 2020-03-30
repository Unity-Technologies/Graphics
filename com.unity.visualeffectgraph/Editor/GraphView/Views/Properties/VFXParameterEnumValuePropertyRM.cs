using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;
using EnumField = UnityEditor.VFX.UIElements.VFXEnumField;
using VFXVector2Field = UnityEditor.VFX.UI.VFXVector2Field;
using VFXVector4Field = UnityEditor.VFX.UI.VFXVector4Field;

namespace UnityEditor.VFX.UI
{
    class VFXParameterEnumValuePropertyRM : PropertyRM<VFXEnumValue>
    {
        VFXStringField m_NameField;
        PropertyRM m_ValueProperty;

        class ValueProvider : IPropertyRMProvider
        {
            public ValueProvider(VFXParameterEnumValuePropertyRM owner)
            {
                m_Owner = owner;
            }

            VFXParameterEnumValuePropertyRM m_Owner;
            bool IPropertyRMProvider.expanded => false;

            bool IPropertyRMProvider.expandable => false;

            bool IPropertyRMProvider.expandableIfShowsEverything => false;

            object IPropertyRMProvider.value
            {
                get => ((VFXEnumValue)m_Owner.m_Provider.value).value.Get();

                set
                {
                    var val = (VFXEnumValue)m_Owner.m_Provider.value;

                    val.value.Set(value);

                    m_Owner.m_Provider.value = val;
                }
            }

            bool IPropertyRMProvider.spaceableAndMasterOfSpace => false;

            VFXCoordinateSpace IPropertyRMProvider.space { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            string IPropertyRMProvider.name => "";

            VFXPropertyAttributes IPropertyRMProvider.attributes => new VFXPropertyAttributes();

            object[] IPropertyRMProvider.customAttributes => null;

            Type IPropertyRMProvider.portType => ((VFXEnumValue)m_Owner.m_Provider.value).value.type;

            int IPropertyRMProvider.depth => 0;

            bool IPropertyRMProvider.editable => m_Owner.m_Provider.editable;

            void IPropertyRMProvider.ExpandPath()
            {
                throw new NotImplementedException();
            }

            bool IPropertyRMProvider.IsSpaceInherited()
            {
                return false;
            }

            void IPropertyRMProvider.RetractPath()
            {
                throw new NotImplementedException();
            }
        }

        public VFXParameterEnumValuePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            AddToClassList("VFXParameterEnumValuePropertyRM");
            m_NameField = new VFXStringField("");
            m_NameField.style.flexGrow = 1;
            m_NameField.textfield.style.flexGrow = 1;
            m_NameField.style.flexShrink = 1;
            m_NameField.textfield.style.flexShrink = 1;
            m_NameField.OnValueChanged += OnNameChanges;
            Add(m_NameField);
            m_ValueProperty = PropertyRM.Create(new ValueProvider(this),0);
            m_ValueProperty.style.width = 50;
            Add(m_ValueProperty);
        }
        public override bool showsEverything => true;

        public override float GetPreferredControlWidth()
        {
            return 150;
        }


        void OnNameChanges()
        {
            VFXEnumValue value = (VFXEnumValue)m_Provider.value;
            value.name = m_NameField.value;

            m_Provider.value = value;
        }

        public override void UpdateGUI(bool force)
        {
            m_NameField.value = ((VFXEnumValue)m_Provider.value).name;
            m_ValueProperty.Update();
        }

        protected override void UpdateEnabled()
        {
            m_NameField.SetEnabled(propertyEnabled);
            m_ValueProperty.propertyEnabled = propertyEnabled;
        }

        protected override void UpdateIndeterminate()
        {
            m_NameField.SetEnabled(indeterminate);
            m_ValueProperty.propertyEnabled = indeterminate;
        }
    }

    class VFXListParameterEnumValuePropertyRM : ListPropertyRM<VFXEnumValue, VFXParameterEnumValuePropertyRM>
    {
        public VFXListParameterEnumValuePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override VFXParameterEnumValuePropertyRM CreateField(IPropertyRMProvider provider)
        {
            return new VFXParameterEnumValuePropertyRM(provider,18);
        }

        protected override VFXEnumValue CreateItem()
        {
            return new VFXEnumValue() { name = "New item", value = new VFXSerializableObject(m_Provider.portType,VFXConverter.ConvertTo(m_List.itemCount,m_Provider.portType))};
        }
    }
}
