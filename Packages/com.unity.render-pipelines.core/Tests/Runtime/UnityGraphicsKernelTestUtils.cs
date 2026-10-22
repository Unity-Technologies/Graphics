namespace UnityEngine.Rendering.Tests
{
    static class UnityGraphicsKernelTestUtils
    {
        static bool? s_IsForcedUGK;

        // The graphics device can't change during a run, so this is evaluated once.
        internal static bool IsForcedUGK() => s_IsForcedUGK ??= IsUGKRequested() || IsUGKActive();

        static bool IsUGKRequested() =>
            System.Array.Exists(System.Environment.GetCommandLineArgs(),
                arg => arg.Equals("-force-ugk", System.StringComparison.OrdinalIgnoreCase));

        // On iOS the managed command line doesn't contain the player args, so also check the device version.
        static bool IsUGKActive() => SystemInfo.graphicsDeviceVersion.EndsWith("(UGK)", System.StringComparison.Ordinal);
    }
}
