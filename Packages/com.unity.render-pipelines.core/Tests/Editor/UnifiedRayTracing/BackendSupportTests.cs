using NUnit.Framework;

namespace UnityEngine.Rendering.UnifiedRayTracing.Tests
{
    internal class BackendSupportTests
    {
        [Test]
        public void GetCapabilities_HardwareBackend_MatchesSystemInfo()
        {
            var features = RayTracingContext.GetCapabilities(RayTracingBackend.Hardware);

            Assert.AreEqual(SystemInfo.supportsRayTracing, features != CapabilityMask.None);
            if (features != CapabilityMask.None)
            {
                Assert.AreEqual(SystemInfo.supportsRayTracingShaders, features.HasFlag(CapabilityMask.RayTracingShaders));
                Assert.AreEqual(SystemInfo.supportsInlineRayTracing, features.HasFlag(CapabilityMask.InlineRayTracing));
                Assert.AreEqual(SystemInfo.supportsIndirectDispatchRays, features.HasFlag(CapabilityMask.RayTracingDispatchIndirect));
            }
        }

        [Test]
        public void GetCapabilities_ComputeBackend_MatchesSystemInfo()
        {
            var features = RayTracingContext.GetCapabilities(RayTracingBackend.Compute);

            Assert.AreEqual(SystemInfo.supportsComputeShaders, features != CapabilityMask.None);
            if (features != CapabilityMask.None)
            {
                Assert.AreEqual(
                    CapabilityMask.RayTracingShaders | CapabilityMask.RayTracingDispatchIndirect | CapabilityMask.InlineRayTracing,
                    features);
            }
        }

        [Test]
        public void GetCapabilities_UndefinedBackend_ReturnsNone()
        {
            Assert.AreEqual(CapabilityMask.None, RayTracingContext.GetCapabilities((RayTracingBackend)(-1)));
        }

        [Test]
        public void IsBackendSupported_MatchesGetCapabilities()
        {
#pragma warning disable 618
            Assert.AreEqual(RayTracingContext.GetCapabilities(RayTracingBackend.Hardware) != CapabilityMask.None, RayTracingContext.IsBackendSupported(RayTracingBackend.Hardware));
            Assert.AreEqual(RayTracingContext.GetCapabilities(RayTracingBackend.Compute) != CapabilityMask.None, RayTracingContext.IsBackendSupported(RayTracingBackend.Compute));
#pragma warning restore 618
        }
    }
}
