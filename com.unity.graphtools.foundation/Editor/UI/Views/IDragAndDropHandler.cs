using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Handle every drag and drop call
    /// </summary>
    [PublicAPI]
    public interface IDragAndDropHandler
    {
        /// <summary>
        /// Tells whether this handler wants to actively accept or reject the dropped objects
        /// and eventually perform the drop operation.
        /// </summary>
        /// <returns>True if the handler wants to handle the operation, false if it does not.</returns>
        bool CanHandleDrop();

        /// <summary>
        /// Handler for any DragEnterEvent passed to this element
        /// </summary>
        /// <param name="evt">event passed.</param>
        void OnDragEnter(DragEnterEvent evt);

        /// <summary>
        /// Handler for any DragEnterEvent passed to this element
        /// </summary>
        /// <param name="evt">event passed.</param>
        void OnDragLeave(DragLeaveEvent evt);

        /// <summary>
        /// Handler for any DragEnterEvent passed to this element
        /// </summary>
        /// <param name="evt">event passed.</param>
        void OnDragUpdated(DragUpdatedEvent evt);

        /// <summary>
        /// Handler for any DragEnterEvent passed to this element
        /// </summary>
        /// <param name="evt">event passed.</param>
        void OnDragPerform(DragPerformEvent evt);

        /// <summary>
        /// Handler for any DragEnterEvent passed to this element
        /// </summary>
        /// <param name="evt">event passed.</param>
        void OnDragExited(DragExitedEvent evt);
    }
}
