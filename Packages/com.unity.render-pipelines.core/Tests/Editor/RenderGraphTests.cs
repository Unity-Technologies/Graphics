using NUnit.Framework;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Tests
{
    class RenderGraphTests
    {
        RenderGraph m_RenderGraph = new RenderGraph();

        [SetUp]
        public void SetupRenderGraph()
        {
            m_RenderGraph.ClearCompiledGraph();
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            m_RenderGraph.Cleanup();
        }

        class RenderGraphTestPassData
        {
            public TextureHandle[] textures = new TextureHandle[8];
            public ComputeBufferHandle[] buffers = new ComputeBufferHandle[8];
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

            m_RenderGraph.CompileRenderGraph();

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

            m_RenderGraph.CompileRenderGraph();

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

            m_RenderGraph.CompileRenderGraph();

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(1, compiledPasses.size);
            Assert.AreEqual(false, compiledPasses[0].culled);
        }

        [Test]
        public void WriteToImportedComputeBufferNotCulled()
        {
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.WriteComputeBuffer(m_RenderGraph.ImportComputeBuffer(null));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph();

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

            m_RenderGraph.CompileRenderGraph();

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

            m_RenderGraph.CompileRenderGraph();

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

            m_RenderGraph.CompileRenderGraph();

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

            m_RenderGraph.CompileRenderGraph();

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

                m_RenderGraph.CompileRenderGraph();
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

            m_RenderGraph.CompileRenderGraph();

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

            m_RenderGraph.CompileRenderGraph();

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

            m_RenderGraph.CompileRenderGraph();

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(3, compiledPasses.size);
            Assert.AreEqual(false, compiledPasses[1].culled);
        }

        [Test]
        public void AsyncPassWriteWaitOnGraphcisPipe()
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

            m_RenderGraph.CompileRenderGraph();

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(3, compiledPasses.size);
            Assert.AreEqual(0, compiledPasses[1].syncToPassIndex);
            Assert.AreEqual(1, compiledPasses[2].syncToPassIndex);
        }

        [Test]
        public void AsyncPassReadWaitOnGraphcisPipe()
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

            m_RenderGraph.CompileRenderGraph();

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

            m_RenderGraph.CompileRenderGraph();

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

            m_RenderGraph.CompileRenderGraph();

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(2, compiledPasses.size);
            Assert.AreEqual(0, compiledPasses[1].syncToPassIndex);
        }
    }
}
