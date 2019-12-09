using System;
using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for indirect float widget.
    /// </summary>
    public class DebugUIHandlerIndirectFloatField : DebugUIHandlerWidget
    {
        /// <summary>Name of the indirect float field.</summary>
        public Text nameLabel;
        /// <summary>Value of the indirect float field.</summary>
        public Text valueLabel;

        /// <summary>
        /// Getter function for this indirect widget.
        /// </summary>
        public Func<float> getter;
        /// <summary>
        /// Setter function for this indirect widget.
        /// </summary>
        public Action<float> setter;

        /// <summary>
        /// Getter function for the increment step of this indirect widget.
        /// </summary>
        public Func<float> incStepGetter;
        /// <summary>
        /// Getter function for the increment step multiplier of this indirect widget.
        /// </summary>
        public Func<float> incStepMultGetter;
        /// <summary>
        /// Getter function for the number of decimals of this indirect widget.
        /// </summary>
        public Func<float> decimalsGetter;

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
            float value = getter();
            value += incStepGetter() * (fast ? incStepMultGetter() : 1f) * multiplier;
            setter(value);
            UpdateValueLabel();
        }

        void UpdateValueLabel()
        {
            if (valueLabel != null)
                valueLabel.text = getter().ToString("N" + decimalsGetter());
        }
    }
}
