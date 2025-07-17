using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Utility class to connect SRP to automated test framework.
    /// </summary>
    public static class RenderGraphGraphicsAutomatedTests
    {
        // RenderGraph tests can be enabled from the command line. Cache result to avoid GC.
        static bool activatedFromCommandLine
        {
#if RENDER_GRAPH_REUSE_TESTS_STANDALONE
            get => true;
#else
            get => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-render-graph-reuse-tests");
#endif
        }

        ///<summary> Obsolete, use forceRenderGraphState instead </summary>
        [Obsolete]
        public static bool enabled { get; set; } = activatedFromCommandLine;

        /// <summary>
        /// Used by render pipelines to initialize RenderGraph tests.
        /// True = RenderGraph, False = CompatibilityMode, null = no effect (keep as it is configured in the project settings).
        /// </summary>
        public static bool? forceRenderGraphState { get; set; } = activatedFromCommandLine ? true : null;
    }
}
