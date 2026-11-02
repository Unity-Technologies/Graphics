using NUnit.Framework;
using System;
using UnityEditor.Embree;

namespace UnityEngine.Rendering.UnifiedRayTracing.Tests
{
    internal class GpuBvhBuildTests
    {
        const int k_InternalNodeSizeInDwords = 16;
        const int k_LeafNodeSizeInDwords = 4;

        static GpuBvhBuildOptions ValidOptions()
        {
            return new GpuBvhBuildOptions
            {
                quality = GpuBvhBuildQuality.Medium,
                minLeafSize = 1,
                maxLeafSize = 4,
                allowPrimitiveSplits = false,
                isTopLevel = false
            };
        }

        static GpuBvhPrimitiveDescriptor[] CreatePrimitives(int count)
        {
            var prims = new GpuBvhPrimitiveDescriptor[count];
            for (int i = 0; i < count; ++i)
            {
                prims[i].primID = (uint)i;
                prims[i].lowerBound = new Vector3(2.0f * i, 0.0f, 0.0f);
                prims[i].upperBound = new Vector3(2.0f * i + 1.0f, 1.0f, 1.0f);
            }

            return prims;
        }

        [Test]
        public void Build_ValidOptionsAndEightPrimitives_ReturnsConsistentBvh()
        {
            var result = GpuBvh.Build(ValidOptions(), CreatePrimitives(8));

            Assert.IsNotNull(result);
            uint internalNodeCount = result[0];
            uint leafNodeCount = result[1];
            Assert.Greater(internalNodeCount, 0u);
            Assert.AreEqual(8u, leafNodeCount);
            Assert.AreEqual(
                k_InternalNodeSizeInDwords * (internalNodeCount + 1) + k_LeafNodeSizeInDwords * leafNodeCount,
                (uint)result.Length);
        }

        [Test]
        public void Build_EmptyPrimitiveSpan_ReturnsEmptyResult()
        {
            var result = GpuBvh.Build(ValidOptions(), Array.Empty<GpuBvhPrimitiveDescriptor>());

            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [Test]
        public void Build_DefaultConstructedOptions_ThrowsArgumentException()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => GpuBvh.Build(new GpuBvhBuildOptions(), CreatePrimitives(8)));

            Assert.That(exception.Message, Does.Contain(nameof(GpuBvhBuildOptions.maxLeafSize)));
        }
    }
}
