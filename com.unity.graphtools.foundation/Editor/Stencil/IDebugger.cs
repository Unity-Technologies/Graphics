using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Stencil specific implementation of tracing/debugging
    /// </summary>
    public interface IDebugger
    {
        /// <summary>
        /// Setup called when the tracing plugin is starting
        /// </summary>
        /// <param name="graphModel">The current graph model.</param>
        /// <param name="tracingEnabled">The initial tracing state.</param>
        void Start(IGraphModel graphModel, bool tracingEnabled);

        /// <summary>
        /// Tear down called when the tracing plugin is stopping
        /// </summary>
        void Stop();

        /// <summary>
        /// Gets collection of all debugging targets (entities, game objects, ...) as arbitrary indices
        /// </summary>
        /// <param name="graphModel">The current graph model.</param>
        /// <returns>The list of targets or null if none could be produced.</returns>
        IEnumerable<int> GetDebuggingTargets(IGraphModel graphModel);

        /// <summary>
        /// Used to fill the current tracing target label in the UI
        /// </summary>
        /// <param name="graphModel"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        string GetTargetLabel(IGraphModel graphModel, int target);

        /// <summary>
        /// Produces a list of steps for a given graph, frame and target
        /// </summary>
        /// <param name="currentGraphModel">The current graph.</param>
        /// <param name="frame">The current frame.</param>
        /// <param name="tracingTarget">The current target.</param>
        /// <param name="stepList">The resulting list of steps.</param>
        /// <returns>Returns true if successful.</returns>
        bool GetTracingSteps(IGraphModel currentGraphModel, int frame, int tracingTarget, out List<TracingStep> stepList);

        /// <summary>
        /// Get the existing graph trace of a given graph and target including all recorded frames
        /// </summary>
        /// <param name="assetModelGraphModel">The current graph.</param>
        /// <param name="currentTracingTarget">The current target.</param>
        /// <returns>The trace of all frames relevant to this specific graph and target tuple.</returns>
        IGraphTrace GetGraphTrace(IGraphModel assetModelGraphModel, int currentTracingTarget);
    }
}
