using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.VFX.UI
{
    class GradientPropertyRM : PropertyRM<Gradient>
    {
        public GradientPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_GradientField = new GradientField(ObjectNames.NicifyVariableName(controller.name));
            m_GradientField.RegisterCallback<ChangeEvent<Gradient>>(OnValueChanged);
            m_GradientField.colorSpace = ColorSpace.Linear;
            m_GradientField.hdr = true;

            Add(m_GradientField);
        }

        public override float GetPreferredControlWidth()
        {
            return 120;
        }

        public void OnValueChanged(ChangeEvent<Gradient> e)
        {
            Gradient newValue = m_GradientField.value;
            m_Value = newValue;
            NotifyValueChanged();
        }

        GradientField m_GradientField;

        protected override void UpdateEnabled()
        {
            m_GradientField.SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            m_GradientField.visible = !indeterminate;
        }

        public override void UpdateGUI(bool force)
        {
            m_GradientField.SetValueWithoutNotify(m_Value);
        }

        public override bool showsEverything { get { return true; } }
    }
}
