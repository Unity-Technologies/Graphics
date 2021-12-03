using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for object list widget.
    /// </summary>
    public class DebugUIHandlerObjectList : DebugUIHandlerWidget
    {
        /// <summary>Text displayed for the "next" button.</summary>
        public Text nextButtonText;
        /// <summary>Text displayed for the "previous" button.</summary>
        public Text previousButtonText;
        /// <summary>Name of the enum field.</summary>
        public Text nameLabel;
        /// <summary>Value of the enum field.</summary>
        public Text valueLabel;

        internal protected DebugUI.ObjectListField m_Field;
        int index;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.ObjectListField>();
            nameLabel.text = m_Field.displayName;
            index = 0;
            UpdateValueLabel();
        }

        /// <summary>
        /// OnSelection implementation.
        /// </summary>
        /// <param name="fromNext">True if the selection wrapped around.</param>
        /// <param name="previous">Previous widget.</param>
        /// <returns>State of the widget.</returns>
        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            if (nextButtonText != null)
                nextButtonText.color = colorSelected;
            if (previousButtonText != null)
                previousButtonText.color = colorSelected;
            nameLabel.color = colorSelected;
            valueLabel.color = colorSelected;
            return true;
        }

        /// <summary>
        /// OnDeselection implementation.
        /// </summary>
        public override void OnDeselection()
        {
            if (nextButtonText != null)
                nextButtonText.color = colorDefault;
            if (previousButtonText != null)
                previousButtonText.color = colorDefault;
            nameLabel.color = colorDefault;
            valueLabel.color = colorDefault;
        }

        /// <summary>
        /// OnAction implementation.
        /// </summary>
        public override void OnAction()
        {
            OnIncrement(false);
        }

        /// <summary>
        /// OnIncrement implementation.
        /// </summary>
        /// <param name="fast">True if incrementing fast.</param>
        public override void OnIncrement(bool fast)
        {
            index++;
            UpdateValueLabel();
        }

        /// <summary>
        /// OnDecrement implementation.
        /// </summary>
        /// <param name="fast">Trye if decrementing fast.</param>
        public override void OnDecrement(bool fast)
        {
            index--;
            UpdateValueLabel();
        }

        /// <summary>
        /// Update the label of the widget.
        /// </summary>
        internal protected virtual void UpdateValueLabel()
        {
            string text = "Empty";
            var values = m_Field.GetValue();
            if (values != null)
            {
                index = System.Math.Clamp(index, 0, values.Length - 1);
                text = values[index].name;

                // The UI implementation is tight with space, so let's just truncate the string here if too long.
                const int maxLength = 26;
                if (text.Length > maxLength)
                    text = text.Substring(0, maxLength - 3) + "...";
            }

            valueLabel.text = text;
        }
    }
}
