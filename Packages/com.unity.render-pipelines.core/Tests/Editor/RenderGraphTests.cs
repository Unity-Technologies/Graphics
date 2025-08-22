using NUnit.Framework;
using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using System.Collections.Generic;
using UnityEngine.TestTools;
using Unity.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif
namespace UnityEngine.Rendering.Tests
{
    [InitializeOnLoad]
    class RenderGraphTestsOnLoad
    {
        static bool IsGraphicsAPISupported()
        {
            var gfxAPI = SystemInfo.graphicsDeviceType;
            if (gfxAPI == GraphicsDeviceType.OpenGLCore)
                return false;
            return true;
        }

        static RenderGraphTestsOnLoad()
        {
            ConditionalIgnoreAttribute.AddConditionalIgnoreMapping("IgnoreGraphicsAPI", !IsGraphicsAPISupported());
        }
    }

    class RenderGraphTests
    {
        // For RG Record/Hash/Compile testing, use m_RenderGraph
        RenderGraph m_RenderGraph;

        RenderPipelineAsset m_OldDefaultRenderPipeline;
        RenderPipelineAsset m_OldQualityRenderPipeline;

        // For RG Execute/Submit testing with rendering, use m_RenderGraphTestPipeline and m_RenderGraph in its recordRenderGraphBody
        RenderGraphTestPipelineAsset m_RenderGraphTestPipeline;
        RenderGraphTestGlobalSettings m_RenderGraphTestGlobalSettings;

        // We need a camera to execute the render graph and a game object to attach a camera
        GameObject m_GameObject;
        Camera m_Camera;

        // For the testing of the following RG steps: Execute and Submit (native) with camera rendering, use this custom RenderGraph render pipeline
        // through a camera render call to test the RG with a real ScriptableRenderContext
        class RenderGraphTestPipelineAsset : RenderPipelineAsset<RenderGraphTestPipelineInstance>
        {
            public Action<ScriptableRenderContext, Camera, CommandBuffer> recordRenderGraphBody;
            public RenderGraph renderGraph;
            protected override RenderPipeline CreatePipeline()
            {
                return new RenderGraphTestPipelineInstance(this);
            }

            // Called only once per UTR
            void OnEnable()
            {
                renderGraph = new();
            }
        }

        class RenderGraphTestPipelineInstance : RenderPipeline
        {
            RenderGraphTestPipelineAsset asset;

            // Having the RG at this level allows us to handle RG framework within Render() for easier testing
            RenderGraph m_RenderGraph;

            public RenderGraphTestPipelineInstance(RenderGraphTestPipelineAsset asset)
            {
                this.asset = asset;
                this.m_RenderGraph = asset.renderGraph;
            }

            protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
            {
                foreach (var camera in cameras)
                {
                    if (!camera.enabled)
                        continue;

                    var cmd = CommandBufferPool.Get();

                    RenderGraphParameters rgParams = new()
                    {
                        commandBuffer = cmd,
                        scriptableRenderContext = renderContext,
                        currentFrameIndex = Time.frameCount,
                        invalidContextForTesting = false
                    };

                    m_RenderGraph.BeginRecording(rgParams);

                    asset.recordRenderGraphBody?.Invoke(renderContext, camera, cmd);

                    m_RenderGraph.EndRecordingAndExecute();

                    renderContext.ExecuteCommandBuffer(cmd);

                    CommandBufferPool.Release(cmd);
                }
                renderContext.Submit();
            }
        }

        [SupportedOnRenderPipeline(typeof(RenderGraphTestPipelineAsset))]
        [System.ComponentModel.DisplayName("RenderGraphTest")]
        class RenderGraphTestGlobalSettings : RenderPipelineGlobalSettings<RenderGraphTestGlobalSettings, RenderGraphTestPipelineInstance>
        {
            [SerializeField] RenderPipelineGraphicsSettingsContainer m_Settings = new();
            protected override List<IRenderPipelineGraphicsSettings> settingsList => m_Settings.settingsList;
        }

        [OneTimeSetUp]
        public void Setup()
        {
            // Setting default global settings to the custom RG render pipeline type, no quality settings so we can rely on the default RP
            m_RenderGraphTestGlobalSettings = ScriptableObject.CreateInstance<RenderGraphTestGlobalSettings>();
#if UNITY_EDITOR
            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<RenderGraphTestPipelineInstance>(m_RenderGraphTestGlobalSettings);
#endif
            // Saving old render pipelines to set them back after testing
            m_OldDefaultRenderPipeline = GraphicsSettings.defaultRenderPipeline;
            m_OldQualityRenderPipeline = QualitySettings.renderPipeline;

            // Setting the custom RG render pipeline
            m_RenderGraphTestPipeline = ScriptableObject.CreateInstance<RenderGraphTestPipelineAsset>();
            GraphicsSettings.defaultRenderPipeline = m_RenderGraphTestPipeline;
            QualitySettings.renderPipeline = m_RenderGraphTestPipeline;

            // Getting the RG from the custom asset pipeline
            m_RenderGraph = m_RenderGraphTestPipeline.renderGraph;

            m_RenderGraph.nativeRenderPassesEnabled = true;
            
            // We need a real ScriptableRenderContext and a camera to execute the Render Graph
            m_GameObject = new GameObject("testGameObject")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            m_GameObject.tag = "MainCamera";
            m_Camera = m_GameObject.AddComponent<Camera>();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            GraphicsSettings.defaultRenderPipeline = m_OldDefaultRenderPipeline;
            m_OldDefaultRenderPipeline = null;

            QualitySettings.renderPipeline = m_OldQualityRenderPipeline;
            m_OldQualityRenderPipeline = null;

            m_RenderGraph.Cleanup();

            Object.DestroyImmediate(m_RenderGraphTestPipeline);

#if UNITY_EDITOR
            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<RenderGraphTestPipelineInstance>(null);
#endif
            Object.DestroyImmediate(m_RenderGraphTestGlobalSettings);

            GameObject.DestroyImmediate(m_GameObject);
            m_GameObject = null;
            m_Camera = null;
        }

