using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for parts that might have a different look based on the zoom.
    /// </summary>
    public interface IGraphElementPart : IModelViewPart
    {
        /// <summary>
        /// Sets the level of detail appearance of the part based on the current zoom.
        /// </summary>
        /// <param name="zoom"></param>
        void SetLevelOfDetail(float zoom);
    }
}
