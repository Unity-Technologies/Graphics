#if UNITY_2022_2_OR_NEWER
using System;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for overlay windows.
    /// </summary>
    public abstract class ResizableOverlay : Overlay
    {
        OverlayResizer m_Resizer;

        /// <summary>
        /// The container for the resizable content.
        /// </summary>
        protected VisualElement ResizableContentContainer { get; private set; }

        /// <inheritdoc />
        public override VisualElement CreatePanelContent()
        {
            ResizableContentContainer = new VisualElement();
            ResizableContentContainer.AddToClassList(ussClassName.WithUssElement("resizable-content-container"));

            var content = CreateResizablePanelContent();
            ResizableContentContainer.Add(content);

            m_Resizer = new OverlayResizer();
            ResizableContentContainer.Add(m_Resizer);

            ResizableContentContainer.AddStylesheet(Stylesheet);

            return ResizableContentContainer;
        }

        /// <summary>
        /// The stylesheet to add to the <see cref="ResizableContentContainer"/>.
        /// </summary>
        protected abstract string Stylesheet { get;  }

        /// <summary>
        /// Creates the content to add to the <see cref="ResizableContentContainer"/>.
        /// </summary>
        /// <returns>The root of the content.</returns>
        protected abstract VisualElement CreateResizablePanelContent();
    }
}
#endif