        [TearDown]
        public void CleanupRenderGraph()
        {
            // Cleaning all Render Graph resources and data structures
            // Nothing remains, Render Graph in next test will start from scratch
            m_RenderGraph.Cleanup();
        }

        class RenderGraphTestPassData
        {
            public TextureHandle[] textures = new TextureHandle[8];
            public BufferHandle[] buffers = new BufferHandle[8];
        }

        // Final output (back buffer) of render graph needs to be explicitly imported in order to know that the chain of dependency should not be culled.
        [Test]
        public void WriteToBackBufferNotCulled()
        {
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(1, compiledPasses.size);
            Assert.AreEqual(false, compiledPasses[0].culled);
        }

        // If no back buffer is ever written to, everything should be culled.
        [Test]
        public void NoWriteToBackBufferCulled()
        {
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(1, compiledPasses.size);
            Assert.AreEqual(true, compiledPasses[0].culled);
        }

        // Writing to imported resource is considered as a side effect so passes should not be culled.
        [Test]
        public void WriteToImportedTextureNotCulled()
        {
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.WriteTexture(m_RenderGraph.ImportTexture(null));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(1, compiledPasses.size);
            Assert.AreEqual(false, compiledPasses[0].culled);
        }

        [Test]
        public void WriteToImportedComputeBufferNotCulled()
        {
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.WriteBuffer(m_RenderGraph.ImportBuffer(null));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(1, compiledPasses.size);
            Assert.AreEqual(false, compiledPasses[0].culled);
        }

        [Test]
        public void PassWriteResourcePartialNotReadAfterNotCulled()
        {
            // If a pass writes to a resource that is not unused globally by the graph but not read ever AFTER the pass then the pass should be culled unless it writes to another used resource.
            TextureHandle texture0;
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                texture0 = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            TextureHandle texture1;
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.ReadTexture(texture0);
                texture1 = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            // This pass writes to texture0 which is used so will not be culled out.
            // Since texture0 is never read after this pass, we should decrement refCount for this pass and potentially cull it.
            // However, it also writes to texture1 which is used in the last pass so we mustn't cull it.
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.WriteTexture(texture0);
                builder.WriteTexture(texture1);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass3", out var passData))
            {
                builder.ReadTexture(texture1);
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0)); // Needed for the passes to not be culled
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(4, compiledPasses.size);
            Assert.AreEqual(false, compiledPasses[0].culled);
            Assert.AreEqual(false, compiledPasses[1].culled);
            Assert.AreEqual(false, compiledPasses[2].culled);
            Assert.AreEqual(false, compiledPasses[3].culled);
        }

        [Test]
        public void PassDisallowCullingNotCulled()
        {
            // This pass does nothing so should be culled but we explicitly disallow it.
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(1, compiledPasses.size);
            Assert.AreEqual(false, compiledPasses[0].culled);
        }

        // First pass produces two textures and second pass only read one of the two. Pass one should not be culled.
        [Test]
        public void PartialUnusedProductNotCulled()
        {
            TextureHandle texture;
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                texture = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.ReadTexture(texture);
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(2, compiledPasses.size);
            Assert.AreEqual(false, compiledPasses[0].culled);
            Assert.AreEqual(false, compiledPasses[1].culled);
        }

