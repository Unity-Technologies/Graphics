using NUnit.Framework;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Tests
{
    class RenderGraphTests
    {
        RenderGraph m_RenderGraph = new RenderGraph(false, MSAASamples.None);
        ComputeBuffer m_DummyComputeBuffer;


        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_DummyComputeBuffer = new ComputeBuffer(1, 4);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_DummyComputeBuffer.Release();
        }

        [SetUp]
        public void SetupRenderGraph()
        {
            m_RenderGraph.ClearCompiledGraph();
        }

        [TearDown]
        public void ResetRenderGraph()
        {
        }

        class RenderGraphTestPassData
        {
            public TextureHandle[] textures = new TextureHandle[8];
            public ComputeBufferHandle[] buffers = new ComputeBufferHandle[8];
        }

        // Pass Pruning
        // Clear textures
        // Texture creation/release (w/ async)
        // Async synchronization
        // Side effect

        // Pass0 :
        // - Write buffer0
        // - Write texture0
        // Pass1 : Read buffer0 => not pruned
        // OR
        // Pass1 : Read texture0 => not pruned

        // Write to back buffer, not pruned.

        [Test]
        public void WriteToBackBufferNotPruned()
        {
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph();

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(1, compiledPasses.size);
            Assert.AreEqual(false, compiledPasses[0].pruned);
        }

        [Test]
        public void PrunePassWithNoProduct()
        {
            // This pass reads an input but does not produce anything (no writes) so it should be pruned.
            TextureHandle texture = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                passData.textures[0] = builder.ReadTexture(texture);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph();

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(1, compiledPasses.size);
            Assert.AreEqual(true, compiledPasses[0].pruned);
        }

        [Test]
        public void PrunePassWithTextureDependenciesAndNoProduct()
        {
            // First pass produces an output that is read by second pass.
            // Second pass does not produce anything so it should be pruned as well as all its unused dependencies.
            TextureHandle texture;
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                passData.textures[0] = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
                texture = passData.textures[0];
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                passData.textures[0] = builder.ReadTexture(texture);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph();

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(2, compiledPasses.size);
            Assert.AreEqual(true, compiledPasses[0].pruned);
            Assert.AreEqual(true, compiledPasses[1].pruned);
        }

        [Test]
        public void PrunePassWithBufferDependenciesAndNoProduct()
        {
            // First pass produces an output that is read by second pass.
            // Second pass does not produce anything so it should be pruned as well as all its unused dependencies.
            ComputeBufferHandle computeBuffer;
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                passData.buffers[0] = builder.WriteComputeBuffer(m_RenderGraph.ImportComputeBuffer(m_DummyComputeBuffer));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
                computeBuffer = passData.buffers[0];
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                passData.buffers[0] = builder.ReadComputeBuffer(computeBuffer);
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph();

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(2, compiledPasses.size);
            Assert.AreEqual(false, compiledPasses[0].pruned); // Not pruned because writing to an imported resource is a side effect.
            Assert.AreEqual(true, compiledPasses[1].pruned);
        }


        [Test]
        public void SimpleCreateReleaseTexture()
        {
            TextureHandle texture;
            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                passData.textures[0] = builder.WriteTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }));
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
                texture = passData.textures[0];
            }

            using (var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                passData.textures[0] = builder.ReadTexture(texture);
                builder.WriteTexture(m_RenderGraph.ImportBackbuffer(0)); // Needed for the passes to not be pruned
                builder.SetRenderFunc((RenderGraphTestPassData data, RenderGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph();

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(2, compiledPasses.size);
            Assert.Contains(texture, compiledPasses[0].textureCreateList);
            Assert.Contains(texture, compiledPasses[1].textureReleaseList);
        }
    }
}
