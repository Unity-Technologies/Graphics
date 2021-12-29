using System;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class GpuPrefixSumTests
    {
        private GpuPrefixSum m_PrefixSumSystem;

        [SetUp]
        public void OnSetup()
        {
#if UNITY_EDITOR
            var globalSettings = HDRenderPipelineGlobalSettings.Ensure();
#else
            var globalSettings = HDRenderPipelineGlobalSettings.instance;
#endif
            m_PrefixSumSystem.Initialize(globalSettings.renderPipelineResources);
        }

        [TearDown]
        public void OnTeardown()
        {
            m_PrefixSumSystem.Dispose();
        }

        ComputeBuffer CreateBuffer(uint[] numbers)
        {
            ComputeBuffer buffer = new ComputeBuffer(numbers.Length, 4, ComputeBufferType.Raw);
            buffer.SetData(numbers);
            return buffer;
        }

        uint[] DownloadData(ComputeBuffer buffer)
        {
            CommandBuffer cmdBuffer = new CommandBuffer();
            uint[] outBuffer = null;
            cmdBuffer.RequestAsyncReadback(buffer, (AsyncGPUReadbackRequest req) =>
            {
                if (req.done)
                {
                    var data = req.GetData<uint>();
                    outBuffer = data.ToArray();
                }
            });
            cmdBuffer.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmdBuffer);

            return outBuffer;
        }

        uint[] CpuPrefixSum(uint[] input, bool isExclusive = false)
        {
            uint[] output = new uint[input.Length];
            uint sum = 0;
            if (isExclusive)
            {
                for (int i = 0; i < input.Length; ++i)
                {
                    output[i] = sum;
                    sum += input[i];
                }
            }
            else
            {
                for (int i = 0; i < input.Length; ++i)
                {
                    sum += input[i];
                    output[i] = sum;
                }
            }
            return output;
        }

        uint[] CreateInputArray0(int count)
        {
            uint[] output = new uint[Math.Max(count, 1)];
            for (int i = 0; i < output.Length; ++i)
                output[i] = (uint)(i * 2) + 1;
            return output;
        }

        bool TestCompareArrays(uint[] a, uint[] b, int offset = 0, int length = -1)
        {
            Assert.Less(offset, a.Length);
            Assert.Less(offset, b.Length);
            if (offset >= a.Length || offset >= b.Length)
                return false;

            int endIdx = 0;
            if (length < 0)
            {
                Assert.IsTrue(a.Length == b.Length);
                if (a.Length != b.Length)
                {
                    return false;
                }
                endIdx = a.Length;
            }
            else
            {
                endIdx = offset + length;
                Assert.GreaterOrEqual(a.Length, endIdx);
                Assert.GreaterOrEqual(b.Length, endIdx);
            }

            for (int i = offset; i < endIdx; ++i)
            {
                if (a[i] != b[i])
                {
                    Assert.Fail("Mismatching array: a[{0}]={1} and b[{0}]={2}.", i, a[i], b[i]);
                    return false;
                }
            }

            return true;
        }

        void ClearOutput(GpuPrefixSumSupportResources resources)
        {
            uint[] zeroArray = new uint[resources.maxBufferCount];
            resources.output.SetData(zeroArray);
        }

        public void TestPrefixSumDirectCommon(int bufferCount, bool isExclusive = false)
        {
            uint[] inputArray = CreateInputArray0(bufferCount);
            var inputBuffer = CreateBuffer(inputArray);

            CommandBuffer cmdBuffer = new CommandBuffer();
            //allocate slack memory
            var resources = GpuPrefixSumSupportResources.Create(Math.Max(inputArray.Length, 2 * GpuPrefixSumDefs.GroupSize));

            //Clear the output
            ClearOutput(resources);

            var arguments = new GpuPrefixSumDirectArgs();
            arguments.exclusive = isExclusive;
            arguments.input = inputBuffer;
            arguments.inputCount = inputArray.Length;
            arguments.supportResources = resources;
            m_PrefixSumSystem.DispatchDirect(cmdBuffer, arguments);

            Graphics.ExecuteCommandBuffer(cmdBuffer);

            var referenceOutput = CpuPrefixSum(inputArray, isExclusive);
            var results = DownloadData(arguments.supportResources.output);

            TestCompareArrays(referenceOutput, results, 0, bufferCount);

            cmdBuffer.Release();
            inputBuffer.Dispose();
            resources.Dispose();
        }

        public void TestPrefixSumIndirectCommon(int bufferCount, bool isExclusive = false)
        {
            uint[] inputArray = CreateInputArray0(bufferCount);
            var inputBuffer = CreateBuffer(inputArray);

            var countBuffer = CreateBuffer(new uint[] { (uint)bufferCount });

            CommandBuffer cmdBuffer = new CommandBuffer();
            //allocate slack memory
            var resources = GpuPrefixSumSupportResources.Create(Math.Max(inputArray.Length, 2 * GpuPrefixSumDefs.GroupSize));

            //Clear the output
            ClearOutput(resources);

            var arguments = new GpuPrefixSumIndirectDirectArgs();
            arguments.exclusive = isExclusive;
            arguments.input = inputBuffer;
            arguments.inputCountBuffer = countBuffer;
            arguments.inputCountBufferByteOffset = 0;
            arguments.supportResources = resources;
            m_PrefixSumSystem.DispatchIndirect(cmdBuffer, arguments);

            Graphics.ExecuteCommandBuffer(cmdBuffer);

            var referenceOutput = CpuPrefixSum(inputArray, isExclusive);
            var results = DownloadData(arguments.supportResources.output);
            var buff1 = DownloadData(arguments.supportResources.prefixBuffer1);
            var buff2 = DownloadData(arguments.supportResources.totalLevelCountBuffer);

            TestCompareArrays(referenceOutput, results, 0, bufferCount);

            cmdBuffer.Release();
            inputBuffer.Dispose();
            countBuffer.Dispose();
            resources.Dispose();
        }

        [Test]
        public void TestPrefixSumOnSingleGroup()
        {
            TestPrefixSumDirectCommon(GpuPrefixSumDefs.GroupSize);
        }

        [Test]
        public void TestPrefixSumOnSingleGroupExclusive()
        {
            TestPrefixSumDirectCommon(GpuPrefixSumDefs.GroupSize, isExclusive: true);
        }


        [Test]
        public void TestPrefixSumIndirectOnSingleGroup()
        {
            TestPrefixSumIndirectCommon(GpuPrefixSumDefs.GroupSize);
        }

        [Test]
        public void TestPrefixSumIndirectOnSingleGroupExclusive()
        {
            TestPrefixSumIndirectCommon(GpuPrefixSumDefs.GroupSize, isExclusive: true);
        }

        [Test]
        public void TestPrefixSumIndirectOnSubGroup()
        {
            TestPrefixSumIndirectCommon(GpuPrefixSumDefs.GroupSize - 10);
        }

        [Test]
        public void TestPrefixSumIndirectOnSubGroupExclusive()
        {
            TestPrefixSumIndirectCommon(GpuPrefixSumDefs.GroupSize - 10, isExclusive: true);
        }

        [Test]
        public void TestPrefixSumIndirectOnBigArray()
        {
            TestPrefixSumIndirectCommon(913);
        }

        [Test]
        public void TestPrefixSumIndirectOnBigArrayExclusive()
        {
            TestPrefixSumIndirectCommon(913, isExclusive: true);
        }

        [Test]
        public void TestPrefixSumIndirectOnZero()
        {
            TestPrefixSumIndirectCommon(0);
        }

        [Test]
        public void TestPrefixSumIndirectOnZeroExclusive()
        {
            TestPrefixSumIndirectCommon(0, isExclusive: true);
        }

        [Test]
        public void TestPrefixSumIndirectOnOne()
        {
            TestPrefixSumIndirectCommon(1);
        }

        [Test]
        public void TestPrefixSumIndirectOnOneExclusive()
        {
            TestPrefixSumIndirectCommon(1, isExclusive: true);
        }
    }
}
