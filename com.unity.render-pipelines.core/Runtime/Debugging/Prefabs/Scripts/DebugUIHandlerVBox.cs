namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for vertical layoyut widget.
    /// </summary>
    public class DebugUIHandlerVBox : DebugUIHandlerWidget
    {
        DebugUIHandlerContainer m_Container;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Container = GetComponent<DebugUIHandlerContainer>();
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
