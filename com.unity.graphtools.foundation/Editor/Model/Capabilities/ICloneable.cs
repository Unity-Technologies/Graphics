using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for cloneable graph element models.
    /// </summary>
    public interface ICloneable : IGraphElementModel
    {
        /// <summary>
        /// Clones the instance.
        /// </summary>
        /// <remarks>Note that it does not add the instance to a <see cref="IGraphModel"/>.</remarks>
        /// <returns>A clone of this graph element.</returns>
        IGraphElementModel Clone();
    }
}
