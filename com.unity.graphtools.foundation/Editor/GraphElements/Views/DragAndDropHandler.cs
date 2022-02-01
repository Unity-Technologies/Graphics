using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Handler dispatching an action when external elements are dropped
    /// Basic helper with default implementation
    /// </summary>
    public abstract class DragAndDropHandler : IDragAndDropHandler
    {
        public virtual void OnDragUpdated(DragUpdatedEvent e)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        }

        public virtual void OnDragPerform(DragPerformEvent evt)
        {
        }

        public virtual void OnDragEnter(DragEnterEvent e)
        {
        }

        public virtual void OnDragLeave(DragLeaveEvent e)
        {
        }

        public virtual void OnDragExited(DragExitedEvent e)
        {
        }
    }
}
