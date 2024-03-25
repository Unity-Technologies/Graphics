using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    sealed class EnumPropertyRM : PropertyRM<Enum>
    {
        private readonly EnumField m_EnumField;

        public EnumPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_EnumField = new EnumField(ObjectNames.NicifyVariableName(m_Provider.name), (Enum)m_Provider.value);
            m_EnumField.RegisterCallback<ChangeEvent<Enum>>(OnValueChange);
            Add(m_EnumField);
            SetLabelWidth(labelWidth);
        }

        private void OnValueChange(ChangeEvent<Enum> evt)
        {
            provider.value = evt.newValue;
        }

        public override float GetPreferredControlWidth() => 120;
        protected override void UpdateEnabled() => m_EnumField.SetEnabled(propertyEnabled);
        protected override void UpdateIndeterminate() => m_EnumField.showMixedValue = indeterminate;
        public override void UpdateGUI(bool force) => m_EnumField.SetValueWithoutNotify(m_Value);
        public override bool showsEverything => true;
    }

    [Serializable]
    struct MultipleValuesChoice<T> where T: class
    {
        [SerializeField]
        private T selection;
        [SerializeField]
        private int selectedIndex;

        public List<T> values { get; set; }

        public void SetSelection(T value)
        {
            selectedIndex = values?.IndexOf(value) ?? -1;

            if (selectedIndex >= 0)
            {
                selection = value;
            }
        }

        public T GetSelection()
        {
            return selection;
        }
    }

    class ListPropertyRM : PropertyRM<MultipleValuesChoice<string>>
    {
        private DropdownField m_Field;
        public ListPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            var choices = (MultipleValuesChoice<string>)m_Provider.value;
            m_Field = new DropdownField(ObjectNames.NicifyVariableName(controller.name), choices.values ?? new List<string>(), 0, FormatSelectedValueCallback);
            m_Field.RegisterValueChangedCallback(OnValueChanged);
            Add(m_Field);
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            m_Value.SetSelection(evt.newValue);
            NotifyValueChanged();
        }

        public override float GetPreferredControlWidth() => 120;
        protected override void UpdateEnabled() => m_Field.SetEnabled(propertyEnabled);
        protected override void UpdateIndeterminate() => m_Field.showMixedValue = indeterminate;

        public override void UpdateGUI(bool force)
        {
            if (m_Value.values?.Count > 0)
            {
                m_Field.choices = m_Value.values;
                m_Field.SetEnabled(true);
                m_Field.value = m_Value.GetSelection();
            }
            else
            {
                m_Field.value = null;
                m_Field.SetEnabled(false);
            }
        }

        public override bool showsEverything => false;

        private string FormatSelectedValueCallback(string selection)
        {

            return selection;
        }
    }

    class Matrix4x4PropertyRM : SimpleVFXUIPropertyRM<VFXMatrix4x4Field, Matrix4x4>
    {
        public Matrix4x4PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            fieldControl.onValueDragFinished += ValueDragFinished;
            fieldControl.onValueDragStarted += ValueDragStarted;
        }

        public override float GetPreferredControlWidth() => 260;

        protected override void UpdateIndeterminate()
        {
            ((VFXMatrix4x4Field)field).indeterminate = indeterminate;
        }

        public override INotifyValueChanged<Matrix4x4> CreateField()
        {
            return new VFXMatrix4x4Field(ObjectNames.NicifyVariableName(provider.name));
        }
    }

    class FlipBookPropertyRM : SimpleVFXUIPropertyRM<VFXFlipBookField, FlipBook>
    {
        public FlipBookPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth() => 100;

        protected override void UpdateIndeterminate()
        {
            ((VFXFlipBookField)field).indeterminate = indeterminate;
        }

        public override INotifyValueChanged<FlipBook> CreateField()
        {
            return new VFXFlipBookField(ObjectNames.NicifyVariableName(provider.name));
        }
    }
}
