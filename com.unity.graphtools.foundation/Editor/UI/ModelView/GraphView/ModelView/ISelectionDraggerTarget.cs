using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface defining the methods used by the <see cref="SelectionDragger"/>
    /// when elements are dragged over other elements.
    /// </summary>
    public interface ISelectionDraggerTarget
    {
        /// <summary>
        /// Determines whether this object will accept <paramref name="dropCandidates"/>.
        /// </summary>
        /// <param name="dropCandidates">The elements that would be dropped on this element.</param>
        /// <returns>True if this instance will accept the dropped elements.</returns>
        bool CanAcceptDrop(IReadOnlyList<IGraphElementModel> dropCandidates);

        /// <summary>
        /// Called by the <see cref="SelectionDragger"/> to enable the object to give feedback about the potential drop.
        /// </summary>
        /// <param name="dropCandidates">The elements that would be dropped on this element.</param>
        void SetDropHighlightStatus(IReadOnlyList<IGraphElementModel> dropCandidates);

        /// <summary>
        /// Called by the <see cref="SelectionDragger"/> to enable the object to clear the feedback about the potential drop.
        /// </summary>
        void ClearDropHighlightStatus();

        /// <summary>
        /// Performs the action triggered by dropping <paramref name="dropCandidates"/> onto this object.
        /// </summary>
        /// <param name="dropCandidates">A list of elements to drop.</param>
        void PerformDrop(IReadOnlyList<IGraphElementModel> dropCandidates);
    }
}
