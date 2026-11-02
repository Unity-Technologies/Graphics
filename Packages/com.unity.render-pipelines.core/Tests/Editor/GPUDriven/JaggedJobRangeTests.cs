#if !UNITY_WEBGL_RENDERER_ONLY
using NUnit.Framework;
using Unity.Collections;

namespace UnityEngine.Rendering.Tests
{
    class JaggedJobRangeTests
    {
        const int kBatchSizeHint = 64;

        static int SumRangeLengths(in NativeList<JaggedJobRange> ranges)
        {
            int total = 0;
            for (int i = 0; i < ranges.Length; i++)
                total += ranges[i].length;
            return total;
        }

        [Test]
        public void ComputeRanges_NoSections_ReturnsCreatedEmptyList()
        {
            using var span = new JaggedSpan<int>(1, Allocator.Temp);

            var ranges = JaggedJobRange.FromSpanWithMaxBatchSize(span, kBatchSizeHint, Allocator.TempJob);
            try
            {
                Assert.IsTrue(ranges.IsCreated);
                Assert.AreEqual(0, ranges.Length);
            }
            finally
            {
                if (ranges.IsCreated)
                    ranges.Dispose();
            }
        }

        [Test]
        public void ComputeRanges_SectionsPresentButAllEmpty_ReturnsCreatedEmptyList()
        {
            using var emptySection = new NativeArray<int>(0, Allocator.Temp);
            using var span = new JaggedSpan<int>(2, Allocator.Temp);
            span.Add(emptySection);
            span.Add(emptySection);

            Assert.AreEqual(2, span.sectionCount);
            Assert.AreEqual(0, span.totalLength);

            var ranges = JaggedJobRange.FromSpanWithMaxBatchSize(span, kBatchSizeHint, Allocator.TempJob);
            try
            {
                Assert.IsTrue(ranges.IsCreated);
                Assert.AreEqual(0, ranges.Length);
            }
            finally
            {
                if (ranges.IsCreated)
                    ranges.Dispose();
            }
        }

        [Test]
        public void ComputeRanges_RelaxedBatchSize_SectionsPresentButAllEmpty_ReturnsCreatedEmptyList()
        {
            using var emptySection = new NativeArray<int>(0, Allocator.Temp);
            using var span = new JaggedSpan<int>(1, Allocator.Temp);
            span.Add(emptySection);

            var ranges = JaggedJobRange.FromSpanWithRelaxedBatchSize(span, kBatchSizeHint, Allocator.TempJob);
            try
            {
                Assert.IsTrue(ranges.IsCreated);
                Assert.AreEqual(0, ranges.Length);
            }
            finally
            {
                if (ranges.IsCreated)
                    ranges.Dispose();
            }
        }

        [Test]
        public void ComputeRanges_EmptyAndPopulatedSectionsMixed_CoversEveryElement()
        {
            const int kPopulatedLength = 100;

            using var emptySection = new NativeArray<int>(0, Allocator.Temp);
            using var populatedSection = new NativeArray<int>(kPopulatedLength, Allocator.Temp);
            using var span = new JaggedSpan<int>(3, Allocator.Temp);
            span.Add(emptySection);
            span.Add(populatedSection);
            span.Add(emptySection);

            Assert.AreEqual(kPopulatedLength, span.totalLength);

            var ranges = JaggedJobRange.FromSpanWithMaxBatchSize(span, kBatchSizeHint, Allocator.TempJob);
            try
            {
                Assert.IsTrue(ranges.IsCreated);
                Assert.Greater(ranges.Length, 0);
                Assert.AreEqual(span.totalLength, SumRangeLengths(ranges));

                for (int i = 0; i < ranges.Length; i++)
                    Assert.LessOrEqual(ranges[i].length, kBatchSizeHint);
            }
            finally
            {
                if (ranges.IsCreated)
                    ranges.Dispose();
            }
        }
    }
}
#endif
