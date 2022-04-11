using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for object widget.
    /// </summary>
    public class DebugUIHandlerObject : DebugUIHandlerWidget
    {
        /// <summary>Name of the value field.</summary>
        public Text nameLabel;
        /// <summary>Value of the value field.</summary>
        public Text valueLabel;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            var field = CastWidget<DebugUI.ObjectField>();
            nameLabel.text = field.displayName;
            valueLabel.text = field.GetValue().name;
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
            valueLabel.color = colorSelected;
            return true;
        }

        /// <summary>
        /// OnDeselection implementation.
        /// </summary>
        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
            valueLabel.color = colorDefault;
        }
    }
}
