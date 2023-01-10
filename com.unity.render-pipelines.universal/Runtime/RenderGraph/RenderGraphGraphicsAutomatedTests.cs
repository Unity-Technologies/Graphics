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
#if UNITY_EDITOR
            get => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-render-graph-reuse-tests");
#elif RENDER_GRAPH_REUSE_TESTS_STANDALONE
            get => true;
#else
            get => false;
#endif
        }

        /// <summary>
        /// Used by render pipelines to initialize RenderGraph tests.
        /// </summary>
        public static bool enabled { get; } = activatedFromCommandLine;

    }
}
