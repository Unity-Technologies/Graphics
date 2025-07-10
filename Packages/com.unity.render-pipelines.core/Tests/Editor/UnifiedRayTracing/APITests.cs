using NUnit.Framework;
using System;
using UnityEditor;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.UnifiedRayTracing.Tests
{
    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class IRayTracingBackendTests
    {
        readonly RayTracingBackend m_BackendType;
        RayTracingResources m_Resources;
        IRayTracingBackend m_Backend;

        public IRayTracingBackendTests(string backendAsString)
        {
            m_BackendType = Enum.Parse<RayTracingBackend>(backendAsString);
        }

        [SetUp]
        public void SetUp()
        {
            if (!SystemInfo.supportsRayTracing && m_BackendType == RayTracingBackend.Hardware)
            {
                Assert.Ignore("Cannot run test on this Graphics API. Hardware RayTracing is not supported");
            }

            if (!SystemInfo.supportsComputeShaders && m_BackendType == RayTracingBackend.Compute)
            {
                Assert.Ignore("Cannot run test on this Graphics API. Compute shaders are not supported");
            }

            m_Resources = new RayTracingResources();
            m_Resources.Load();

            if (m_BackendType == RayTracingBackend.Hardware)
                m_Backend = new HardwareRayTracingBackend(m_Resources);
            else if (m_BackendType == RayTracingBackend.Compute)
                m_Backend = new ComputeRayTracingBackend(m_Resources);
            else
                Assert.Fail("Invalid backend type");
        }

        [Test]
        public void IRayTracingBackend_QueryScratchBufferStride_ShouldGenerateCorrectResult()
        {
            Assert.AreEqual(4, RayTracingContext.GetScratchBufferStrideInBytes());
        }

        [Test]
        public void IRayTracingBackend_QueryScratchBufferSize_ShouldGenerateCorrectResult()
        {
            if (m_BackendType == RayTracingBackend.Hardware)
                Assert.AreEqual(0, m_Backend.GetRequiredTraceScratchBufferSizeInBytes(1, 2, 3));
            else if (m_BackendType == RayTracingBackend.Compute)
                Assert.AreEqual(1536, m_Backend.GetRequiredTraceScratchBufferSizeInBytes(1, 2, 3));
        }
    }
}
