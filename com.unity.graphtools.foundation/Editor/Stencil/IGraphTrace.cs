using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The trace of all recorded frames relevant to a specific graph and target tuple
    /// </summary>
    public interface IGraphTrace
    {
        /// <summary>
        /// Data for each recorded tracing frame.
        /// </summary>
        IReadOnlyList<IFrameData> AllFrames { get; }
    }
}
