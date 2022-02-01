using System;
using JetBrains.Annotations;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Options for graph processing.
    /// </summary>
    [Flags]
    [PublicAPI]
    public enum GraphProcessingOptions
    {
        Default = 0,
        Tracing = 1 << 0,
        Profiling = 1 << 1,
        LiveEditing = 1 << 2,
        ImplementationOnly = 1 << 3
    }
}
