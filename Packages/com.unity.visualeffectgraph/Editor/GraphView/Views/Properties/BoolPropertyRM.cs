using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    sealed class BoolPropertyRM : PropertyRM<bool>
    {
        public BoolPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            // Hack: no label for '_vfx_enable' activation port
            m_Toggle = controller.name == "_vfx_enabled" ? new Toggle() : new Toggle(ObjectNames.NicifyVariableName(controller.name));
            m_Toggle.RegisterCallback<ChangeEvent<bool>>(OnValueChanged);
            Add(m_Toggle);
            SetLabelWidth(labelWidth);
        }

        void OnValueChanged(ChangeEvent<bool> e)
        {
            m_Value = m_Toggle.value;
            NotifyValueChanged();
        }

        public override float GetPreferredControlWidth()
        {
            return 20;
        }

        public override void UpdateGUI(bool force)
        {
            m_Toggle.SetValueWithoutNotify(m_Value);
        }

        Toggle m_Toggle;

        protected override void UpdateEnabled()
        {
            m_Toggle.SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            if (indeterminate)
                m_Toggle.AddToClassList("indeterminate");
            else
                m_Toggle.RemoveFromClassList("indeterminate");
        }

        public override bool showsEverything { get { return true; } }
    }
}
