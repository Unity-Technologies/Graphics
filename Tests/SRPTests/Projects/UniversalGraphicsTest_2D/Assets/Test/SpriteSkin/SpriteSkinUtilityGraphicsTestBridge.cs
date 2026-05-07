using UnityEngine.U2D.Animation;

namespace Unity.U2D.Animation.Tests.RuntimeTests
{
    /// <summary>
    /// Calls <see cref="SpriteSkinUtility.SetUsingGpuDeformationForTest"/> for graphics tests. This assembly is
    /// named to match <c>InternalsVisibleTo</c> on <c>Unity.2D.Animation.Runtime</c>.
    /// </summary>
    public static class SpriteSkinUtilityGraphicsTestBridge
    {
        public static void SetUsingGpuDeformationForTest(bool useGpu) =>
            SpriteSkinUtility.SetUsingGpuDeformationForTest(useGpu);
    }
}
