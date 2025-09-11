using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.TestTools;
using Unity.Collections;
using UnityEngine.Rendering.RendererUtils;
using System.Text.RegularExpressions;

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

    partial class RenderGraphTests : RenderGraphTestsCore
    {
        const string k_InvalidOperationMessage = "InvalidOperationException: ";

        Dictionary<RenderGraphState, List<Action>> m_GraphStateActions = new Dictionary<RenderGraphState, List<Action>>();

        class RenderGraphTestPassData
        {
            public TextureHandle[] textures = new TextureHandle[8];
            public BufferHandle[] buffers = new BufferHandle[8];
        }

        // Final output (back buffer) of render graph needs to be explicitly imported in order to know that the chain of dependency should not be culled.
        [Test]
        public void WriteToBackBufferNotCulled()
        {
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseTexture(m_RenderGraph.ImportBackbuffer(0), AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
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
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseTexture(m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm }), AccessFlags.WriteAll);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
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
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseTexture(m_RenderGraph.ImportTexture(null), AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(1, compiledPasses.size);
            Assert.AreEqual(false, compiledPasses[0].culled);
        }

        [Test]
        public void WriteToImportedComputeBufferNotCulled()
        {
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseBuffer(m_RenderGraph.ImportBuffer(null), AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
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
            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            TextureHandle texture1 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Read);
                builder.UseTexture(texture1, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            // This pass writes to texture0 which is used so will not be culled out.
            // Since texture0 is never read after this pass, we should decrement refCount for this pass and potentially cull it.
            // However, it also writes to texture1 which is used in the last pass so we mustn't cull it.
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Write);
                builder.UseTexture(texture1, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass3", out var passData))
            {
                builder.UseTexture(texture1, AccessFlags.Read);
                builder.UseTexture(m_RenderGraph.ImportBackbuffer(0), AccessFlags.Write); // Needed for the passes to not be culled
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
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
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
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
            TextureHandle texture = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseTexture(texture, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.UseTexture(texture, AccessFlags.Read);
                builder.UseTexture(m_RenderGraph.ImportBackbuffer(0), AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
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
            TextureHandle texture = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseTexture(texture, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            // Add dummy passes
            for (int i = 0; i < 2; ++i)
            {
                using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass1", out var passData))
                {
                    builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
                }
            }

            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.UseTexture(texture, AccessFlags.Read);
                builder.UseTexture(m_RenderGraph.ImportBackbuffer(0), AccessFlags.Write); // Needed for the passes to not be culled
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
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
                using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass0", out var passData))
                {
                    texture = builder.CreateTransientTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
                    builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
                }

                using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass1", out var passData))
                {
                    builder.UseTexture(texture, AccessFlags.Read); // This is illegal (transient resource was created in previous pass)
                    builder.UseTexture(m_RenderGraph.ImportBackbuffer(0), AccessFlags.Write); // Needed for the passes to not be culled
                    builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
                }

                m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());
            });
        }

        [Test]
        public void TransientCreateReleaseInSamePass()
        {
            TextureHandle texture;
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                texture = builder.CreateTransientTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
                builder.UseTexture(m_RenderGraph.ImportBackbuffer(0), AccessFlags.Write); // Needed for the passes to not be culled
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
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
            TextureHandle texture0 =
                m_RenderGraph.CreateTexture(
                    new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            TextureHandle texture1 =
                m_RenderGraph.CreateTexture(
                    new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            TextureHandle texture2; // transient texture
            TextureHandle texture3 =
                m_RenderGraph.CreateTexture(
                    new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            // First pass creates and writes two textures.
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("Async_TestPass0", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Write);
                builder.UseTexture(texture1, AccessFlags.Write);
                builder.EnableAsyncCompute(true);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            // Second pass creates a transient texture => Create/Release should happen in this pass but we want to delay the release until the first graphics pipe pass that sync with async queue.
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("Async_TestPass1", out var passData))
            {
                texture2 = builder.CreateTransientTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
                builder.UseTexture(texture0, AccessFlags.Write);
                builder.EnableAsyncCompute(true);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            // This pass is the last to read texture0 => Release should happen in this pass but we want to delay the release until the first graphics pipe pass that sync with async queue.
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("Async_TestPass2", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Read);
                builder.UseTexture(texture1, AccessFlags.Write);
                builder.EnableAsyncCompute(true);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            // Just here to add "padding" to the number of passes to ensure resources are not released right at the first sync pass.
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass3", out var passData))
            {
                builder.UseTexture(texture3, AccessFlags.Write);
                builder.EnableAsyncCompute(false);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            // Pass prior to synchronization should be where textures are released.
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass4", out var passData))
            {
                builder.UseTexture(texture3, AccessFlags.Write);
                builder.EnableAsyncCompute(false);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            // Graphics pass that reads texture1. This will request a sync with compute pipe. The previous pass should be the one releasing async textures.
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass5", out var passData))
            {
                builder.UseTexture(texture1, AccessFlags.Read);
                builder.UseTexture(texture3, AccessFlags.Read);
                builder.UseTexture(m_RenderGraph.ImportBackbuffer(0), AccessFlags.Write); // Needed for the passes to not be culled
                builder.EnableAsyncCompute(false);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
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
            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.CreateTransientTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
                builder.UseTexture(texture0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            // Graphics pass that reads texture1. This will request a sync with compute pipe. The previous pass should be the one releasing async textures.
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass5", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Read);
                builder.UseTexture(m_RenderGraph.ImportBackbuffer(0), AccessFlags.Write); // Needed for the passes to not be culled
                builder.EnableAsyncCompute(false);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(3, compiledPasses.size);
            Assert.AreEqual(false, compiledPasses[1].culled);
        }

        [Test]
        public void AsyncPassWriteWaitOnGraphicsPipe()
        {
            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("Async_TestPass1", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Write);
                builder.EnableAsyncCompute(true);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Read);
                builder.UseTexture(m_RenderGraph.ImportBackbuffer(0), AccessFlags.Write); // Needed for the passes to not be culled
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
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
            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            TextureHandle texture1 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("Async_TestPass1", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Read);
                builder.UseTexture(texture1, AccessFlags.Write);
                builder.EnableAsyncCompute(true);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.UseTexture(texture1, AccessFlags.Read);
                builder.UseTexture(m_RenderGraph.ImportBackbuffer(0), AccessFlags.Write); // Needed for the passes to not be culled
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
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
            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("Async_TestPass0", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Write);
                builder.EnableAsyncCompute(true);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            // This pass should sync with the "Async_TestPass0"
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            // Read result and output to backbuffer to avoid culling passes.
            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Read);
                builder.UseTexture(m_RenderGraph.ImportBackbuffer(0), AccessFlags.Write); // Needed for the passes to not be culled
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            m_RenderGraph.CompileRenderGraph(m_RenderGraph.ComputeGraphHash());

            var compiledPasses = m_RenderGraph.GetCompiledPassInfos();
            Assert.AreEqual(3, compiledPasses.size);
            Assert.AreEqual(0, compiledPasses[1].syncToPassIndex);
        }

        [Test]
        public void GraphicsPassReadWaitOnAsyncPipe()
        {
            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(Vector2.one) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("Async_TestPass0", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Write);
                builder.EnableAsyncCompute(true);
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.UseTexture(texture0, AccessFlags.Read);
                builder.UseTexture(m_RenderGraph.ImportBackbuffer(0), AccessFlags.Write); // Needed for the passes to not be culled
                builder.SetRenderFunc((RenderGraphTestPassData data, UnsafeGraphContext context) => { });
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
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: In pass TestPassWithNoRenderFunc - " + RenderGraph.RenderGraphExceptionMessages.k_NoRenderFunction);
            m_Camera.Render();
        }

        [Test]
        [TestMustExpectAllLogs]
        public void ExceptionsOnExecuteAreHandledAsExpected()
        {
            const string kErrorMessage = "A fatal error.";
            const int kWidth = 4;
            const int kHeight = 4;

            // record and execute render graph calls
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(kWidth, kHeight) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
                TextureHandle texture1 = m_RenderGraph.CreateTexture(new TextureDesc(kWidth, kHeight) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("WorkingPass", out var passData))
                {
                    builder.AllowPassCulling(false);
                    builder.SetRenderAttachment(texture0, 0);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }

                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("BrokenPass", out var passData))
                {
                    builder.AllowPassCulling(false);
                    builder.SetInputAttachment(texture1, 0);
                    builder.SetRenderAttachment(texture0, 1);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => throw new Exception(kErrorMessage));
                }
            };
            LogAssert.Expect(LogType.Error, "Render Graph Execution error");
            LogAssert.Expect(LogType.Exception, $"Exception: {kErrorMessage}");
            m_Camera.Render();
        }

        [Test]
        public void UsingAddRenderPassWithNRPThrows()
        {
            // m_RenderGraph.nativeRenderPassesEnabled is true by default
            // record and execute render graph calls
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
                using var builder = m_RenderGraph.AddRenderPass<RenderGraphTestPassData>("HDRP Render Pass", out var passData);
#pragma warning restore CS0618 // Type or member is obsolete
            };

            LogAssert.Expect(LogType.Error, "Render Graph Execution error");
            LogAssert.Expect(LogType.Exception,
                "InvalidOperationException: `AddRenderPass` is not compatible with the Native Render Pass Compiler. It is meant to be used with the HDRP Compiler. " +
                "The APIs that are compatible with the Native Render Pass Compiler are AddUnsafePass, AddComputePass and AddRasterRenderPass.");

            m_Camera.Render();
        }

        class TestBufferTextureComputeData
        {
            public BufferHandle bufferHandle;
            public TextureHandle depthTexture;
            public ComputeShader computeShader;
        }

        [Test, ConditionalIgnore("IgnoreGraphicsAPI", "Compute Shaders are not supported for this Graphics API.")]
        public void RenderGraphClearDepthTextureWithDepthReadOnlyFlag()
        {
            const int kWidth = 4;
            const int kHeight = 4;
            const string kPathComputeShader = "Packages/com.unity.render-pipelines.core/Tests/Editor/CopyDepthToBuffer.compute";

            var computeShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(kPathComputeShader);
            // Check if the compute shader was loaded successfully
            if (computeShader == null)
            {
                Debug.LogError("Compute Shader not found!");
                return;
            }

            // Define the size of the buffer (number of elements)
            int bufferSize = kWidth*kHeight; // We are only interested in the first four values

            // Allocate the buffer with the given size and format
            var buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferSize, sizeof(float));

            // Initialize the buffer with zeros
            float[] initialData = new float[bufferSize];


            // Ensure the data is set to 0.0f
            for (int i = 0; i < bufferSize; i++)
            {
                initialData[i] = 1.0f;
            }

            buffer.SetData(initialData);

            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(kWidth, kHeight) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
                TextureHandle texture1 = m_RenderGraph.CreateTexture(new TextureDesc(kWidth, kHeight) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

                TextureHandle depthTexture = m_RenderGraph.CreateTexture(new TextureDesc(kWidth, kHeight) { colorFormat = GraphicsFormat.D16_UNorm });
                // no depth
                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                {

                    builder.SetRenderAttachment(texture0, 0, AccessFlags.Write);
                    builder.UseTexture(texture1, AccessFlags.Read);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }

                // with depth
                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                {
                    builder.SetRenderAttachment(texture0, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(depthTexture, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }

                // Compute pass
                using (var builder = m_RenderGraph.AddComputePass<TestBufferTextureComputeData>("TestPass Compute", out var passData))
                {
                    builder.AllowPassCulling(false);

                    // Import resources into the Render Graph
                    passData.bufferHandle = m_RenderGraph.ImportBuffer(buffer); // Import external ComputeBuffer
                    passData.depthTexture = depthTexture; // Import RTHandle texture

                    builder.UseBuffer(passData.bufferHandle, AccessFlags.Write); // Ensure correct usage of the buffer
                    builder.UseTexture(passData.depthTexture, AccessFlags.ReadWrite);

                    // Assign the compute shader
                    passData.computeShader = computeShader;

                    builder.SetRenderFunc((TestBufferTextureComputeData data, ComputeGraphContext ctx) =>
                    {
                        int kernel = data.computeShader.FindKernel("CSMain");

                        ctx.cmd.SetComputeBufferParam(data.computeShader, kernel, "resultBuffer", data.bufferHandle);
                        ctx.cmd.SetComputeTextureParam(data.computeShader, kernel, "_DepthTexture", data.depthTexture);
                        ctx.cmd.DispatchCompute(data.computeShader, kernel, kWidth, kHeight, 1);
                    });
                }

                var result = m_RenderGraph.CompileNativeRenderGraph(m_RenderGraph.ComputeGraphHash());
                var passes = result.contextData.GetNativePasses();
                Assert.AreEqual(1, passes.Count); // 1 native Pass + compute
                Assert.AreEqual(2, passes[0].numNativeSubPasses);
                Assert.True(result.contextData.nativeSubPassData[0].flags.HasFlag(SubPassFlags.ReadOnlyDepth));
            };
            m_Camera.Render();

            // TODO: With current structure of the Tests, nativePassCompiler is not accessible out of recordRenderGraphBody.
            // Add checks for the passes.count and checks if first subpass has readOnlyDepth flag in future update

            // Read back the data from the buffer
            float[] result2 = new float[bufferSize];
            buffer.GetData(result2);

            buffer.Release();

            // Ensure the data has been updated
            for (int i = 0; i < bufferSize; i++)
            {
                Assert.IsTrue(result2[i] == 0.0f);
            }
        }

        [Test]
        public void RenderGraphTilePropertiesWorksWithDepthOnlyReadFlag()
        {
            const int kWidth = 4;
            const int kHeight = 4;

            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(kWidth, kHeight) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            TextureHandle depthTexture = m_RenderGraph.CreateTexture(new TextureDesc(kWidth, kHeight) { colorFormat = GraphicsFormat.D16_UNorm });
            // no depth with Tile Properties
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachment(texture0, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.TileProperties);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // with depth
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachment(texture0, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(depthTexture, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = m_RenderGraph.CompileNativeRenderGraph(m_RenderGraph.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();
            // EXpected result is that ReadOnlyDepth is added to subpass 0 to be able to merge subpass 0 and 1 into the same render pass.
            // TileProperties flag should be added to subpass 0 and not interfere with merging.
            Assert.AreEqual(1, passes.Count); // 1 native Pass
            Assert.AreEqual(2, passes[0].numNativeSubPasses);
            Assert.True(result.contextData.nativeSubPassData[0].flags.HasFlag(SubPassFlags.ReadOnlyDepth));
            Assert.True(result.contextData.nativeSubPassData[0].flags.HasFlag(SubPassFlags.TileProperties));
        }

        [Test]
        public void RenderGraphTilePropertiesWorksWhenItsLast()
        {
            const int kWidth = 4;
            const int kHeight = 4;

            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(kWidth, kHeight) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            TextureHandle depthTexture = m_RenderGraph.CreateTexture(new TextureDesc(kWidth, kHeight) { colorFormat = GraphicsFormat.D16_UNorm });

            // no Tile Properties
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachment(texture0, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // with Tile Properties
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachment(texture0, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.TileProperties);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = m_RenderGraph.CompileNativeRenderGraph(m_RenderGraph.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();
            // TilePrperties flag should be added to subpass 0 and not interfere with merging.
            Assert.AreEqual(1, passes.Count); // 1 native Pass
            Assert.True(result.contextData.nativeSubPassData[0].flags.HasFlag(SubPassFlags.TileProperties));
        }

        [Test]
        public void RenderGraphTilePropertiesWorksWhenItsMiddle()
        {
            const int kWidth = 4;
            const int kHeight = 4;

            TextureHandle texture0 = m_RenderGraph.CreateTexture(new TextureDesc(kWidth, kHeight) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
            TextureHandle depthTexture = m_RenderGraph.CreateTexture(new TextureDesc(kWidth, kHeight) { colorFormat = GraphicsFormat.D16_UNorm });

            // no tile properties
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachment(texture0, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // with Tile Properties
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachment(texture0, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.TileProperties);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // no tile properties
            using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderAttachment(texture0, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = m_RenderGraph.CompileNativeRenderGraph(m_RenderGraph.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();
            // TilePrperties flag should be added to subpass 0 and not interfere with merging.
            Assert.AreEqual(1, passes.Count); // 1 native Pass
            Assert.True(result.contextData.nativeSubPassData[0].flags.HasFlag(SubPassFlags.TileProperties));
        }

        [Test]
        public void RenderGraphTilePropertiesCanOnlyBeSetForOnePass()
        {
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                {
                    builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.TileProperties);
                }

                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                {
                    builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.TileProperties);
                }
                LogAssert.Expect(LogType.Error, "Render Graph Execution error");
                LogAssert.Expect(LogType.Exception, "Exception: ExtendedFeatureFlags.TileProperties can only be set once per render graph (render graph RenderGraph, pass TestPass1), previously set at (pass TestPass0).");
                var result = m_RenderGraph.CompileNativeRenderGraph(m_RenderGraph.ComputeGraphHash());
            };
            m_Camera.Render();
        }

        [Test]
        public void RenderGraphMultisampledShaderResolvePassWorks()
        {
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                if (!SystemInfo.supportsMultisampledShaderResolve)
                {
                    return; // Skip the test if the platform does not support multisampled shader resolve
                }

                var colorTexDesc = new TextureDesc(Vector2.one, false, false)
                {
                    width = 4,
                    height = 4,
                    format = GraphicsFormat.R8G8B8A8_UNorm,
                    clearBuffer = true,
                    clearColor = Color.red,
                    msaaSamples = MSAASamples.MSAA4x,
                    memoryless = RenderTextureMemoryless.None, // Initially set memoryless to false, RG will modify it
                    name = "MSAA Color Texture"
                };

                var createdMSAAx4Color = m_RenderGraph.CreateTexture(colorTexDesc);

                colorTexDesc.msaaSamples = MSAASamples.None;
                var createdMSAAx1Color = m_RenderGraph.CreateTexture(colorTexDesc);

                // MSAA4x pass
                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                {
                    builder.SetRenderAttachment(createdMSAAx4Color, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }

                // MSAA1x pass with shader resolve enabled
                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                {
                    builder.SetRenderAttachment(createdMSAAx1Color, 0, AccessFlags.Write);
                    builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultisampledShaderResolve);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }
                var result = m_RenderGraph.CompileNativeRenderGraph(m_RenderGraph.ComputeGraphHash());
                var passes = result.contextData.GetNativePasses();

                Assert.AreEqual(1, passes.Count);
            };
            m_Camera.Render();
        }

        [Test]
        public void RenderGraphMultisampledShaderResolvePassWorksForMSAATarget()
        {
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                var colorTexDesc = new TextureDesc(Vector2.one, false, false)
                {
                    width = 4,
                    height = 4,
                    format = GraphicsFormat.R8G8B8A8_UNorm,
                    clearBuffer = true,
                    clearColor = Color.red,
                    msaaSamples = MSAASamples.MSAA4x,
                    memoryless = RenderTextureMemoryless.None, // Initially set memoryless to false, RG will modify it
                    name = "MSAA Color Texture"
                };

                var createdMSAAx4Color0 = m_RenderGraph.CreateTexture(colorTexDesc);
                var createdMSAAx4Color1 = m_RenderGraph.CreateTexture(colorTexDesc);

                // MSAA4x pass
                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                {
                    builder.SetRenderAttachment(createdMSAAx4Color0, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }

                // MSAA4x pass with shader resolve enabled
                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                {
                    builder.SetRenderAttachment(createdMSAAx4Color1, 0, AccessFlags.Write);
                    builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultisampledShaderResolve);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }
                var result = m_RenderGraph.CompileNativeRenderGraph(m_RenderGraph.ComputeGraphHash());
                var passes = result.contextData.GetNativePasses();

                Assert.AreEqual(1, passes.Count);
            };
            m_Camera.Render();
        }

        [Test]
        public void RenderGraphMultisampledShaderResolvePassMustBeTheLastSubpass()
        {
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                const int kWidth = 4;
                const int kHeight = 4;

                TextureHandle dummyTexture0 = m_RenderGraph.CreateTexture(new TextureDesc(kWidth, kHeight) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

                // Shader resolve enabled pass
                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                {
                    builder.SetRenderAttachment(dummyTexture0, 0, AccessFlags.Write);
                    builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultisampledShaderResolve);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }

                // Second pass that should not be merged.
                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                {
                    builder.SetRenderAttachment(dummyTexture0, 0, AccessFlags.Write);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }
                var result = m_RenderGraph.CompileNativeRenderGraph(m_RenderGraph.ComputeGraphHash());
                var passes = result.contextData.GetNativePasses();

                // Can't merge any pass into shader resolve enabled pass
                Assert.AreEqual(2, passes.Count); // 2 native passes
            };
            m_Camera.Render();
        }

        [Test]
        public void RenderGraphMultisampledShaderResolvePassMustHaveOneColorAttachment()
        {
            const string kErrorMessage = "Low level rendergraph error: last subpass with shader resolve must have one color attachment.";
            const int kWidth = 4;
            const int kHeight = 4;

            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                TextureHandle dummyTexture0 = m_RenderGraph.CreateTexture(new TextureDesc(kWidth, kHeight) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });
                TextureHandle dummyTexture1 = m_RenderGraph.CreateTexture(new TextureDesc(kWidth, kHeight) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm });

                // Shader resolve enabled pass
                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                {
                    builder.SetRenderAttachment(dummyTexture0, 0, AccessFlags.Write);
                    builder.SetRenderAttachment(dummyTexture1, 1, AccessFlags.Write);
                    builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultisampledShaderResolve);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }

                var result = m_RenderGraph.CompileNativeRenderGraph(m_RenderGraph.ComputeGraphHash());
                LogAssert.Expect(LogType.Error, "Render Graph Execution error");
                LogAssert.Expect(LogType.Exception, $"Exception: {kErrorMessage}");
            };
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
        }

        [Test]
        public void RenderGraphThrowsException_ErrorsWhenRecordingPass()
        {
            PopulateRecordingPassAPIActions();

            ExecuteRenderGraphAPIActions(RenderGraphState.RecordingPass);
        }

        [Test]
        public void RenderGraphThrowsException_ErrorsWhenRecordingGraph()
        {
            PopulateRecordingGraphAPIActions();

            ExecuteRenderGraphAPIActions(RenderGraphState.RecordingGraph);
        }

        [Test]
        public void RenderGraphThrowsException_ErrorsWhenExecutingGraph()
        {
            PopulateExecutingAPIActions();

            ExecuteRenderGraphAPIActions(RenderGraphState.Executing);
        }

        [Test]
        public void RenderGraphThrowsException_ErrorsWhenRecordingPassAndExecutingGraph()
        {
            PopulateRecordingPassAndExecutingGraphAPIActions();

            ExecuteRenderGraphAPIActions(RenderGraphState.RecordingPass | RenderGraphState.Executing);
        }

        [Test]
        public void RenderGraphThrowsException_ErrorsWhenRecordingPassAndGraphAndExecutingGraph()
        {
            PopulateActiveGraphAPIActions();

            ExecuteRenderGraphAPIActions(RenderGraphState.Active);
        }

        // It's forbidden to use these APIs in RecordingPass mode.
        void PopulateRecordingPassAPIActions()
        {
            var errorAPIPassName = "TestPassMustThrowException";
            var recordingPassActions = new List<Action>
            {
                () => m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>(errorAPIPassName, out var passData2),
                () => m_RenderGraph.AddUnsafePass<RenderGraphTestPassData>(errorAPIPassName, out var passData2),
                () => m_RenderGraph.AddComputePass<RenderGraphTestPassData>(errorAPIPassName, out var passData2),
            };

            m_GraphStateActions.Add(RenderGraphState.RecordingPass, recordingPassActions);
        }

        // It's forbidden to use these APIs in RecordingGraph mode.
        void PopulateRecordingGraphAPIActions()
        {
            var recordingGraphActions = new List<Action>();

            // Empty for the moment
            m_GraphStateActions.Add(RenderGraphState.RecordingGraph, recordingGraphActions);
        }

        // It's forbidden to use these APIs in Executing mode.
        void PopulateExecutingAPIActions()
        {
            var textureHandle = new TextureHandle();
            var flagActions = RenderGraphState.Executing;
            var executingActions = new List<Action>
            {
                // Texture APIs
                () => m_RenderGraph.CreateTexture(textureHandle),
                () => m_RenderGraph.CreateTextureIfInvalid(new TextureDesc(), ref textureHandle),
                () => m_RenderGraph.CreateTexture(new TextureDesc()),
                () => m_RenderGraph.ImportTexture(null),
                () => m_RenderGraph.ImportTexture(null, new ImportResourceParams()),
                () => m_RenderGraph.ImportTexture(null, new RenderTargetInfo(), new ImportResourceParams()),
                () => m_RenderGraph.ImportTexture(null, isBuiltin: false),

                // Buffer APIs
                () => m_RenderGraph.CreateBuffer(new BufferDesc()),
                () => m_RenderGraph.CreateBuffer(new BufferHandle()),
                () => m_RenderGraph.ImportBuffer(null),
                () => m_RenderGraph.ImportBackbuffer(new RenderTargetIdentifier(), new RenderTargetInfo()),
                () => m_RenderGraph.ImportBackbuffer(new RenderTargetIdentifier()),

                // RendererList APIs
                () => m_RenderGraph.CreateRendererList(new RendererListDesc()),
                () => m_RenderGraph.CreateRendererList(new RendererListParams()),
                () => m_RenderGraph.CreateGizmoRendererList(m_Camera, GizmoSubset.PreImageEffects),
                () => m_RenderGraph.CreateUIOverlayRendererList(m_Camera),
                () => m_RenderGraph.CreateUIOverlayRendererList(m_Camera, UISubset.All),
                () => m_RenderGraph.CreateWireOverlayRendererList(m_Camera),
                () => m_RenderGraph.CreateSkyboxRendererList(m_Camera),
                () => m_RenderGraph.CreateSkyboxRendererList(m_Camera, Matrix4x4.identity, Matrix4x4.identity),
                () => m_RenderGraph.CreateSkyboxRendererList(m_Camera, Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity),

                // Other APIs
                () => m_RenderGraph.ImportRayTracingAccelerationStructure(null)
            };

            m_GraphStateActions.Add(flagActions, executingActions);
        }

        // It's forbidden to use these APIs in RecordingPass and Executing mode.
        void PopulateRecordingPassAndExecutingGraphAPIActions()
        {
            var flagActions = RenderGraphState.RecordingPass | RenderGraphState.Executing;
            var recordingPassAndExecutingActions = new List<Action>
            {
                () => m_RenderGraph.EndRecordingAndExecute()
            };

            m_GraphStateActions.Add(flagActions, recordingPassAndExecutingActions);
        }

        // It's forbidden to use these APIs in RecordingGraph, RecordingPass and Executing mode.
        void PopulateActiveGraphAPIActions()
        {
            var flagActions = RenderGraphState.Active;
            var activeGraphActions = new List<Action>
            {
                () => m_RenderGraph.Cleanup(),
                () => m_RenderGraph.RegisterDebug(),
                () => m_RenderGraph.UnRegisterDebug(),
                () => m_RenderGraph.EndFrame(),
                () => m_RenderGraph.BeginRecording(new RenderGraphParameters())
            };

            m_GraphStateActions.Add(flagActions, activeGraphActions);
        }

        void ExecuteRenderGraphAPIActions(RenderGraphState state)
        {
            if (!m_GraphStateActions.ContainsKey(state))
                return;

            var listAPIs = m_GraphStateActions[state];
            var graphStateException = RenderGraph.RenderGraphExceptionMessages.GetExceptionMessage(state);

            foreach (var action in listAPIs)
            {
                // Clear the graph to avoid any previous state (invalid pass because we threw an exception during the setup of the pass).
                m_RenderGraph.ClearCurrentCompiledGraph();

                // Manually check the flag to avoid testing Idle and Active states.
                if ((state & RenderGraphState.Executing) == RenderGraphState.Executing)
                {
                    RecordGraphAPIError(RenderGraphState.Executing, graphStateException, action);
                }

                if ((state & RenderGraphState.RecordingGraph) == RenderGraphState.RecordingGraph)
                {
                    RecordGraphAPIError(RenderGraphState.RecordingGraph, graphStateException, action);
                }

                if ((state & RenderGraphState.RecordingPass) == RenderGraphState.RecordingPass)
                {
                    RecordGraphAPIError(RenderGraphState.RecordingPass, graphStateException, action);
                }
            }
        }

        void RecordGraphAPIError(RenderGraphState graphState, string exceptionExpected, Action action)
        {
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                if (graphState == RenderGraphState.RecordingGraph)
                    action.Invoke();

                using (var builder = m_RenderGraph.AddRasterRenderPass<RenderGraphTestPassData>("TestPass_APIRecordingError", out var passData))
                {
                    builder.AllowPassCulling(false);

                    if (graphState == RenderGraphState.RecordingPass)
                        action.Invoke();

                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) =>
                    {
                        if (graphState == RenderGraphState.Executing)
                            action.Invoke();
                    });
                }
            };

            LogAssert.Expect(LogType.Error, RenderGraph.RenderGraphExceptionMessages.k_RenderGraphExecutionError);
            LogAssert.Expect(LogType.Exception, k_InvalidOperationMessage + exceptionExpected);

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

        class ErrorCastPassData
        {
            public TextureHandle outputHandle;
        }

        [Test]
        public void CastToRTHandle_ThrowsException_WhenCastingHandleOutsideSetRenderFunc()
        {
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                var texDesc = new TextureDesc(Vector2.one, false, false)
                {
                    width = 1920,
                    height = 1080,
                    format = GraphicsFormat.R8G8B8A8_UNorm,
                    clearBuffer = true,
                    clearColor = Color.red,
                    name = "Dummy Texture"
                };
                var output = m_RenderGraph.CreateTexture(texDesc);
                using (var builder = m_RenderGraph.AddRasterRenderPass<ErrorCastPassData>("TestPass0", out var passData))
                {
                    // Trying to cast to a RTHandle too early, it will be created later during the execution of the pass
                    Assert.Throws<InvalidOperationException>(() =>
                    {
                        RTHandle error = (RTHandle)output;
                    });
                    passData.outputHandle = output;
                    builder.AllowPassCulling(false);
                    builder.UseTexture(output);
                    builder.SetRenderFunc((ErrorCastPassData data, RasterGraphContext context) =>
                    {
                        // We can safely cast into a RTHandle during the RG execution, resource has been created
                        Assert.DoesNotThrow(() =>
                        {
                            RTHandle rtHandle = (RTHandle)data.outputHandle;
                        });
                    });
                }
            };

            m_Camera.Render();
        }

        class MemorylessCastPassData
        {
            public TextureHandle createdDepthOutputHandle;
            public TextureHandle createdColorOutputHandle;
            public TextureHandle transientColorOutputHandle;
        }

        [Test]
        public void CastToRTHandle_WithMemorylessResource()
        {
            // Testing for each MSAA value
            foreach (var msaaSamplesId in Enum.GetValues(typeof(MSAASamples)))
            {
                var msaaSamples = (MSAASamples)msaaSamplesId;
                GraphicsFormat depthStencilFormat = GraphicsFormat.D24_UNorm_S8_UInt;
                GraphicsFormat colorFormat = GraphicsFormat.R8G8B8A8_UNorm;

                // No need to check MSAA > 2
                if (msaaSamples != MSAASamples.None && msaaSamples != MSAASamples.MSAA2x)
                    continue;

                // Skipping testing when the texture format is not supported by the platform
                if ((msaaSamples == MSAASamples.None && !SystemInfo.IsFormatSupported(depthStencilFormat, GraphicsFormatUsage.Render)) ||
                   (msaaSamples == MSAASamples.None && !SystemInfo.IsFormatSupported(colorFormat, GraphicsFormatUsage.Render)) ||
                   (msaaSamples == MSAASamples.MSAA2x && !SystemInfo.IsFormatSupported(depthStencilFormat, GraphicsFormatUsage.MSAA2x)) ||
                   (msaaSamples == MSAASamples.MSAA2x && !SystemInfo.IsFormatSupported(colorFormat, GraphicsFormatUsage.MSAA2x)))
                    continue;

                m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
                {
                    using (var builder = m_RenderGraph.AddRasterRenderPass<MemorylessCastPassData>("TestPass", out var passData))
                    {
                        var depthTexDesc = new TextureDesc(Vector2.one, false, false)
                        {
                            width = 1920,
                            height = 1080,
                            format = depthStencilFormat,
                            msaaSamples = msaaSamples,
                            memoryless = RenderTextureMemoryless.None, // Initially set memoryless to false, RG will modify it
                            name = "Dummy Depth Memoryless Texture"
                        };

                        var colorTexDesc = new TextureDesc(Vector2.one, false, false)
                        {
                            width = 1920,
                            height = 1080,
                            format = colorFormat,
                            clearBuffer = true,
                            clearColor = Color.red,
                            msaaSamples = msaaSamples,
                            memoryless = RenderTextureMemoryless.None, // Initially set memoryless to false, RG will modify it
                            name = "Dummy Color Memoryless Texture"
                        };

                        var createdDepthOutput = m_RenderGraph.CreateTexture(depthTexDesc);
                        var createdColorOutput = m_RenderGraph.CreateTexture(colorTexDesc);
                        colorTexDesc.name = "Transient Color Memoryless Texture";
                        var transientColorOutput = builder.CreateTransientTexture(colorTexDesc);

                        passData.createdDepthOutputHandle = createdDepthOutput;
                        passData.createdColorOutputHandle = createdColorOutput;
                        passData.transientColorOutputHandle = transientColorOutput;

                        builder.AllowPassCulling(false);

                        // These two resources should be tagged as memoryless by the NRP compiler as they are used as attachments in a single pass
                        builder.SetRenderAttachmentDepth(createdDepthOutput);
                        builder.SetRenderAttachment(createdColorOutput, 0);
                        builder.SetRenderAttachment(transientColorOutput, 1);
                        builder.SetRenderFunc((MemorylessCastPassData data, RasterGraphContext context) =>
                        {
                            RTHandle createdDepthRTHandle = null;
                            RTHandle createdColorRTHandle = null;
                            RTHandle transientColorRTHandle = null;

                            // Verify that the texture handles can be casted into RTHandles
                            Assert.DoesNotThrow(() =>
                            {
                                createdDepthRTHandle = (RTHandle)data.createdDepthOutputHandle;
                                createdColorRTHandle = (RTHandle)data.createdColorOutputHandle;
                                transientColorRTHandle = (RTHandle)data.transientColorOutputHandle;
                            });

                            if (!SystemInfo.supportsMemorylessTextures)
                            {
                                Assert.IsTrue(createdDepthRTHandle.rt.memorylessMode == RenderTextureMemoryless.None);
                                Assert.IsTrue(createdColorRTHandle.rt.memorylessMode == RenderTextureMemoryless.None);
                                Assert.IsTrue(transientColorRTHandle.rt.memorylessMode == RenderTextureMemoryless.None);
                            }
                            // And let's make sure that the RTHandles are memoryless, i.e. no memory is allocated in system memory
                            else if (msaaSamples != MSAASamples.None)
                            {
                                Assert.IsTrue(createdDepthRTHandle.rt.memorylessMode == (RenderTextureMemoryless.Depth | RenderTextureMemoryless.MSAA));
                                Assert.IsTrue(createdColorRTHandle.rt.memorylessMode == (RenderTextureMemoryless.Color | RenderTextureMemoryless.MSAA));
                                Assert.IsTrue(transientColorRTHandle.rt.memorylessMode == (RenderTextureMemoryless.Color | RenderTextureMemoryless.MSAA));
                            }
                            else
                            {
                                Assert.IsTrue(createdDepthRTHandle.rt.memorylessMode == RenderTextureMemoryless.Depth);
                                Assert.IsTrue(createdColorRTHandle.rt.memorylessMode == RenderTextureMemoryless.Color);
                                Assert.IsTrue(transientColorRTHandle.rt.memorylessMode == RenderTextureMemoryless.Color);
                            }
                        });
                    }
                };

                m_Camera.Render();

                m_RenderGraph.Cleanup();
            }
        }

        [Test]
        public void ResourcePool_Cleanup_ReleaseGfxResourceAndClearPool()
        {
            var texturePool = new TexturePool();

            // Initialize the RTHandle system if necessary
            RTHandles.Initialize(9, 9);

            // Create a new RTHandle texture
            RTHandle resIn = RTHandles.Alloc(9, 9,
                                               GraphicsFormat.R8G8B8A8_UNorm,
                                               dimension: TextureDimension.Tex2D,
                                               useMipMap: false,
                                               autoGenerateMips: false,
                                               name: "DummyPoolTexture");
            // Release it into the pool
            texturePool.ReleaseResource(0, resIn, 0);

            Assert.IsTrue(texturePool.GetMemorySizeInMB() > 0);
            Assert.IsTrue(texturePool.GetNumResourcesAvailable() == 1);

            // Clean the pool
            texturePool.Cleanup();

            Assert.IsTrue(texturePool.GetMemorySizeInMB() == 0);
            Assert.IsTrue(texturePool.GetNumResourcesAvailable() == 0);
        }

        [Test]
        public void ResourcePool_TryGet()
        {
            var texturePool = new TexturePool();

            // Initialize the RTHandle system if necessary
            RTHandles.Initialize(9, 9);

            // Create a new RTHandle texture
            RTHandle resIn = RTHandles.Alloc(9, 9,
                                               GraphicsFormat.R8G8B8A8_UNorm,
                                               dimension: TextureDimension.Tex2D,
                                               useMipMap: false,
                                               autoGenerateMips: false,
                                               name: "DummyPoolTexture");
            // Release it into the pool
            texturePool.ReleaseResource(0, resIn, 0);

            // Retrieve it from the pool and make sure this is the right one
            RTHandle resOut;
            texturePool.TryGetResource(0, out resOut);
            Assert.IsTrue(resIn.GetInstanceID() == resOut.GetInstanceID());

            texturePool.Cleanup();
        }

        TextureDesc SimpleTextureDesc(string name, int w, int h, int samples)
        {
            TextureDesc result = new TextureDesc(w, h);
            result.msaaSamples = (MSAASamples)samples;
            result.format = GraphicsFormat.R8G8B8A8_UNorm;
            result.name = name;
            return result;
        }

        class TestRenderTargets
        {
            public TextureHandle backBuffer;
            public TextureHandle depthBuffer;
            public TextureHandle[] extraTextures = new TextureHandle[10];
            public TextureHandle extraDepthBuffer;
            public TextureHandle extraDepthBufferBottomLeft;
        };

        TestRenderTargets ImportAndCreateRenderTargets(RenderGraph g, TextureUVOrigin backBufferUVOrigin)
        {
            TestRenderTargets result = new TestRenderTargets();
            var backBuffer = BuiltinRenderTextureType.CameraTarget;
            var backBufferHandle = RTHandles.Alloc(backBuffer, "Backbuffer Color");
            var depthBuffer = BuiltinRenderTextureType.Depth;
            var depthBufferHandle = RTHandles.Alloc(depthBuffer, "Backbuffer Depth");
            var extraDepthBufferHandle = RTHandles.Alloc(depthBuffer, "Extra Depth Buffer");
            var extraDepthBufferBottomLeftHandle = RTHandles.Alloc(depthBuffer, "Extra Depth Buffer Bottom Left");

            ImportResourceParams importParams = new ImportResourceParams();
            importParams.textureUVOrigin = backBufferUVOrigin;

            RenderTargetInfo importInfo = new RenderTargetInfo();
            RenderTargetInfo importInfoDepth = new RenderTargetInfo();
            importInfo.width = 1024;
            importInfo.height = 768;
            importInfo.volumeDepth = 1;
            importInfo.msaaSamples = 1;
            importInfo.format = GraphicsFormat.R16G16B16A16_SFloat;
            result.backBuffer = g.ImportTexture(backBufferHandle, importInfo, importParams);

            importInfoDepth = importInfo;
            importInfoDepth.format = GraphicsFormat.D32_SFloat_S8_UInt;
            result.depthBuffer = g.ImportTexture(depthBufferHandle, importInfoDepth, importParams);

            importInfo.format = GraphicsFormat.D24_UNorm;
            result.extraDepthBuffer = g.ImportTexture(extraDepthBufferHandle, importInfoDepth, importParams);

            importParams.textureUVOrigin = TextureUVOrigin.BottomLeft;
            result.extraDepthBufferBottomLeft = g.ImportTexture(extraDepthBufferBottomLeftHandle, importInfoDepth, importParams);

            for (int i = 0; i < result.extraTextures.Length; i++)
            {
                result.extraTextures[i] = g.CreateTexture(SimpleTextureDesc("ExtraTexture" + i, 1024, 768, 1));
            }

            return result;
        }

        class UVOriginPassData
        {
            public TextureUVOrigin backBufferUVOrigin;
            public TextureHandle renderAttachment;
            public TextureHandle inputAttachment;
        }

        // Test the case we want to work, attachment reads connected to the backbuffer inherit the backbuffer uv origin.
        [Test]
        public void TextureUVOrigin_CheckBackbufferUVOriginInherited()
        {
            // We don't send the list of graphics commands to execute to avoid mistmatch attachment size errors in the native render pass layer due to the backbuffer usage
            m_RenderGraphTestPipeline.invalidContextForTesting = true;
            m_RenderGraphTestPipeline.renderTextureUVOriginStrategy = RenderTextureUVOriginStrategy.PropagateAttachmentOrientation; // Switch to the mode we want to test.
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                // On !SystemInfo.graphicsUVStartsAtTop APIs everything simplifies as that matches Unity's texture reads so we can't test a TopLeft system there.
                var backBufferUVOrigin = SystemInfo.graphicsUVStartsAtTop ? TextureUVOrigin.TopLeft : TextureUVOrigin.BottomLeft;
                var renderTargets = ImportAndCreateRenderTargets(m_RenderGraph, backBufferUVOrigin);

                // Render something to 0
                using (var builder = m_RenderGraph.AddRasterRenderPass<UVOriginPassData>("TestPass0", out var passData))
                {
                    passData.backBufferUVOrigin = backBufferUVOrigin;
                    passData.renderAttachment = renderTargets.extraTextures[0];

                    builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                    builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                    builder.SetRenderFunc(static (UVOriginPassData data, RasterGraphContext context) =>
                    {
                        // Check that the backbuffer UV origin is inherited by attachments.
                        Assert.AreEqual(context.GetTextureUVOrigin(data.renderAttachment), data.backBufferUVOrigin);
                    });
                }

                // Render to 1 reading from 0 as an attachment
                using (var builder = m_RenderGraph.AddRasterRenderPass<UVOriginPassData>("TestPass1", out var passData))
                {
                    passData.backBufferUVOrigin = backBufferUVOrigin;
                    passData.inputAttachment = renderTargets.extraTextures[0];
                    passData.renderAttachment = renderTargets.extraTextures[1];

                    //builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                    builder.SetRenderAttachment(renderTargets.extraTextures[1], 0, AccessFlags.Write);
                    builder.SetInputAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Read);
                    builder.SetRenderFunc(static (UVOriginPassData data, RasterGraphContext context) =>
                    {
                        // Check that the backbuffer UV origin is inherited by attachments.
                        Assert.AreEqual(context.GetTextureUVOrigin(data.renderAttachment), data.backBufferUVOrigin);
                        Assert.AreEqual(context.GetTextureUVOrigin(data.inputAttachment), data.backBufferUVOrigin);
                    });
                }

                // Render to final buffer reading from 1 as an attachment
                using (var builder = m_RenderGraph.AddRasterRenderPass<UVOriginPassData>("TestPass2", out var passData))
                {
                    passData.backBufferUVOrigin = backBufferUVOrigin;
                    passData.inputAttachment = renderTargets.extraTextures[1];
                    passData.renderAttachment = renderTargets.backBuffer;

                    //builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                    builder.SetRenderAttachment(renderTargets.backBuffer, 0, AccessFlags.Write);
                    builder.SetInputAttachment(renderTargets.extraTextures[1], 0, AccessFlags.Read);
                    builder.SetRenderFunc(static (UVOriginPassData data, RasterGraphContext context) =>
                    {
                        // Check that the backbuffer UV origin is inherited by attachments.
                        Assert.AreEqual(context.GetTextureUVOrigin(data.renderAttachment), data.backBufferUVOrigin);
                        Assert.AreEqual(context.GetTextureUVOrigin(data.inputAttachment), data.backBufferUVOrigin);
                    });
                }

                var result = m_RenderGraph.CompileNativeRenderGraph(m_RenderGraph.ComputeGraphHash());
                var passes = result.contextData.GetNativePasses();
                Assert.AreEqual(1, passes.Count);
                Assert.AreEqual(4, passes[0].attachments.size);
                Assert.AreEqual(3, passes[0].numGraphPasses);
                Assert.AreEqual(3, passes[0].numNativeSubPasses);
            };

            m_Camera.Render();

            m_RenderGraph.Cleanup();
        }

        // Test that texture reads break the inherited UV origin from the backbuffer.
        [Test]
        public void TextureUVOrigin_CheckTextureReadBreaksBackbufferUVOriginInherited()
        {
            // On OpenGL based APIs (!SystemInfo.graphicsUVStartsAtTop) we can't perform this test as we always assume the origin is BottomLeft which is compatible with texture reads.
            if (!SystemInfo.graphicsUVStartsAtTop) return;

            // We don't send the list of graphics commands to execute to avoid mistmatch attachment size errors in the native render pass layer due to the backbuffer usage
            m_RenderGraphTestPipeline.invalidContextForTesting = true;
            m_RenderGraphTestPipeline.renderTextureUVOriginStrategy = RenderTextureUVOriginStrategy.PropagateAttachmentOrientation; // Switch to the mode we want to test.
            m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
            {
                var backBufferUVOrigin = TextureUVOrigin.TopLeft;
                var renderTargets = ImportAndCreateRenderTargets(m_RenderGraph, backBufferUVOrigin);

                // Render something to 0
                using (var builder = m_RenderGraph.AddRasterRenderPass<UVOriginPassData>("TestPass0", out var passData))
                {
                    passData.renderAttachment = renderTargets.extraTextures[0];

                    builder.SetRenderAttachmentDepth(renderTargets.extraDepthBufferBottomLeft, AccessFlags.Write);
                    builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                    builder.SetRenderFunc(static (UVOriginPassData data, RasterGraphContext context) =>
                    {
                        // 0 is read in the next pass as a unity texture so needs to be bottom left.
                        Assert.AreEqual(TextureUVOrigin.BottomLeft, context.GetTextureUVOrigin(data.renderAttachment));
                    });
                }

                // Render to 1 reading from 0 as a texture
                using (var builder = m_RenderGraph.AddRasterRenderPass<UVOriginPassData>("TestPass1", out var passData))
                {
                    passData.backBufferUVOrigin = backBufferUVOrigin;
                    passData.renderAttachment = renderTargets.extraTextures[1];

                    builder.SetRenderAttachment(renderTargets.extraTextures[1], 0, AccessFlags.Write);
                    builder.UseTexture(renderTargets.extraTextures[0], AccessFlags.Read);
                    builder.SetRenderFunc(static (UVOriginPassData data, RasterGraphContext context) =>
                    {
                        // Check that the backbuffer UV origin is inherited by attachment 1.
                        // 1 is read via attachments so could inherit the backbuffer attachment but will probably generate an exception for mixed UV origin, but only on platforms (not in the editor) where we are top left origin.
                        Assert.AreEqual(data.backBufferUVOrigin, context.GetTextureUVOrigin(data.renderAttachment));
                    });
                }

                // Render to final buffer reading from 1 as an attachment
                using (var builder = m_RenderGraph.AddRasterRenderPass<UVOriginPassData>("TestPass2", out var passData))
                {
                    passData.backBufferUVOrigin = backBufferUVOrigin;
                    passData.renderAttachment = renderTargets.backBuffer;

                    builder.SetRenderAttachment(renderTargets.backBuffer, 0, AccessFlags.Write);
                    builder.SetInputAttachment(renderTargets.extraTextures[1], 0, AccessFlags.Read);
                    builder.SetRenderFunc(static (UVOriginPassData data, RasterGraphContext context) =>
                    {
                        // Check the backbuffer is using the UV origin we expect
                        Assert.AreEqual(data.backBufferUVOrigin, context.GetTextureUVOrigin(data.renderAttachment));
                    });
                }

                var result = m_RenderGraph.CompileNativeRenderGraph(m_RenderGraph.ComputeGraphHash());
                var passes = result.contextData.GetNativePasses();

                Assert.AreEqual(2, passes.Count);
                Assert.AreEqual(2, passes[0].attachments.size);
                Assert.AreEqual(1, passes[0].numGraphPasses);
                Assert.AreEqual(1, passes[0].numNativeSubPasses);

                Assert.AreEqual(2, passes[1].attachments.size);
                Assert.AreEqual(2, passes[1].numGraphPasses);
                Assert.AreEqual(2, passes[1].numNativeSubPasses);
            };

            m_Camera.Render();

            m_RenderGraph.Cleanup();
        }
    }
}
