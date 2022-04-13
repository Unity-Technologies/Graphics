using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for float widget.
    /// </summary>
    public class DebugUIHandlerFloatField : DebugUIHandlerWidget
    {
        /// <summary>Name of the enum field.</summary>
        public Text nameLabel;
        /// <summary>Value of the enum field.</summary>
        public Text valueLabel;
        DebugUI.FloatField m_Field;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.FloatField>();
            nameLabel.text = m_Field.displayName;
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

        /// <summary>
        /// OnIncrement implementation.
        /// </summary>
        /// <param name="fast">True if incrementing fast.</param>
        public override void OnIncrement(bool fast)
        {
            ChangeValue(fast, 1);
        }

        /// <summary>
        /// OnDecrement implementation.
        /// </summary>
        /// <param name="fast">Trye if decrementing fast.</param>
        public override void OnDecrement(bool fast)
        {
            ChangeValue(fast, -1);
        }

        void ChangeValue(bool fast, float multiplier)
        {
            float value = m_Field.GetValue();
            value += m_Field.incStep * (fast ? m_Field.incStepMult : 1f) * multiplier;
            m_Field.SetValue(value);
            UpdateValueLabel();
        }

        void UpdateValueLabel()
        {
            valueLabel.text = m_Field.GetValue().ToString("N" + m_Field.decimals);
        }
    }
}
