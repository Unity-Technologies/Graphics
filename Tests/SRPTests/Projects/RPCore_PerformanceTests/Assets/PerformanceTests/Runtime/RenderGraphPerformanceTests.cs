using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;
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
        const int k_NumMeasurements = 80;

        readonly RenderGraph m_RenderGraph = new();
        readonly ScriptableRenderContext m_Context = new(); // NOTE: Dummy context, can't call its functions
        readonly Compiler m_Compiler;
        Camera m_Camera;

        int m_NumPasses;

        readonly ProfilingSampler k_RecordRenderGraphSampler = new ProfilingSampler("RecordRenderGraph");

        public RenderGraphPerformanceTests(Compiler compiler)
        {
            m_Compiler = compiler;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_RenderGraph.ClearCurrentCompiledGraph();
            m_RenderGraph.nativeRenderPassesEnabled = m_Compiler == Compiler.NativeRenderGraph;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_RenderGraph.Cleanup();
        }

        [SetUp]
        public void SetUp()
        {
            var camera = new GameObject("Camera", typeof(Camera));
            m_Camera = camera.GetComponent<Camera>();
            m_Camera.cameraType = CameraType.Game;
        }

        [TearDown]
        public void TearDown()
        {
            CoreUtils.Destroy(m_Camera);
            m_Camera = null;
        }

        public enum TestCase
        {
            SimplePass,
            ComplexPass,
            SimplePassWithCulledPasses
        }

        // This is a [Test] because we don't want to measure overhead from executing a full frame.
        [Test, Performance]
        public void RecordAndExecute_MeasureTotal([Values] TestCase testCase, [Values(1, 5, 15, 500)] int numPasses, [Values] RenderTextureUVOriginStrategy strategy)
        {
            m_NumPasses = numPasses;

            Measure.Method(() => TestBody(testCase, strategy))
                .WarmupCount(k_NumWarmupIterations)
                .MeasurementCount(k_NumMeasurements)
                .Run();
        }

        IEnumerable<ProfilingSampler> GetAllMarkers()
        {
            yield return k_RecordRenderGraphSampler;
            
            // High level profiling markers for Render Graph
            foreach (var val in Enum.GetValues(typeof(RenderGraphProfileId)))
                yield return ProfilingSampler.Get((RenderGraphProfileId)val);

            if (m_Compiler == Compiler.NativeRenderGraph)
            {
                // Low level profiling markers for Native Render Pass Compiler
                foreach (var val in Enum.GetValues(typeof(NativePassCompiler.NativeCompilerProfileId)))
                    yield return ProfilingSampler.Get((NativePassCompiler.NativeCompilerProfileId)val);
            }
        }

        // This is a [UnityTest] because Profiling.Recorder requires frame tick to happen.
        [UnityTest, Performance]
        public IEnumerator RecordAndExecute_MeasureProfileIds([Values] TestCase testCase, [Values(1, 5, 15, 500)] int numPasses, [Values] RenderTextureUVOriginStrategy strategy)
        {
            m_NumPasses = numPasses;

            for (var i = 0; i < k_NumWarmupIterations; i++)
            {
                TestBody(testCase, strategy);
                yield return null;
            }

            List<(SampleGroup, ProfilingSampler)> samplers = new();

            foreach (var sampler in GetAllMarkers())
            {
                samplers.Add((new SampleGroup(sampler.name), sampler));
                sampler.enableRecording = true;
            }

            for (var i = 0; i < k_NumMeasurements; i++)
            {
                TestBody(testCase, strategy);
                yield return null;

                foreach (var s in samplers)
                    Measure.Custom(s.Item1, s.Item2.inlineCpuElapsedTime);
            }

            foreach (var s in samplers)
                s.Item2.enableRecording = false;
        }

        void TestBody(TestCase testCase, RenderTextureUVOriginStrategy strategy)
        {
            RenderGraphParameters rgParams = new()
            {
                executionId = m_Camera.GetEntityId(),
                generateDebugData = m_Camera.cameraType != CameraType.Preview && !m_Camera.isProcessingRenderRequest,
                commandBuffer = new CommandBuffer(),
                scriptableRenderContext = m_Context,
                currentFrameIndex = Time.frameCount,
                invalidContextForTesting = true,
                renderTextureUVOriginStrategy = strategy
            };

            m_RenderGraph.BeginRecording(rgParams);
            switch (testCase)
            {
                case TestCase.SimplePass:
                    RecordSimplePasses();
                    break;
                case TestCase.ComplexPass:
                    RecordComplexPasses();
                    break;
                case TestCase.SimplePassWithCulledPasses:
                    RecordSimplePassesWithCulledPasses();
                    break;
                default:
                    throw new NotImplementedException();
            }

            m_RenderGraph.EndRecordingAndExecute();
        }

        class SimplePassData
        {
        }

        void RecordSimplePasses()
        {
            using (new ProfilingScope(k_RecordRenderGraphSampler))
            {
                var colorTarget = m_RenderGraph.CreateTexture(new TextureDesc(1920, 1080)
                {
                    colorFormat = GraphicsFormat.R8G8B8A8_UNorm
                });
                var depthTarget = m_RenderGraph.CreateTexture(new TextureDesc(1920, 1080)
                {
                    colorFormat = GraphicsFormat.None,
                    depthBufferBits = DepthBits.Depth32
                });

                for (int i = 0; i < m_NumPasses; ++i)
                {
                    AddSimplePass(colorTarget, depthTarget);
                }
            }
        }

        void AddSimplePass(TextureHandle colorTarget, TextureHandle depthTarget)
        {
            using (var builder = m_RenderGraph.AddRasterRenderPass<SimplePassData>("Simple Pass", out var passData))
            {
                builder.SetRenderAttachment(colorTarget, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(depthTarget, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((SimplePassData data, RasterGraphContext context) => { });
            }
        }

        void RecordSimplePassesWithCulledPasses()
        {
            var colorTarget = m_RenderGraph.CreateTexture(new TextureDesc(1920, 1080)
            {
                colorFormat = GraphicsFormat.R8G8B8A8_UNorm
            });
            var depthTarget = m_RenderGraph.CreateTexture(new TextureDesc(1920, 1080)
            {
                colorFormat = GraphicsFormat.None,
                depthBufferBits = DepthBits.Depth32
            });

            for (int i = 0; i < m_NumPasses; i++)
            {
                if ((i + 1) % 4 == 0) // Every 4 passes
                {
                    AddSimpleCulledPass();
                    continue;
                }

                AddSimplePass(colorTarget, depthTarget);
            }
        }

        void AddSimpleCulledPass()
        {
            using (var builder = m_RenderGraph.AddRasterRenderPass<SimplePassData>("Simple Culled Pass", out var passData))
            {
                builder.AllowPassCulling(true);
                builder.SetRenderFunc((SimplePassData data, RasterGraphContext context) => { });
            }
        }

        class ComplexPassData
        {
        }

        class TestRenderTargets
        {
            public TextureHandle backBuffer;
            public TextureHandle depthBuffer;
            public TextureHandle[] shadowMaps = new TextureHandle[3];
            public TextureHandle[] colorMaps = new TextureHandle[2];
            public TextureHandle[] colorMaps1 = new TextureHandle[2];
            public TextureHandle[] colorMaps2 = new TextureHandle[2];
        };

        void RecordComplexPasses()
        {
            using (new ProfilingScope(k_RecordRenderGraphSampler))
            {
                // Create textures, import them, then create render graph intermediates.
                // Create some dependencies between passes.

                TestRenderTargets ImportAndCreateRenderTargets(RenderGraph g)
                {
                    TestRenderTargets result = new TestRenderTargets();
                    var backBuffer = BuiltinRenderTextureType.CameraTarget;
                    var backBufferHandle = RTHandles.Alloc(backBuffer, "Backbuffer Color");
                    var depthBuffer = BuiltinRenderTextureType.Depth;
                    var depthBufferHandle = RTHandles.Alloc(depthBuffer, "Backbuffer Depth");

                    ImportResourceParams importParams = new ImportResourceParams();
                    importParams.textureUVOrigin = TextureUVOrigin.TopLeft;

                    RenderTargetInfo importInfo = new RenderTargetInfo();
                    RenderTargetInfo importInfoDepth = new RenderTargetInfo();
                    importInfo.width = 1920;
                    importInfo.height = 1080;
                    importInfo.volumeDepth = 1;
                    importInfo.msaaSamples = 1;
                    importInfo.format = GraphicsFormat.R16G16B16A16_SFloat;
                    result.backBuffer = g.ImportTexture(backBufferHandle, importInfo, importParams);

                    importInfoDepth = importInfo;
                    importInfoDepth.format = GraphicsFormat.D32_SFloat_S8_UInt;
                    result.depthBuffer = g.ImportTexture(depthBufferHandle, importInfoDepth, importParams);

                    importInfoDepth.format = GraphicsFormat.D24_UNorm;
                    importInfoDepth.width = 1024;
                    importInfoDepth.height = 1024;
                    for (int i = 0; i < result.shadowMaps.Length; ++i)
                    {
                        result.shadowMaps[i] = m_RenderGraph.CreateTexture(new TextureDesc(1024, 1024)
                        {
                            colorFormat = GraphicsFormat.D32_SFloat,
                            name = $"ShadowMap {i}"
                        });
                    }

                    for (int i = 0; i < result.colorMaps.Length; ++i)
                    {
                        result.colorMaps[i] = m_RenderGraph.CreateTexture(new TextureDesc(1920, 1080)
                        {
                            colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                            name = $"ColorMap {i}"
                        });
                    }
                    for (int i = 0; i < result.colorMaps1.Length; ++i)
                    {
                        result.colorMaps1[i] = m_RenderGraph.CreateTexture(new TextureDesc(1920, 1080)
                        {
                            colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                            name = $"ColorMap1 {i}"
                        });
                    }
                    for (int i = 0; i < result.colorMaps2.Length; ++i)
                    {
                        result.colorMaps2[i] = m_RenderGraph.CreateTexture(new TextureDesc(1920, 1080)
                        {
                            colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                            name = $"ColorMap2 {i}"
                        });
                    }
                    return result;
                }

                TestRenderTargets targets = ImportAndCreateRenderTargets(m_RenderGraph);

                void AddComplexPass(TestRenderTargets targets, int index, int input, int output)
                {
                    using (var builder = m_RenderGraph.AddRasterRenderPass<ComplexPassData>($"Complex Pass {index}", out var passData))
                    {
                        if (output < 0)
                        {
                            builder.SetRenderAttachment(targets.backBuffer, 0, AccessFlags.Write);
                        }
                        else
                        {
                            builder.SetRenderAttachment(targets.colorMaps[output], 0, AccessFlags.Write);
                            builder.SetRenderAttachment(targets.colorMaps1[output], 1, AccessFlags.Write);
                            builder.SetRenderAttachment(targets.colorMaps2[output], 2, AccessFlags.Write);
                        }
                        builder.SetRenderAttachmentDepth(targets.depthBuffer, AccessFlags.Write);
                        if (input >= 0)
                        {
                            builder.SetInputAttachment(targets.colorMaps[input], 0, AccessFlags.Read);
                            builder.SetInputAttachment(targets.colorMaps1[input], 1, AccessFlags.Read);
                            builder.SetInputAttachment(targets.colorMaps2[input], 2, AccessFlags.Read);
                        }
                        for (int i = 0; i < targets.shadowMaps.Length; ++i)
                        {
                            builder.UseTexture(targets.shadowMaps[i], AccessFlags.Read);
                        }
                        builder.AllowPassCulling(false);
                        builder.SetRenderFunc(static (ComplexPassData data, RasterGraphContext context) => { });
                    }
                }

                // Add some shadow passes.
                for (int i = 0; i < targets.shadowMaps.Length; ++i)
                {
                    using (var builder = m_RenderGraph.AddRasterRenderPass<ComplexPassData>("Shadow Pass", out var passData))
                    {
                        builder.SetRenderAttachmentDepth(targets.shadowMaps[i], AccessFlags.Write);
                        builder.AllowPassCulling(false);
                        builder.SetRenderFunc(static (ComplexPassData data, RasterGraphContext context) => { });
                    }
                }

                AddComplexPass(targets, -1, -1, 1);

                for (int i = 0; i < m_NumPasses; i++)
                {
                    AddComplexPass(targets, i, (i + 1) & 1, i & 1);
                }
                AddComplexPass(targets, m_NumPasses, m_NumPasses & 1, -1);
            }
        }
    }
}
