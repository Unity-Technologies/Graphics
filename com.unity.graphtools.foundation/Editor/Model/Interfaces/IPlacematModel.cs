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
    }
}
