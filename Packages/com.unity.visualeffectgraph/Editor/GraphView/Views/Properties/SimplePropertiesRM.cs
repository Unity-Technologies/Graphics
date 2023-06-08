using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using EnumField = UnityEditor.VFX.UIElements.VFXEnumField;

namespace UnityEditor.VFX.UI
{
    class EnumPropertyRM : SimplePropertyRM<int>
    {
        public EnumPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            int min = 120;
            foreach (var str in Enum.GetNames(provider.portType))
            {
                Vector2 size = m_Field.Q<TextElement>().MeasureTextSize(str, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);

                size.x += 60;
                if (min < size.x)
                    min = (int)size.x;
            }
            if (min > 200)
                min = 200;


            return min;
        }

        public override ValueControl<int> CreateField()
        {
            var field = new EnumField(m_Label, m_Provider.portType);
            field.OnDisplayMenu = OnDisplayMenu;

            return field;
        }

        void OnDisplayMenu(EnumField field)
        {
            field.filteredOutValues = provider.filteredOutEnumerators;
        }
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
            m_Field = new DropdownField(null, choices.values ?? new List<string>(), 0, FormatSelectedValueCallback);
            m_Field.RegisterValueChangedCallback(OnValueChanged);
            Add(m_Field);
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            m_Value.SetSelection(evt.newValue);
            NotifyValueChanged();
        }

        public override float GetPreferredControlWidth() => 120;
        protected override void UpdateEnabled()
        {
        }

        protected override void UpdateIndeterminate()
        {
        }

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
            m_FieldParent.style.flexDirection = FlexDirection.Row;

            fieldControl.onValueDragFinished = () => ValueDragFinished();
            fieldControl.onValueDragStarted = () => ValueDragStarted();
        }

        public override float GetPreferredControlWidth()
        {
            return 260;
        }

        protected void ValueDragFinished()
        {
            m_Provider.EndLiveModification();
            hasChangeDelayed = false;
            NotifyValueChanged();
        }

        protected void ValueDragStarted()
        {
            m_Provider.StartLiveModification();
        }
    }

    class FlipBookPropertyRM : SimpleVFXUIPropertyRM<VFXFlipBookField, FlipBook>
    {
        public FlipBookPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 100;
        }
    }
}
