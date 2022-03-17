using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Class to add a border to its parent. The border is highlighted when the element is selected.
    /// </summary>
    public class SelectionBorder : VisualElement
    {
        public static readonly string ussClassName = "ge-selection-border";
        public static readonly string contentContainerElementName = "content-container";

        public VisualElement ContentContainer { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionBorder"/> class.
        /// </summary>
        public SelectionBorder()
        {
            // [VSB-695]
            // Setting pickingMode to Ignore so that our clumsy drag and drop implementation work for placemats.
            // Dnd is handled by the graph view and it looks at the event target. This means the event target
            // should be the placemat (in general, the element having the border), not the border or the content container.
            // This would be unneeded if the placemat handled DnD itself.
            pickingMode = PickingMode.Ignore;
            AddToClassList(ussClassName);

            ContentContainer = new VisualElement { name = contentContainerElementName, pickingMode = PickingMode.Ignore };
            ContentContainer.AddToClassList(ussClassName.WithUssElement(contentContainerElementName));
            Add(ContentContainer);

            this.AddStylesheet("SelectionBorder.uss");
        }
    }
}
