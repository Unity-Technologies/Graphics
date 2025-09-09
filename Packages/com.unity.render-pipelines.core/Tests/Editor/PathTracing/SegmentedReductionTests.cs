using NUnit.Framework;
using System;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering;

namespace UnityEngine.PathTracing.Tests
{
    internal class SegmentedReductionTests
    {
        SegmentedReduction reduction;

        [SetUp]
        public void SetUp()
        {
            reduction = new SegmentedReduction(SegmentedReduction.LoadShader());
        }

        [Test]
        [TestCase(1u,  64u,    64u)]    // Test some small problem size
        [TestCase(3u,  64u,    64u)]
        [TestCase(7u,  64u,    64u)]
        [TestCase(1u,  1697u,  1201u)]  // Test some prime numbers - these are the tricky cases
        [TestCase(3u,  1697u,  1201u)]
        [TestCase(7u,  1697u,  1201u)]
        [TestCase(1u,  193u,   43201u)] // Test some large prime numbers for size
        [TestCase(3u,  193u,   43201u)]
        [TestCase(7u,  193u,   43201u)]
        [TestCase(1u,  43201u, 193u)]   // ... and for number of sums
        [TestCase(3u,  43201u, 193u)]
        [TestCase(7u,  43201u, 193u)]
        public void SegmentedReduction_WithAnyData_MatchesReferenceImplementation(uint stride, uint numSums, uint sumSize)
        {
            // Generate some random data to sum
            System.Random r = new System.Random(1337);
            float[] sums = new float[numSums * sumSize * stride];
            for (uint i = 0; i < sums.Length; i++)
            {
                sums[i] = (float)r.NextDouble() * 10.0f;
            }

            // Super simple manual sum on CPU
            float[] referenceSums = new float[numSums * stride];
            for (uint i = 0; i < numSums; i++)
            {
                for (uint j = 0; j < sumSize; j++)
                {
                    for (int k = 0; k < stride; k++)
                    {
                        referenceSums[i * stride + k] += sums[(i * sumSize + j) * stride + k];
                    }
                }
            }

            // Now sum on GPU
            using var bufferToSum = new GraphicsBuffer(GraphicsBuffer.Target.Structured, sums.Length, sizeof(float));
            bufferToSum.SetData(sums);

            uint scratchSize = SegmentedReduction.GetScratchBufferSizeInDwords(sumSize, stride, numSums);
            using var scratchBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)scratchSize, sizeof(float));

            using var outputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, referenceSums.Length, sizeof(float));

            using var cmd = new CommandBuffer();
            reduction.TwoPassSegmentedReduction(cmd, sumSize, stride, numSums, 0, 0, bufferToSum, scratchBuffer, outputBuffer, true);
            Graphics.ExecuteCommandBuffer(cmd);

            float[] gpuSums = new float[referenceSums.Length];
            outputBuffer.GetData(gpuSums);

            // Compare results
            for (uint i = 0; i < referenceSums.Length; i++)
            {
                float difference = Math.Abs(referenceSums[i] - gpuSums[i]);
                float error = difference / referenceSums[i];
                // Floating point addition is not associative, so we can't expect perfect results.
                // We allow for a maximum error of 0.01%.
                Assert.Less(error, 0.0001f, $"Value at {i} didn't match reference result!");
            }
        }
    }
}
