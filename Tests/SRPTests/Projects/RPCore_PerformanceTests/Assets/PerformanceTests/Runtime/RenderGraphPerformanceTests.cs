using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace PerformanceTests.Runtime
{
    public enum Compiler
    {
        RenderGraph,
        NativeRenderGraph
    }

    [TestFixture(Compiler.RenderGraph)]
    [TestFixture(Compiler.NativeRenderGraph)]
    public class RenderGraphPerformanceTests
    {
        const int k_NumWarmupIterations = 5;
        const int k_NumMeasurements = 20;

        readonly RenderGraph m_RenderGraph = new();
        readonly ScriptableRenderContext m_Context = new(); // NOTE: Dummy context, can't call its functions
        readonly Compiler m_Compiler;

        int m_NumPasses;

        public RenderGraphPerformanceTests(Compiler compiler)
        {
            m_Compiler = compiler;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_RenderGraph.ClearCompiledGraph();
            m_RenderGraph.NativeRenderPassesEnabled = m_Compiler == Compiler.NativeRenderGraph;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_RenderGraph.Cleanup();
        }

        public enum TestCase
        {
            SimplePass
        }

        // This is a [Test] because we don't want to measure overhead from executing a full frame.
        [Test, Performance]
        public void RecordAndExecute_MeasureTotal([Values] TestCase testCase, [Values(1, 5, 15)] int numPasses)
        {
            m_NumPasses = numPasses;

            Measure.Method(() => TestBody(testCase))
                .WarmupCount(k_NumWarmupIterations)
                .MeasurementCount(k_NumMeasurements)
                .Run();
        }

        // This is a [UnityTest] because Profiling.Recorder requires frame tick to happen.
        [UnityTest, Performance]
        public IEnumerator RecordAndExecute_MeasureProfileIds([Values] TestCase testCase, [Values(1, 5, 15)] int numPasses)
        {
            m_NumPasses = numPasses;

            for (var i = 0; i < k_NumWarmupIterations; i++)
            {
                TestBody(testCase);
                yield return null;
            }

            List<(SampleGroup, ProfilingSampler)> samplers = new();

            ProfilingSampler GetSampler(object profilerId) => m_Compiler switch
            {
                Compiler.NativeRenderGraph => ProfilingSampler.Get((NativePassCompiler.NativeCompilerProfileId) profilerId),
                Compiler.RenderGraph => ProfilingSampler.Get((RenderGraphProfileId) profilerId),
            };

            Type profileIdEnumType = m_Compiler == Compiler.NativeRenderGraph
                ? typeof(NativePassCompiler.NativeCompilerProfileId)
                : typeof(RenderGraphProfileId);

            foreach (var profilerId in Enum.GetValues(profileIdEnumType))
            {
                var sampler = GetSampler(profilerId);
                samplers.Add((new SampleGroup(sampler.name), sampler));
                sampler.enableRecording = true;
            }

            for (var i = 0; i < k_NumMeasurements; i++)
            {
                TestBody(testCase);
                yield return null;

                foreach (var s in samplers)
                    Measure.Custom(s.Item1, s.Item2.inlineCpuElapsedTime);
            }

            foreach (var s in samplers)
               s.Item2.enableRecording = false;
        }

        void TestBody(TestCase testCase)
        {
            RenderGraphParameters rgParams = new()
            {
                commandBuffer = new CommandBuffer(),
                scriptableRenderContext = m_Context,
                currentFrameIndex = Time.frameCount,
                invalidContextForTesting = true
            };

            m_RenderGraph.BeginRecording(rgParams);
            switch (testCase)
            {
                case TestCase.SimplePass:
                    AddSimplePasses();
                    break;
                default:
                    throw new NotImplementedException();
            }

            m_RenderGraph.EndRecordingAndExecute();
        }

        class SimplePassData
        {
        }

        void AddSimplePasses()
        {
            var colorTarget = m_RenderGraph.CreateTexture(new TextureDesc(1920, 1080)
            {
                colorFormat = GraphicsFormat.R8G8B8A8_UNorm
            });
            var depthTarget = m_RenderGraph.CreateTexture(new TextureDesc(1920, 1080)
            {
                colorFormat = GraphicsFormat.D32_SFloat_S8_UInt,
                depthBufferBits = DepthBits.Depth32
            });

            void AddSimplePass()
            {
                using (var builder = m_RenderGraph.AddRasterRenderPass<SimplePassData>("Simple Pass", out var passData))
                {
                    builder.UseTextureFragment(colorTarget, 0, AccessFlags.Write);
                    builder.UseTextureFragmentDepth(depthTarget, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((SimplePassData data, RasterGraphContext context) => { });
                }
            }

            for (int i = 0; i < m_NumPasses; i++)
            {
                AddSimplePass();
            }
        }
    }
}
