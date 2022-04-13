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

        float m_Timer;

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
                valueLabel.text = m_Field.GetValue().ToString();
                m_Timer -= m_Field.refreshRate;
            }

            m_Timer += Time.deltaTime;
        }
    }
}
