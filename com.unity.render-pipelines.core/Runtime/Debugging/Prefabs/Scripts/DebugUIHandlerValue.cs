using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for value widgets.
    /// </summary>
    public class DebugUIHandlerValue : DebugUIHandlerWidget
    {
        /// <summary>Name of the value field.</summary>
        public Text nameLabel;
        /// <summary>Value of the value field.</summary>
        public Text valueLabel;

        DebugUI.Value m_Field;
        protected internal float m_Timer;
        static readonly Color k_ZeroColor = Color.gray;

        /// <summary>
        /// OnEnable implementation.
        /// </summary>
        protected override void OnEnable()
        {
            m_Timer = 0f;
        }

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.Value>();
            nameLabel.text = m_Field.displayName;
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

        void Update()
        {
            if (m_Timer >= m_Field.refreshRate)
            {
                var value = m_Field.GetValue();
                valueLabel.text = m_Field.FormatString(value);
                // De-emphasize zero values by switching to dark gray color
                if (value is float)
                    valueLabel.color = (float)value == 0f ? k_ZeroColor : colorDefault;
                m_Timer -= m_Field.refreshRate;
            }

            m_Timer += Time.deltaTime;
        }
    }
}
