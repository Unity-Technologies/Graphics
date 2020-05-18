using System;

// need to add proper doc !

namespace UnityEngine.Rendering
{
    public static class XRGraphicsAutomatedTests
    {
        // XR tests can be enabled from the command line. Cache result to avoid GC.
        static bool activatedFromCommandLine { get => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-xr-tests"); }

        // TODO: rename to ??? initialized ?
        // Used by render pipelines to initialize XR tests.
        public static bool enabled { get; } = activatedFromCommandLine;

        // Set by automated test framework and read by render pipelines.
        public static bool running = false;
    }
}
