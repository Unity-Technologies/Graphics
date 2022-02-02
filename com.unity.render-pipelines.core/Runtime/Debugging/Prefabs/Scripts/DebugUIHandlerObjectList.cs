namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for object list widget.
    /// </summary>
    public class DebugUIHandlerObjectList : DebugUIHandlerField<DebugUI.ObjectListField>
    {
        int m_Index;

        /// <summary>
        /// Sets the widget and updates the label
        /// </summary>
        /// <param name="widget">The <see cref="DebugUI.Widget"/></param>
        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Index = 0;
        }

        /// <summary>
        /// OnIncrement implementation.
        /// </summary>
        /// <param name="fast">True if incrementing fast.</param>
        public override void OnIncrement(bool fast)
        {
            m_Index++;
            UpdateValueLabel();
        }

        /// <summary>
        /// OnDecrement implementation.
        /// </summary>
        /// <param name="fast">Trye if decrementing fast.</param>
        public override void OnDecrement(bool fast)
        {
            m_Index--;
            UpdateValueLabel();
        }

        /// <summary>
        /// Update the label of the widget.
        /// </summary>
        public override void UpdateValueLabel()
        {
            string text = "Empty";
            var values = m_Field.GetValue();
            if (values != null)
            {
                m_Index = System.Math.Clamp(m_Index, 0, values.Length - 1);
                text = values[m_Index].name;
            }
            SetLabelText(text);
        }
    }
}
