using System;
using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for indirect toggle widget.
    /// </summary>
    public class DebugUIHandlerIndirectToggle : DebugUIHandlerWidget
    {
        /// <summary>
        /// Label of the widget.
        /// </summary>
        public Text nameLabel;
        /// <summary>Toggle of the toggle field.</summary>
        public Toggle valueToggle;
        /// <summary>Checkmark image.</summary>
        public Image checkmarkImage;

        /// <summary>
        /// Getter function for this indirect widget.
        /// </summary>
        public Func<int, bool> getter;
        /// <summary>
        /// Setter function for this indirect widget.
        /// </summary>
        public Action<int, bool> setter;

        // Should not be here, this is a byproduct of the Bitfield UI Handler implementation.
        internal int index;

        /// <summary>
        /// Initialize the indirect widget.
        /// </summary>
        public void Init()
        {
            UpdateValueLabel();
        }

        /// <summary>
        /// OnSelection implementation.
        /// </summary>
        /// <param name="fromNext">True if the selection wrapped around.</param>
        /// <param name="previous">Previous widget.</param>
        /// <returns>True if the selection is allowed.</returns>
        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            nameLabel.color = colorSelected;
            checkmarkImage.color = colorSelected;
            return true;
        }

        /// <summary>
        /// OnDeselection implementation.
        /// </summary>
        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
            checkmarkImage.color = colorDefault;
        }

        /// <summary>
        /// OnAction implementation.
        /// </summary>
        public override void OnAction()
        {
            bool value = !getter(index);
            setter(index, value);
            UpdateValueLabel();
        }

        internal void UpdateValueLabel()
        {
            if (valueToggle != null)
                valueToggle.isOn = getter(index);
        }
    }
}
