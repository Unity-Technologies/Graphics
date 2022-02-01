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

        protected TextField TextField { get; }

        /// <inheritdoc />
        public override string text
        {
            get => base.text;
            set
            {
                if (base.text == value)
                    return;
                base.text = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeBubble"/> class.
        /// </summary>
        public EdgeBubble()
        {
            TextField = new TextField { isDelayed = true };

            AddToClassList(ussClassName);
        }

        protected void OnBlur(BlurEvent evt)
        {
            SaveAndClose();
        }

        protected void SaveAndClose()
        {
            text = TextField.text;
            Close();
        }

        protected void Close()
        {
            TextField.value = text;
            TextField.RemoveFromHierarchy();
            TextField.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            TextField.UnregisterCallback<BlurEvent>(OnBlur);
        }

        protected void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.KeypadEnter:
                case KeyCode.Return:
                    SaveAndClose();
                    break;
                case KeyCode.Escape:
                    Close();
                    break;
            }
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
            ComputeTextSize();
        }

        public void SetAttacherOffset(Vector2 offset)
        {
            if (m_Attacher != null)
                m_Attacher.Offset = offset;
        }

        protected void ComputeTextSize()
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
