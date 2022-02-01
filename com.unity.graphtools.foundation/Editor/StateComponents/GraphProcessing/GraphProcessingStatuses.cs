using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The status of the graph processing.
    /// </summary>
    public enum GraphProcessingStatuses
    {
        /// <summary>
        /// Graph processing was successful.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Graph processing encountered errors.
        /// </summary>
        Failed
    }
}