        // Simple cycle of create/release of a texture across multiple passes.
        [Test]
        public void SimpleCreateReleaseTexture()
        {
            TextureHandle texture;
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                texture = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            // Add dummy passes
            for (int i = 0; i < 2; ++i)
            {
                using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                {
                    builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
                }
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.ReadTexture(texture);
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0)); // Needed for the passes to not be culled
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(4, compiledPasses.size);
            Assert.Contains(texture.handle.index, compiledPasses[0].resourceCreateList[(int)RenderGraphResourceType.Texture]);
            Assert.Contains(texture.handle.index, compiledPasses[3].resourceReleaseList[(int)RenderGraphResourceType.Texture]);
        }

        [Test]
        public void UseTransientOutsidePassRaiseException()
        {
            Assert.Catch<System.ArgumentException>(() =>
            {
                TextureHandle texture;
                using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                {
                    texture = builder.CreateTransientTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
                    builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
                }

                using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                {
                    builder.ReadTexture(texture); // This is illegal (transient resource was created in previous pass)
                    builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0)); // Needed for the passes to not be culled
                    builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
                }

                m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());
            });
        }

        [Test]
        public void TransientCreateReleaseInSamePass()
        {
            TextureHandle texture;
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                texture = builder.CreateTransientTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0)); // Needed for the passes to not be culled
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(1, compiledPasses.size);
            Assert.Contains(texture.handle.index, compiledPasses[0].resourceCreateList[(int)RenderGraphResourceType.Texture]);
            Assert.Contains(texture.handle.index, compiledPasses[0].resourceReleaseList[(int)RenderGraphResourceType.Texture]);
        }

        // Texture that should be released during an async pass should have their release delayed until the first pass that syncs with the compute pipe.
        // Otherwise they may be reused by the graphics pipe even if the async pipe is not done executing.
        [Test]
        public void AsyncPassReleaseTextureOnGraphicsPipe()
        {
            TextureHandle texture0;
            TextureHandle texture1;
            TextureHandle texture2;
            TextureHandle texture3;
            // First pass creates and writes two textures.
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("Async_TestPass0", out var passData))
            {
                texture0 = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                texture1 = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.EnableAsyncCompute(true);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            // Second pass creates a transient texture => Create/Release should happen in this pass but we want to delay the release until the first graphics pipe pass that sync with async queue.
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("Async_TestPass1", out var passData))
            {
                texture2 = builder.CreateTransientTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
                builder.WriteTexture(texture0);
                builder.EnableAsyncCompute(true);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            // This pass is the last to read texture0 => Release should happen in this pass but we want to delay the release until the first graphics pipe pass that sync with async queue.
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("Async_TestPass2", out var passData))
            {
                texture0 = builder.ReadTexture(texture0);
                builder.WriteTexture(texture1);
                builder.EnableAsyncCompute(true);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            // Just here to add "padding" to the number of passes to ensure resources are not released right at the first sync pass.
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass3", out var passData))
            {
                texture3 = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.EnableAsyncCompute(false);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            // Pass prior to synchronization should be where textures are released.
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass4", out var passData))
            {
                builder.WriteTexture(texture3);
                builder.EnableAsyncCompute(false);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            // Graphics pass that reads texture1. This will request a sync with compute pipe. The previous pass should be the one releasing async textures.
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass5", out var passData))
            {
                builder.ReadTexture(texture1);
                builder.ReadTexture(texture3);
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0)); // Needed for the passes to not be culled
                builder.EnableAsyncCompute(false);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(6, compiledPasses.size);
            Assert.Contains(texture0.handle.index, compiledPasses[4].resourceReleaseList[(int)RenderGraphResourceType.Texture]);
            Assert.Contains(texture2.handle.index, compiledPasses[4].resourceReleaseList[(int)RenderGraphResourceType.Texture]);
        }

        [Test]
        public void TransientResourceNotCulled()
        {
            TextureHandle texture0;
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                texture0 = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.CreateTransientTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
                builder.WriteTexture(texture0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            // Graphics pass that reads texture1. This will request a sync with compute pipe. The previous pass should be the one releasing async textures.
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass5", out var passData))
            {
                builder.ReadTexture(texture0);
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0)); // Needed for the passes to not be culled
                builder.EnableAsyncCompute(false);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(3, compiledPasses.size);
            Assert.AreEqual(false, compiledPasses[1].culled);
        }

        [Test]
        public void AsyncPassWriteWaitOnGraphicsPipe()
        {
            TextureHandle texture0;
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                texture0 = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("Async_TestPass1", out var passData))
            {
                texture0 = builder.WriteTexture(texture0);
                builder.EnableAsyncCompute(true);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.ReadTexture(texture0);
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0)); // Needed for the passes to not be culled
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(3, compiledPasses.size);
            Assert.AreEqual(0, compiledPasses[1].syncToPassIndex);
            Assert.AreEqual(1, compiledPasses[2].syncToPassIndex);
        }

        [Test]
        public void AsyncPassReadWaitOnGraphicsPipe()
        {
            TextureHandle texture0;
            TextureHandle texture1;
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                texture0 = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("Async_TestPass1", out var passData))
            {
                builder.ReadTexture(texture0);
                texture1 = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.EnableAsyncCompute(true);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.ReadTexture(texture1);
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0)); // Needed for the passes to not be culled
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(3, compiledPasses.size);
            Assert.AreEqual(0, compiledPasses[1].syncToPassIndex);
            Assert.AreEqual(1, compiledPasses[2].syncToPassIndex);
        }

        [Test]
        public void GraphicsPassWriteWaitOnAsyncPipe()
        {
            TextureHandle texture0;
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("Async_TestPass0", out var passData))
            {
                texture0 = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.EnableAsyncCompute(true);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            // This pass should sync with the "Async_TestPass0"
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                texture0 = builder.WriteTexture(texture0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            // Read result and output to backbuffer to avoid culling passes.
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.ReadTexture(texture0);
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0)); // Needed for the passes to not be culled
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(3, compiledPasses.size);
            Assert.AreEqual(0, compiledPasses[1].syncToPassIndex);
        }

        [Test]
        public void GraphicsPassReadWaitOnAsyncPipe()
        {
            TextureHandle texture0;
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("Async_TestPass0", out var passData))
            {
                texture0 = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.EnableAsyncCompute(true);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.ReadTexture(texture0);
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0)); // Needed for the passes to not be culled
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(2, compiledPasses.size);
            Assert.AreEqual(0, compiledPasses[1].syncToPassIndex);
        }

        [Test]
        public void SetRenderAttachmentValidation()
        {
            TextureHandle texture0;
            texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            TextureHandle texture1;
            texture1 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            // Using two different textures on the same slot not allowed
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("Async_TestPass0", out var passData))
            {
                builder.SetRenderAttachment(texture0, 0, AccessFlags.ReadWrite);
                Assert.Throws<System.InvalidOperationException>(() =>
                {
                    builder.SetRenderAttachment(texture1, 0, AccessFlags.ReadWrite);
                });
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Using the same texture on two slots, not allowed
            // TODO: Would this be allowed if read-only? Likely an edge case possibly hardware dependent... let's not bother with it
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("Async_TestPass0", out var passData))
            {
                builder.SetRenderAttachment(texture0, 0, AccessFlags.ReadWrite);
                Assert.Throws<System.InvalidOperationException>(() =>
                {
                    builder.SetRenderAttachment(texture0, 1, AccessFlags.ReadWrite);
                });
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Using a texture both as a texture and as a fragment, not allowed
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("Async_TestPass0", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.ReadWrite);
                Assert.Throws<System.InvalidOperationException>(() =>
                {
                    builder.SetRenderAttachment(texture0, 0, AccessFlags.ReadWrite);
                });
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }


            // Using a texture both as a texture and as a fragment, not allowed (reversed)
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("Async_TestPass0", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.ReadWrite);
                Assert.Throws<System.InvalidOperationException>(() =>
                {
                    builder.SetRenderAttachment(texture0, 0, AccessFlags.ReadWrite);
                });
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
        }

        [Test]
        public void UseTextureValidation()
        {
            TextureHandle texture0;
            texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            TextureHandle texture1;
            texture1 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            // Writing the same texture twice is not allowed
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("Async_TestPass0", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.ReadWrite);
                Assert.Throws<System.InvalidOperationException>(() =>
                {
                    builder.UseTexture(texture0, AccessFlags.ReadWrite);
                });
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Reading the same texture twice is allowed
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("Async_TestPass0", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Read);
                builder.UseTexture(texture0, AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
        }

        [Test]
        public void ComputeHashDifferentPerResolution()
        {
            static void RenderFunc(RenderGraphTestPassData data, RenderGraphContext context) { }

            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(256, 256) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseColorBuffer(texture0, 0);
                builder.SetRenderFunc<RenderGraphTestPassData>(RenderFunc);
            }

            var hash0 = m_RenderGraph.ComputeGraphHash();
            m_RenderGraph.ClearCompiledGraph();

            TextureHandle texture1 = m_RenderGraph.CreateTexture(new TextureDesc(512, 512) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseColorBuffer(texture1, 0);
                builder.SetRenderFunc<RenderGraphTestPassData>(RenderFunc);
            }

            var hash1 = m_RenderGraph.ComputeGraphHash();
            m_RenderGraph.ClearCompiledGraph();

            Assert.AreNotEqual(hash0, hash1);
        }

        [Test]
        public void ComputeHashDifferentForMSAA()
        {
            static void RenderFunc(RenderGraphTestPassData data, RenderGraphContext context) { }

            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, msaaSamples = MSAASamples.None });

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseColorBuffer(texture0, 0);
                builder.SetRenderFunc<RenderGraphTestPassData>(RenderFunc);
            }

            var hash0 = m_RenderGraph.ComputeGraphHash();
            m_RenderGraph.ClearCompiledGraph();

            texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, msaaSamples = MSAASamples.MSAA4x });

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseColorBuffer(texture0, 0);
                builder.SetRenderFunc<RenderGraphTestPassData>(RenderFunc);
            }

            var hash1 = m_RenderGraph.ComputeGraphHash();
            m_RenderGraph.ClearCompiledGraph();

            Assert.AreNotEqual(hash0, hash1);
        }

        [Test]
        public void ComputeHashDifferentForRenderFunc()
        {
            static void RenderFunc(RenderGraphTestPassData data, RenderGraphContext context) { }
            static void RenderFunc2(RenderGraphTestPassData data, RenderGraphContext context) { }

            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseColorBuffer(texture0, 0);
                builder.SetRenderFunc<RenderGraphTestPassData>(RenderFunc);
            }

            var hash0 = m_RenderGraph.ComputeGraphHash();
            m_RenderGraph.ClearCompiledGraph();

            texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseColorBuffer(texture0, 0);
                builder.SetRenderFunc<RenderGraphTestPassData>(RenderFunc2);
            }

            var hash1 = m_RenderGraph.ComputeGraphHash();
            m_RenderGraph.ClearCompiledGraph();

            Assert.AreNotEqual(hash0, hash1);
        }

        [Test]
        public void ComputeHashDifferentForMorePasses()
        {
            static void RenderFunc(RenderGraphTestPassData data, RenderGraphContext context) { }
            static void RenderFunc2(RenderGraphTestPassData data, RenderGraphContext context) { }
            static void RenderFunc3(RenderGraphTestPassData data, RenderGraphContext context) { }

            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseColorBuffer(texture0, 0);
                builder.SetRenderFunc<RenderGraphTestPassData>(RenderFunc);
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0));
                builder.ReadTexture(texture0);
                builder.SetRenderFunc<RenderGraphTestPassData>(RenderFunc2);
            }

            var hash0 = m_RenderGraph.ComputeGraphHash();
            m_RenderGraph.ClearCompiledGraph();

            texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { format = GraphicsFormat.R8G8B8A8_UNorm });

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseColorBuffer(texture0, 0);
                builder.SetRenderFunc<RenderGraphTestPassData>(RenderFunc);
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.UseColorBuffer(texture0, 0);
                builder.SetRenderFunc<RenderGraphTestPassData>(RenderFunc3);
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0));
                builder.ReadTexture(texture0);
                builder.SetRenderFunc<RenderGraphTestPassData>(RenderFunc2);
            }

            var hash1 = m_RenderGraph.ComputeGraphHash();
            m_RenderGraph.ClearCompiledGraph();

            Assert.AreNotEqual(hash0, hash1);
        }

        [Test]
        public void ComputeHashSameForOneSetup()
        {
            static void RenderFunc(RenderGraphTestPassData data, RenderGraphContext context) { }
            static void RenderFunc2(RenderGraphTestPassData data, RenderGraphContext context) { }
            static void RenderFunc3(RenderGraphTestPassData data, RenderGraphContext context) { }

            static void RecordRenderGraph(RenderGraph renderGraph)
            {
                TextureHandle texture0 = renderGraph.CreateTexture(new TextureDesc(Vector2.one) { format = GraphicsFormat.R8G8B8A8_UNorm });

                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                {
                    builder.UseColorBuffer(texture0, 0);
                    builder.SetRenderFunc<RenderGraphTestPassData>(RenderFunc);
                }

                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                {
                    builder.UseColorBuffer(texture0, 0);
                    builder.SetRenderFunc<RenderGraphTestPassData>(RenderFunc3);
                }

                using (var builder = renderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
                {
                    builder.WriteTexture(renderGraph.ImportBackbuffer(0));
                    builder.ReadTexture(texture0);
                    builder.SetRenderFunc<RenderGraphTestPassData>(RenderFunc2);
                }
            }

            RecordRenderGraph(m_RenderGraph);

            var hash0 = m_RenderGraph.ComputeGraphHash();
            m_RenderGraph.ClearCompiledGraph();

            RecordRenderGraph(m_RenderGraph);

            var hash1 = m_RenderGraph.ComputeGraphHash();
            m_RenderGraph.ClearCompiledGraph();

            Assert.AreEqual(hash0, hash1);
        }

        [Test]
        public void GetDescAndInfoForImportedTextureWorks()
        {
            RenderTextureDescriptor desc = new RenderTextureDescriptor(37, 53, GraphicsFormat.R16G16_SNorm, GraphicsFormat.None, 4);
            RenderTexture renderTexture = new RenderTexture(desc);
            RTHandle renderTextureHandle = RTHandles.Alloc(renderTexture);

            var importedTexture = m_RenderGraph.ImportTexture(renderTextureHandle);
            var renderGraphDesc = m_RenderGraph.GetTextureDesc(importedTexture);

            Assert.AreEqual(desc.width, renderGraphDesc.width);
            Assert.AreEqual(desc.height, renderGraphDesc.height);
            Assert.AreEqual(desc.graphicsFormat, renderGraphDesc.colorFormat);
            Assert.AreEqual(DepthBits.None, renderGraphDesc.depthBufferBits);


            var renderTargetInfo = m_RenderGraph.GetRenderTargetInfo(importedTexture);
            Assert.AreEqual(desc.width, renderTargetInfo.width);
            Assert.AreEqual(desc.height, renderTargetInfo.height);
            Assert.AreEqual(desc.graphicsFormat, renderTargetInfo.format);

            CoreUtils.Destroy(renderTexture);
        }

        [Test]
        public void TextureDescFormatPropertiesWork()
        {
            var formatR32 = GraphicsFormat.R32_SFloat;

            var textureDesc = new TextureDesc(16, 16);
            textureDesc.format = formatR32;

            Assert.AreEqual(textureDesc.colorFormat,formatR32);
            Assert.AreEqual(textureDesc.depthBufferBits, DepthBits.None);

            textureDesc.depthBufferBits = DepthBits.None;

            //No change expected
            Assert.AreEqual(textureDesc.colorFormat, formatR32);
            Assert.AreEqual(textureDesc.depthBufferBits, DepthBits.None);

            textureDesc.depthBufferBits = DepthBits.Depth32;

            //Not entirely sure what the platform will select but at least it should be 24 or more (not 0)
            Assert.IsTrue((int)textureDesc.depthBufferBits >= 24);
            Assert.AreEqual(textureDesc.colorFormat, GraphicsFormat.None);

            textureDesc.format = formatR32;

            Assert.AreEqual(textureDesc.colorFormat, formatR32);
            Assert.AreEqual(textureDesc.depthBufferBits, DepthBits.None);

            textureDesc.format = GraphicsFormat.D16_UNorm;

            Assert.AreEqual(textureDesc.depthBufferBits, DepthBits.Depth16);
            Assert.AreEqual(textureDesc.colorFormat, GraphicsFormat.None);

            {
                var importedTexture = m_RenderGraph.CreateTexture(textureDesc);

                var importedDesc = importedTexture.GetDescriptor(m_RenderGraph);
                Assert.AreEqual(textureDesc.format, importedDesc.format);
            }            

            textureDesc.colorFormat = formatR32;
            Assert.AreEqual(textureDesc.depthBufferBits, DepthBits.None);
            Assert.AreEqual(textureDesc.colorFormat, textureDesc.format);

            {
                var importedTexture = m_RenderGraph.CreateTexture(textureDesc);

                var importedDesc = importedTexture.GetDescriptor(m_RenderGraph);
                Assert.AreEqual(textureDesc.format, importedDesc.format);
            } 
        }

        [Test]
        public void ImportingBuiltinRenderTextureTypeWithNoInfoThrows()
        {
            RenderTargetIdentifier renderTargetIdentifier = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
            RTHandle renderTextureHandle = RTHandles.Alloc(renderTargetIdentifier);

            Assert.Throws<Exception>(() =>
            {
                var importedTexture = m_RenderGraph.ImportTexture(renderTextureHandle);
            });

            renderTextureHandle.Release();
        }

        [Test]
        public void ImportingRenderTextureWithColorAndDepthThrows()
        {
            // Create a new RTHandle texture
            var desc = new RenderTextureDescriptor(16, 16, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.D32_SFloat_S8_UInt);
            var rt = new RenderTexture(desc) { name = "RenderTextureWithColorAndDepth"};

            var renderTextureHandle = RTHandles.Alloc(rt);

            Assert.Throws<Exception>(() =>
            {
                var importedTexture = m_RenderGraph.ImportTexture(renderTextureHandle);
            });

            renderTextureHandle.Release();
            rt.Release();
        }

        [Test]
        public void ImportingBuiltinRenderTextureTypeWithInfoHasNoDesc()
        {
            RenderTargetIdentifier renderTargetIdentifier = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
            RTHandle renderTextureHandle = RTHandles.Alloc(renderTargetIdentifier);

            var importedInfo = new RenderTargetInfo();
            importedInfo.width = 128;
            importedInfo.height = 128;
            importedInfo.volumeDepth = 1;
            importedInfo.msaaSamples = 1;
            importedInfo.format = GraphicsFormat.B8G8R8A8_SNorm;
            var importedTexture = m_RenderGraph.ImportTexture(renderTextureHandle, importedInfo);

            Assert.Throws<ArgumentException>(() =>
            {
                var renderGraphDesc = m_RenderGraph.GetTextureDesc(importedTexture);
            });

            var renderTargetInfo = m_RenderGraph.GetRenderTargetInfo(importedTexture);

            // It just needs to return what was fed in.
            Assert.AreEqual(importedInfo, renderTargetInfo);
        }

        [Test]
        public void CreateLegacyRendererLists()
        {
            // record and execute render graph calls
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                var rendererListHandle = m_RenderGraph.CreateUIOverlayRendererList(camera);
                Assert.IsTrue(rendererListHandle.IsValid());

                rendererListHandle = m_RenderGraph.CreateWireOverlayRendererList(camera);
                Assert.IsTrue(rendererListHandle.IsValid());

                rendererListHandle = m_RenderGraph.CreateGizmoRendererList(camera, GizmoSubset.PostImageEffects);
                Assert.IsTrue(rendererListHandle.IsValid());

                rendererListHandle = m_RenderGraph.CreateSkyboxRendererList(camera);
                Assert.IsTrue(rendererListHandle.IsValid());

                rendererListHandle = m_RenderGraph.CreateSkyboxRendererList(camera, Matrix4x4.identity, Matrix4x4.identity);
                Assert.IsTrue(rendererListHandle.IsValid());

                rendererListHandle = m_RenderGraph.CreateSkyboxRendererList(camera, Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity);
                Assert.IsTrue(rendererListHandle.IsValid());
            };
            m_Camera.Render();
        }

        [Test]
        public void RenderPassWithNoRenderFuncThrows()
        {
            // record and execute render graph calls
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPassWithNoRenderFunc", out var passData))
                {
                    builder.AllowPassCulling(false);

                    // no render func                    
                }
            };
            LogAssert.Expect(LogType.Error, "Render Graph Execution error");
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: RenderPass TestPassWithNoRenderFunc was not provided with an execute function.");
            m_Camera.Render();
        }

        /*
        // Disabled for now as version management is not exposed to user code
        [Test]
        public void VersionManagement()
        {

            TextureHandle texture0;
            TextureHandle texture0v1;
            TextureHandle texture0v2;
            texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            TextureHandle texture1;
            texture1 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            // Handles are unversioned by default. Unversioned handles use an implicit version "the latest version" depending on their
            // usage context.
            Assert.AreEqual(false, texture0.handle.IsVersioned);

            // Writing bumps the version
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("Async_TestPass0", out var passData))
            {
                texture0v1 = builder.UseTexture(texture0, AccessFlags.ReadWrite);
                Assert.AreEqual(true, texture0v1.handle.IsVersioned);
                Assert.AreEqual(1, texture0v1.handle.version);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Writing again bumps again
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("Async_TestPass1", out var passData))
            {
                texture0v2 = builder.UseTexture(texture0, AccessFlags.ReadWrite);
                Assert.AreEqual(true, texture0v2.handle.IsVersioned);
                Assert.AreEqual(2, texture0v2.handle.version);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Reading leaves the version alone
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("Async_TestPass2", out var passData))
            {
                var versioned = builder.UseTexture(texture0, AccessFlags.Read);
                Assert.AreEqual(true, versioned.handle.IsVersioned);
                Assert.AreEqual(2, versioned.handle.version);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Writing to an old version is not supported it would lead to a divergent version timeline for the resource
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("Async_TestPass2", out var passData))
            {
                // If you want do achieve this and avoid copying the move should be used
                Assert.Throws<System.InvalidOperationException>(() =>
                {
                    var versioned = builder.UseTexture(texture0v1, AccessFlags.ReadWrite);
                });
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
        }*/

        class RenderGraphAsyncRequestTestData
        {
            public TextureHandle texture;
            public NativeArray<byte> pixels;
        }

        [Test]
        public void ImportedTexturesAreClearedOnFirstUse()
        {
            bool asyncReadbackDone = false;
            const int kWidth = 4;
            const int kHeight = 4;
            const GraphicsFormat format = GraphicsFormat.R8G8B8A8_SRGB;

            NativeArray<byte> pixels = default;

            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                var redTexture = CreateRedTexture(kWidth, kHeight);
                ImportResourceParams importParams = new ImportResourceParams()
                {
                    clearColor = Color.blue, clearOnFirstUse = true
                };
                var importedTexture = m_RenderGraph.ImportTexture(redTexture, importParams);

                pixels = new NativeArray<byte>(kWidth * kHeight * 4, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);

                using (var builder =
                       m_RenderGraph.AddUnsafePass<RenderGraphAsyncRequestTestData>("ImportedTextureTest", out var passData))
                {
                    builder.AllowPassCulling(false);
                    builder.UseTexture(importedTexture, AccessFlags.ReadWrite);

                    passData.texture = importedTexture;
                    passData.pixels = pixels;

                    builder.SetRenderFunc((RenderGraphAsyncRequestTestData data, UnsafeGraphContext context) =>
                    {
                        context.cmd.RequestAsyncReadbackIntoNativeArray(ref data.pixels, data.texture, 0, format,
                            request => RenderGraphTest_AsyncReadbackCallback(request, ref asyncReadbackDone));
                    });
                }
            };

            m_Camera.Render();

            AsyncGPUReadback.WaitAllRequests();
            Assert.True(asyncReadbackDone);

            // Texture should be clear color instead of original red color
            for (int i = 0; i < kWidth * kHeight; i += 4)
            {
                Assert.True(pixels[i] / 255.0f == Color.blue.r);
                Assert.True(pixels[i + 1] / 255.0f == Color.blue.g);
                Assert.True(pixels[i + 2] / 255.0f == Color.blue.b);
                Assert.True(pixels[i + 3] / 255.0f == Color.blue.a);
            }

            pixels.Dispose();
        }

        [Test]
        public void RequestAsyncReadbackIntoNativeArrayWorks()
        {
            bool asyncReadbackDone = false;
            const int kWidth = 4;
            const int kHeight = 4;
            const GraphicsFormat format = GraphicsFormat.R8G8B8A8_SRGB;

            NativeArray<byte> pixels = default;
            bool passExecuted = false;

            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                // Avoid performing the same request multiple frames for nothing
                if (passExecuted)
                    return;

                passExecuted = true;

                var redTexture = CreateRedTexture(kWidth, kHeight);
                var texture0 = m_RenderGraph.ImportTexture(redTexture);

                pixels = new NativeArray<byte>(kWidth * kHeight * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphAsyncRequestTestData>("ReadbackPass", out var passData))
                {
                    builder.AllowPassCulling(false);

                    builder.UseTexture(texture0, AccessFlags.ReadWrite);

                    passData.texture = texture0;
                    passData.pixels = pixels;

                    builder.SetRenderFunc((RenderGraphAsyncRequestTestData data, UnsafeGraphContext context) =>
                    {
                        context.cmd.RequestAsyncReadbackIntoNativeArray(ref data.pixels, data.texture, 0, format,
                            request => RenderGraphTest_AsyncReadbackCallback(request, ref asyncReadbackDone));
                    });
                }
            };

            m_Camera.Render();

            AsyncGPUReadback.WaitAllRequests();

            Assert.True(asyncReadbackDone);

            for (int i = 0; i < kWidth * kHeight; i += 4)
            {
                Assert.True(pixels[i] / 255.0f == Color.red.r);
                Assert.True(pixels[i+1] / 255.0f == Color.red.g);
                Assert.True(pixels[i+2] / 255.0f == Color.red.b);
                Assert.True(pixels[i+3] / 255.0f == Color.red.a);
            }

            pixels.Dispose();
        }

        void RenderGraphTest_AsyncReadbackCallback(AsyncGPUReadbackRequest request, ref bool asyncReadbackDone)
        {
            if (request.hasError)
            {
                // We shouldn't have any error, asserting.
                Assert.True(asyncReadbackDone);
            }
            else if (request.done)
            {
                asyncReadbackDone = true;
            }
        }

        RTHandle CreateRedTexture(int width, int height)
        {
            // Create a red color
            Color redColor = Color.red;

            // Initialize the RTHandle system if necessary
            RTHandles.Initialize(width, height);

            // Create a new RTHandle texture
            var redTextureHandle = RTHandles.Alloc(width, height,
                                               GraphicsFormat.R8G8B8A8_UNorm,
                                               dimension: TextureDimension.Tex2D,
                                               useMipMap: false,
                                               autoGenerateMips: false,
                                               name: "RedTexture");

            // Set the texture to red
            Texture2D tempTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < tempTexture.height; y++)
            {
                for (int x = 0; x < tempTexture.width; x++)
                {
                    tempTexture.SetPixel(x, y, redColor);
                }
            }
            tempTexture.Apply();

            // Copy the temporary Texture2D to the RTHandle
            Graphics.Blit(tempTexture, redTextureHandle.rt);

            Texture2D.DestroyImmediate(tempTexture);

            // Cleanup the temporary texture
            return redTextureHandle;
        }

        class TestBuffersImport
        {
            public BufferHandle bufferHandle;
            public ComputeShader computeShader;
        }

        private const string kPathToComputeShader = "Packages/com.unity.render-pipelines.core/Tests/Editor/BufferCopyTest.compute";

        [Test, ConditionalIgnore("IgnoreGraphicsAPI", "Compute Shaders are not supported for this Graphics API.")]
        public void ImportingBufferWorks()
        {
#if UNITY_EDITOR
            var computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(kPathToComputeShader);
#else
            var computeShader = Resources.Load<ComputeShader>("_" + Path.GetFileNameWithoutExtension(kPathToComputeShader));
#endif
            // Check if the compute shader was loaded successfully
            if (computeShader == null)
            {
                Debug.LogError("Compute Shader not found!");
                return;
            }

            // Define the size of the buffer (number of elements)
            int bufferSize = 4; // We are only interested in the first four values

            // Allocate the buffer with the given size and format
            var buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferSize, sizeof(float));

            // Initialize the buffer with zeros
            float[] initialData = new float[bufferSize];
            buffer.SetData(initialData);

            // Ensure the data is set to 0.0f
            for (int i = 0; i < bufferSize; i++)
            {
                Assert.IsTrue(initialData[i] == 0.0f);
            }

            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                using (var builder = m_RenderGraph.AddComputePass<TestBuffersImport>("TestPass0", out var passData))
                {
                    builder.AllowPassCulling(false);

                    passData.bufferHandle = m_RenderGraph.ImportBuffer(buffer);

                    builder.UseBuffer(passData.bufferHandle, AccessFlags.Write);

                    passData.computeShader = computeShader;

                    builder.SetRenderFunc((TestBuffersImport data, ComputeGraphContext ctx) =>
                    {
                        int kernel = data.computeShader.FindKernel("CSMain");

                        ctx.cmd.SetComputeBufferParam(data.computeShader, kernel, "resultBuffer", data.bufferHandle);
                        ctx.cmd.DispatchCompute(data.computeShader, kernel, 1, 1, 1);
                    });
                }
            };

            m_Camera.Render();

            // Read back the data from the buffer
            float[] result2 = new float[bufferSize];
            buffer.GetData(result2);

            buffer.Release();

            // Ensure the data has been updated
            for (int i = 0; i < bufferSize; i++)
            {
                Assert.IsTrue(result2[i] == 1.0f);
            }
        }
        
        class RenderGraphTransientTestData
        {
            public TextureHandle transientTexture;
            public TextureHandle whiteTexture;
        }

        private static readonly int k_DefaultWhiteTextureID = Shader.PropertyToID("_DefaultWhiteTex");

        [Test]
        public void TransientHandleAreValidatedByCommandBufferSafetyLayer()
        {
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTransientTestData>("TransientPass", out var passData))
                {
                    builder.AllowPassCulling(false);

                    var texDesc = new TextureDesc(Vector2.one, false, false)
                    {
                        width = 1920,
                        height = 1080,
                        format = GraphicsFormat.B10G11R11_UFloatPack32,
                        clearBuffer = true,
                        clearColor = Color.red,
                        name = "Transient Texture"
                    };
                    passData.transientTexture = builder.CreateTransientTexture(texDesc);
                    passData.whiteTexture = m_RenderGraph.defaultResources.whiteTexture;

                    builder.SetRenderFunc((RenderGraphTransientTestData data, UnsafeGraphContext context) =>
                    {
                        // Will ensure the transient texture is valid or throw an exception otherwise
                        Assert.DoesNotThrow(delegate { context.cmd.SetGlobalTexture(k_DefaultWhiteTextureID, data.transientTexture); });
                        // Put back white instead
                        context.cmd.SetGlobalTexture(k_DefaultWhiteTextureID, data.whiteTexture);
                    });
                }
            };

            m_Camera.Render();
        }
        
        class RenderGraphCleanupTestData
        {
            public TextureHandle textureToRelease;
        }

        [Test]
        public void Cleanup_ReleaseGraphicsResources_WhenCallingCleanup()
        {
            // We need to capture this variable in the lambda function of the CleanupPass unfortunately
            RenderTexture renderTextureToRemove = null;

            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphCleanupTestData>("CleanupPass", out var passData))
                {
                    builder.AllowPassCulling(false);
                    var texDesc = new TextureDesc(Vector2.one, false, false)
                    {
                        width = 1920,
                        height = 1080,
                        format = GraphicsFormat.B10G11R11_UFloatPack32,
                        clearBuffer = true,
                        clearColor = Color.red,
                        name = "Texture To Release"
                    };
                    passData.textureToRelease = m_RenderGraph.CreateTexture(texDesc);
                    builder.UseTexture(passData.textureToRelease);
                    builder.SetRenderFunc((RenderGraphCleanupTestData data, UnsafeGraphContext context) =>
                    {
                        // textureToRelease has been allocated before executing this node
                        renderTextureToRemove = (RenderTexture)data.textureToRelease;
                        Assert.IsNotNull(renderTextureToRemove);
                        // textureToRelease will returned to the texture pool after executing this node
                    });
                }
            };

            // Render Graph hasn't started yet, no texture allocated
            Assert.IsNull(renderTextureToRemove);

            m_Camera.Render();

            // Cleanup pass has been executed
            // RG resource has been created and then released to the pool
            // but the graphics resource has not been released, still attached to the pooled resource
            // in case a next pass will reuse it
            Assert.IsNotNull(renderTextureToRemove);

            m_RenderGraph.Cleanup();

            // All RG resources and data structures have been released
            Assert.IsTrue(renderTextureToRemove == null);
        }

        [Test]
        public void Cleanup_RenderAgain_AfterCallingCleanup()
        {
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphCleanupTestData>("MidCleanupPass", out var passData))
                {
                    builder.AllowPassCulling(false);
                    var texDesc = new TextureDesc(Vector2.one, false, false)
                    {
                        width = 1920,
                        height = 1080,
                        format = GraphicsFormat.B10G11R11_UFloatPack32,
                        clearBuffer = true,
                        clearColor = Color.red,
                        name = "Texture To Release Twice"
                    };
                    passData.textureToRelease = m_RenderGraph.CreateTexture(texDesc);
                    builder.SetRenderFunc((RenderGraphCleanupTestData data, UnsafeGraphContext context) =>
                    {
                        ///
                    });
                }
            };

            m_Camera.Render();

            // Cleanup everything in Render Graph, even the native data structures
            m_RenderGraph.Cleanup();

            // Ensure that the Render Graph data structures can be reinitialized at runtime, even native ones
            Assert.DoesNotThrow(() => m_Camera.Render());
        }

        class TempAllocTestData
        {
            public TextureHandle whiteTexture;
        }

        [Test]
        public void GetTempMaterialPropertyBlockAreReleasedAfterRenderGraphNodeExecution()
        {
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                using (var builder = m_RenderGraph.AddUnsafePass<TempAllocTestData>("MPBPass", out var passData))
                {
                    builder.AllowPassCulling(false);
                    passData.whiteTexture = m_RenderGraph.defaultResources.whiteTexture;

                    builder.SetRenderFunc((TempAllocTestData data, UnsafeGraphContext context) =>
                    {
                        // no temp alloc yet
                        Assert.IsTrue(context.renderGraphPool.IsEmpty());

                        var mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
                        mpb.SetTexture(k_DefaultWhiteTextureID, data.whiteTexture);

                        // memory temporarily allocated
                        Assert.IsFalse(context.renderGraphPool.IsEmpty());
                    });
                }

                using (var builder = m_RenderGraph.AddUnsafePass<TempAllocTestData>("PostPass", out var passData))
                {
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((TempAllocTestData data, UnsafeGraphContext context) =>
                    {
                        // memory has been deallocated at the end of the previous RG node, no leak
                        Assert.IsTrue(context.renderGraphPool.IsEmpty());
                    });
                }
            };

            m_Camera.Render();
        }
    }
}