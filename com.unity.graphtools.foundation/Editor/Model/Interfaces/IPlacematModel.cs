using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for placemats.
    /// </summary>
    public interface IPlacematModel : IHasTitle, IMovable, ICollapsible, IResizable, IRenamable, IDestroyable
    {
        /// <summary>
        /// Elements hidden in the placemat.
        /// </summary>
        IEnumerable<IGraphElementModel> HiddenElements { get; set; }

        /// <summary>
        /// Returns the Z-order of the placemat in the graph.
        /// </summary>
        /// <returns>The Z-order of the placemat in the graph.</returns>
        int GetZOrder();
    }
}
