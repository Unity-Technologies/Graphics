using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for clonable graph element models.
    /// </summary>
    public interface ICloneable : IGraphElementModel
    {
        /// <summary>
        /// Clones the instance.
        /// </summary>
        /// <returns>A clone of this graph element.</returns>
        IGraphElementModel Clone();
    }
}
