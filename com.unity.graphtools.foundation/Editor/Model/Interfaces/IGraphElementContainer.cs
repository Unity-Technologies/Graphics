using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A container for graph elements.
    /// </summary>
    public interface IGraphElementContainer
    {
        /// <summary>
        /// The contained element models.
        /// </summary>
        IEnumerable<IGraphElementModel> GraphElementModels { get; }

        /// <summary>
        /// Removes graph element models from the container.
        /// </summary>
        /// <param name="elementModels">The elements to remove.</param>
        void RemoveElements(IReadOnlyCollection<IGraphElementModel> elementModels);

        /// <summary>
        /// Repair the container by removing invalid or null references.
        /// </summary>
        void Repair();
    }
}
