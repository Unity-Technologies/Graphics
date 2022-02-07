using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// Base class for handling UI actions for widgets.
    /// </summary>
    /// <typeparam name="T">Base type for the field</typeparam>
    public abstract class DebugUIHandlerField<T> : DebugUIHandlerWidget
        where T : DebugUI.Widget
    {
        /// <summary>Text displayed for the "next" button.</summary>
        public Text nextButtonText;
        /// <summary>Text displayed for the "previous" button.</summary>
        public Text previousButtonText;
        /// <summary>Name of the enum field.</summary>
        public Text nameLabel;
        /// <summary>Value of the enum field.</summary>
        public Text valueLabel;

        /// <summary>
        /// The field
        /// </summary>
        internal protected T m_Field;

        /// <summary>
        /// Sets the widget and updates the label
        /// </summary>
        /// <param name="widget">The <see cref="DebugUI.Widget"/></param>
        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<T>();
            nameLabel.text = m_Field.displayName;
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
        /// Update the label of the widget.
        /// </summary>
        public abstract void UpdateValueLabel();

        /// <summary>
        /// Sets the label text
        /// </summary>
        /// <param name="text">The text to set to the label</param>
        protected void SetLabelText(string text)
        {
            // The UI implementation is tight with space, so let's just truncate the string here if too long.
            const int maxLength = 26;
            if (text.Length > maxLength)
            {
                text = text.Substring(0, maxLength - 3) + "...";
            }

            valueLabel.text = text;
        }
    }

}
