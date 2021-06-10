using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Utility class to connect SRP to automated test framework.
    /// </summary>
    public static class XRGraphicsAutomatedTests
    {
        // XR tests can be enabled from the command line. Cache result to avoid GC.
        static bool activatedFromCommandLine
        {
#if UNITY_EDITOR
            // XRTODO: remove temporary alias when all automated tests are ready to use MockHMD
            get => Array.Exists(Environment.GetCommandLineArgs(), arg => (arg == "-xr-tests" || arg == "-xr-reuse-tests"));
#elif XR_REUSE_TESTS_STANDALONE
            get => true;
#else
            get => false;
#endif
        }

        /// <summary>
        /// Used by render pipelines to initialize XR tests.
        /// </summary>
        public static bool enabled { get; } = activatedFromCommandLine;

        /// <summary>
        /// Set by automated test framework and read by render pipelines.
        /// </summary>
        public static bool running = false;
    }
}
