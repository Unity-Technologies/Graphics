using System;
using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for progress bar widget.
    /// </summary>
    public class DebugUIHandlerProgressBar : DebugUIHandlerWidget
    {
        /// <summary>Name of the progress bar.</summary>
        public Text nameLabel;
        /// <summary>Value of the progress bar.</summary>
        public Text valueLabel;
        /// <summary>Rectangle representing the progress bar.</summary>
        public RectTransform progressBarRect;

        DebugUI.ProgressBarValue m_Value;

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
            m_Value = CastWidget<DebugUI.ProgressBarValue>();
            nameLabel.text = m_Value.displayName;
            UpdateValue();
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
            return true;
        }

        /// <summary>
        /// OnDeselection implementation.
        /// </summary>
        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
        }

        void Update()
        {
            if (m_Timer >= m_Value.refreshRate)
            {
                UpdateValue();
                m_Timer -= m_Value.refreshRate;
            }

            m_Timer += Time.deltaTime;
        }

        void UpdateValue()
        {
            float value = (float)m_Value.GetValue();
            valueLabel.text = m_Value.FormatString(value);

            Vector3 scale = progressBarRect.localScale;
            scale.x = value;
            progressBarRect.localScale = scale;
        }
    }
}
