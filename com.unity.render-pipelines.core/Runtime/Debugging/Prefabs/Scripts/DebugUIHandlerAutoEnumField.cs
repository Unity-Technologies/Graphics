using System;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for enumerator widget that is automatic
    /// </summary>
    public class DebugUIHandlerAutoEnumField : DebugUIHandlerField<DebugUI.AutoEnumField>
    {
        private int index { get; set; } = 0;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            var autoEnum = widget as DebugUI.AutoEnumField;
            index = Array.IndexOf(autoEnum.enums, autoEnum.GetValue());

            base.SetWidget(widget);
        }

        /// <summary>
        /// OnIncrement implementation.
        /// </summary>
        /// <param name="fast">True if incrementing fast.</param>
        public override void OnIncrement(bool fast)
        {
            SetValue(index + 1);
        }

        void SetValue(int idx)
        {
            if (m_Field.enumNames.Length == 0)
                return;

            if (idx < 0)
                idx = m_Field.enums.Length - 1;
            else if (m_Field.enums.Length - 1 < idx)
                idx = 0;

            index = idx;

            var selectEnum = m_Field.enums[index];
            m_Field.SetValue(selectEnum);
            UpdateValueLabel();
        }

        /// <summary>
        /// OnDecrement implementation.
        /// </summary>
        /// <param name="fast">True if decrementing fast.</param>
        public override void OnDecrement(bool fast)
        {
            SetValue(index - 1);
        }

        /// <summary>
        /// Update the label of the widget.
        /// </summary>
        public override void UpdateValueLabel()
        {
            if (m_Field.enumNames.Length == 0)
                SetLabelText("None");
            else
                SetLabelText(m_Field.enumNames[index].text);
        }
    }
}
