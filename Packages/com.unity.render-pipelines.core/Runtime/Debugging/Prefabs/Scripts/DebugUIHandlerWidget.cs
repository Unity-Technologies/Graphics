using System;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// Base class for handling UI actions for widgets.
    /// </summary>
    [CoreRPHelpURL("Rendering-Debugger")]
    public class DebugUIHandlerWidget : MonoBehaviour
    {
        /// <summary>
        /// Default widget color.
        /// </summary>
        [HideInInspector]
        public Color colorDefault = new Color(0.8f, 0.8f, 0.8f, 1f);

        /// <summary>
        /// Selected widget color.
        /// </summary>
        [HideInInspector]
        public Color colorSelected = new Color(0.25f, 0.65f, 0.8f, 1f);

        /// <summary>
        /// Parent widget UI Handler.
        /// </summary>
        public DebugUIHandlerWidget parentUIHandler { get; set; }
        /// <summary>
        /// Previous widget UI Handler.
        /// </summary>
        public DebugUIHandlerWidget previousUIHandler { get; set; }
        /// <summary>
        /// Next widget UI Handler.
        /// </summary>
        public DebugUIHandlerWidget nextUIHandler { get; set; }

        /// <summary>
        /// Associated widget.
        /// </summary>
        protected DebugUI.Widget m_Widget;

        /// <summary>
        /// OnEnable implementation.
        /// </summary>
        protected virtual void OnEnable() { }

        internal virtual void SetWidget(DebugUI.Widget widget)
        {
            m_Widget = widget;
        }

        internal DebugUI.Widget GetWidget()
        {
            return m_Widget;
        }

        /// <summary>
        /// Casts the widget to the correct type.
        /// </summary>
        /// <typeparam name="T">Type of the widget.</typeparam>
        /// <returns>Properly cast reference to the widget.</returns>
        protected T CastWidget<T>()
            where T : DebugUI.Widget
        {
            var casted = m_Widget as T;
            string typeName = m_Widget == null ? "null" : m_Widget.GetType().ToString();

            if (casted == null)
                throw new InvalidOperationException("Can't cast " + typeName + " to " + typeof(T));

            return casted;
        }

        /// <summary>
        /// OnSelection implementation.
        /// </summary>
        /// <param name="fromNext">True if the selection wrapped around.</param>
        /// <param name="previous">Previous widget.</param>
        /// <returns>True if the selection is allowed.</returns>
        public virtual bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            return true;
        }

        /// <summary>
        /// OnDeselection implementation.
        /// </summary>
        public virtual void OnDeselection() { }

        /// <summary>
        /// OnAction implementation.
        /// </summary>
        public virtual void OnAction() { }

        /// <summary>
        /// OnIncrement implementation.
        /// </summary>
        /// <param name="fast">True if incrementing fast.</param>
        public virtual void OnIncrement(bool fast) { }

        /// <summary>
        /// OnDecrement implementation.
        /// </summary>
        /// <param name="fast">Trye if decrementing fast.</param>
        public virtual void OnDecrement(bool fast) { }

        /// <summary>
        /// Previous implementation.
        /// </summary>
        /// <returns>Previous widget UI handler, parent if there is none.</returns>
        public virtual DebugUIHandlerWidget Previous()
        {
            if (previousUIHandler != null)
                return previousUIHandler;

            if (parentUIHandler != null)
                return parentUIHandler;

            return null;
        }

        /// <summary>
        /// Next implementation.
        /// </summary>
        /// <returns>Next widget UI handler, parent if there is none.</returns>
        public virtual DebugUIHandlerWidget Next()
        {
            if (nextUIHandler != null)
                return nextUIHandler;

            if (parentUIHandler != null)
            {
                var p = parentUIHandler;
                while (p != null)
                {
                    var n = p.nextUIHandler;

                    if (n != null)
                        return n;

                    p = p.parentUIHandler;
                }
            }

            return null;
        }
    }
}
