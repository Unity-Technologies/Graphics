using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for group widget.
    /// </summary>
    public class DebugUIHandlerGroup : DebugUIHandlerWidget
    {
        /// <summary>Name of the group.</summary>
        public Text nameLabel;
        /// <summary>Header of the group.</summary>
        public Transform header;
        DebugUI.Container m_Field;
        DebugUIHandlerContainer m_Container;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.Container>();
            m_Container = GetComponent<DebugUIHandlerContainer>();

            if (string.IsNullOrEmpty(m_Field.displayName))
                header.gameObject.SetActive(false);
            else
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
            if (!fromNext && !m_Container.IsDirectChild(previous))
            {
                var lastItem = m_Container.GetLastItem();
                DebugManager.instance.ChangeSelection(lastItem, false);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Next implementation.
        /// </summary>
        /// <returns>Next widget UI handler, parent if there is none.</returns>
        public override DebugUIHandlerWidget Next()
        {
            if (m_Container == null)
                return base.Next();

            var firstChild = m_Container.GetFirstItem();

            if (firstChild == null)
                return base.Next();

            return firstChild;
        }
    }
}
