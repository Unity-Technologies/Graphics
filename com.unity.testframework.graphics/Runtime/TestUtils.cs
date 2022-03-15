using UnityEngine.Rendering;

namespace UnityEngine.TestTools.Graphics
{
    public static class TestUtils
    {
        /// <summary>
        /// Returns the test result folder path for the specified test configuration.
        /// </summary>
        /// <remarks>Combine this path with either the Reference Images root or Actual Images root to get the full path.</remarks>
        /// <param name="colorSpace">The color space setting for the current test</param>
        /// <param name="runtimePlatform">The runtime platform setting for the current test</param>
        /// <param name="graphicsApi">The graphics API the current test is running</param>
        /// <param name="xrsdk">The XR SDK for the current test</param>
        public static string GetTestResultsFolderPath(ColorSpace colorSpace, RuntimePlatform runtimePlatform, GraphicsDeviceType graphicsApi, string xrsdk = "None")
            => $"{colorSpace}/{runtimePlatform.ToUniqueString()}/{graphicsApi}/{xrsdk}";

        /// <summary>
        /// Returns the test result folder path for the current test configuration.
        /// </summary>
        /// <remarks>Combine this path with either the Reference Images root or Actual Images root to get the full path.</remarks>
        public static string GetCurrentTestResultsFolderPath()
            => GetTestResultsFolderPath(UseGraphicsTestCasesAttribute.ColorSpace, UseGraphicsTestCasesAttribute.Platform, UseGraphicsTestCasesAttribute.GraphicsDevice, UseGraphicsTestCasesAttribute.LoadedXRDevice);
    }
}
