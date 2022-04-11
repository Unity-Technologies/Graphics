using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for MessageBox widget.
    /// </summary>
    public class DebugUIHandlerMessageBox : DebugUIHandlerWidget
    {
        /// <summary>Name of the widget.</summary>
        public Text nameLabel;

        DebugUI.MessageBox m_Field;

        static Color32 k_WarningBackgroundColor = new Color32(231, 180, 3, 30);
        static Color32 k_WarningTextColor = new Color32(231, 180, 3, 255);
        static Color32 k_ErrorBackgroundColor = new Color32(231, 75, 3, 30);
        static Color32 k_ErrorTextColor = new Color32(231, 75, 3, 255);

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.MessageBox>();
            nameLabel.text = m_Field.displayName;

            var image = GetComponent<Image>();
            switch (m_Field.style)
            {
                case DebugUI.MessageBox.Style.Warning:
                    image.color = k_WarningBackgroundColor;
                    break;

                case DebugUI.MessageBox.Style.Error:
                    image.color = k_ErrorBackgroundColor;
                    break;
            }
        }

        /// <summary>
        /// Method called when the box is selected
        /// </summary>
        /// <param name="fromNext">If is from next</param>
        /// <param name="previous">The previous <see cref="DebugUIHandlerWidget"/></param>
        /// <returns></returns>
        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            return false;
        }
    }
}
