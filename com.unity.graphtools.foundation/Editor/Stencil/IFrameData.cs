using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The trace of all steps recorded during a specific frame, in the context of a specific graph, target and frame
    /// </summary>
    public interface IFrameData
    {
        int Frame { get; }
        IEnumerable<TracingStep> GetDebuggingSteps(Stencil stencil);
    }
}
