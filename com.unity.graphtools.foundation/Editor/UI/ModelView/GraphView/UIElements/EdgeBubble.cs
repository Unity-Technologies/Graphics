using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A rounded label used to display text on an edge. Position of the label follows another VisualElement.
    /// </summary>
    public class EdgeBubble : Label
    {
        public new static readonly string ussClassName = "ge-edge-bubble";

        protected Attacher m_Attacher;

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeBubble"/> class.
        /// </summary>
        public EdgeBubble()
        {
            AddToClassList(ussClassName);
        }

        public void AttachTo(VisualElement edgeControlTarget, SpriteAlignment align)
        {
            if (m_Attacher?.Target == edgeControlTarget && m_Attacher?.Alignment == align)
                return;

            Detach();

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_Attacher = new Attacher(this, edgeControlTarget, align);
        }

        public void Detach()
        {
            if (m_Attacher == null)
                return;

            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_Attacher.Detach();
            m_Attacher = null;
        }

        protected void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ResizeToFitText();
        }

        public void SetAttacherOffset(Vector2 offset)
        {
            if (m_Attacher != null)
                m_Attacher.Offset = offset;
        }

        void ResizeToFitText()
        {
            if (style.fontSize == 0)
                return;

            var newSize = DoMeasure(resolvedStyle.maxWidth.value, MeasureMode.AtMost, 0, MeasureMode.Undefined);

            style.width = newSize.x +
                resolvedStyle.marginLeft +
                resolvedStyle.marginRight +
                resolvedStyle.borderLeftWidth +
                resolvedStyle.borderRightWidth +
                resolvedStyle.paddingLeft +
                resolvedStyle.paddingRight;

            style.height = newSize.y +
                resolvedStyle.marginTop +
                resolvedStyle.marginBottom +
                resolvedStyle.borderTopWidth +
                resolvedStyle.borderBottomWidth +
                resolvedStyle.paddingTop +
                resolvedStyle.paddingBottom;
        }
    }
}
