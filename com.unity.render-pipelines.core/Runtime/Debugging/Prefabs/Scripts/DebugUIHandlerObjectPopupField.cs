using System.Linq;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for object popup widget.
    /// </summary>
    public class DebugUIHandlerObjectPopupField : DebugUIHandlerField<DebugUI.ObjectPopupField>
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

        private void ChangeSelectedObject()
        {
            if (m_Field == null)
                return;

            var elements = m_Field.getObjects();
            if (elements == null)
                return;

            var elementsArray = elements.ToArray();
            var count = elementsArray.Count();

            if (m_Index >= count)
            {
                m_Index = 0;
            }
            else if (m_Index < 0)
            {
                m_Index = count - 1;
            }

            var newSelectedValue = elementsArray[m_Index];
            m_Field.SetValue(newSelectedValue);

            UpdateValueLabel();
        }

        /// <summary>
        /// OnIncrement implementation.
        /// </summary>
        /// <param name="fast">True if incrementing fast.</param>
        public override void OnIncrement(bool fast)
        {
            m_Index++;
            ChangeSelectedObject();
        }

        /// <summary>
        /// OnDecrement implementation.
        /// </summary>
        /// <param name="fast">Trye if decrementing fast.</param>
        public override void OnDecrement(bool fast)
        {
            m_Index--;
            ChangeSelectedObject();
        }

        /// <summary>
        /// Update the label of the widget.
        /// </summary>
        public override void UpdateValueLabel()
        {
            var selectedObject = m_Field.GetValue();
            string text = (selectedObject != null) ? selectedObject.name : "Empty";
            SetLabelText(text);
        }
    }
}
