using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Tests
{
    class NativePassCompilerRenderGraphTests
    {
        class RenderGraphTestPassData
        {
            public TextureHandle[] textures = new TextureHandle[8];
            public BufferHandle[] buffers = new BufferHandle[8];
        }

        TextureDesc SimpleTextureDesc(string name, int w, int h, int samples)
        {
            TextureDesc result = new TextureDesc(w, h);
            result.msaaSamples = (MSAASamples)samples;
            result.colorFormat = GraphicsFormat.R8G8B8A8_UNorm;
            result.name = name;
            return result;
        }

        class TestBuffers
        {
            public TextureHandle backBuffer;
            public TextureHandle depthBuffer;
            public TextureHandle[] extraBuffers = new TextureHandle[10];
            public TextureHandle extraDepthBuffer;
        };

        TestBuffers ImportAndCreateBuffers(RenderGraph g)
        {
            TestBuffers result = new TestBuffers();
            var backBuffer = BuiltinRenderTextureType.CameraTarget;
            var backBufferHandle = RTHandles.Alloc(backBuffer, "Backbuffer Color");
            var depthBuffer = BuiltinRenderTextureType.Depth;
            var depthBufferHandle = RTHandles.Alloc(depthBuffer, "Backbuffer Depth");
            var extraDepthBufferHandle = RTHandles.Alloc(depthBuffer, "Extra Depth Buffer");

            RenderTargetInfo importInfo = new RenderTargetInfo();
            RenderTargetInfo importInfoDepth = new RenderTargetInfo();
            importInfo.width = 1024;
            importInfo.height = 768;
            importInfo.volumeDepth = 1;
            importInfo.msaaSamples = 1;
            importInfo.format = GraphicsFormat.R16G16B16A16_SFloat;
            result.backBuffer = g.ImportTexture(backBufferHandle, importInfo);

            importInfoDepth = importInfo;
            importInfoDepth.format = GraphicsFormat.D32_SFloat_S8_UInt;
            result.depthBuffer = g.ImportTexture(depthBufferHandle, importInfoDepth);

            importInfo.format = GraphicsFormat.D24_UNorm;
            result.extraDepthBuffer = g.ImportTexture(extraDepthBufferHandle, importInfoDepth);

            for (int i = 0; i < result.extraBuffers.Length; i++)
            {
                result.extraBuffers[i] = g.CreateTexture(SimpleTextureDesc("ExtraBuffer" + i, 1024, 768, 1));
            }

            return result;
        }

        RenderGraph AllocateRenderGraph()
        {
            RenderGraph g = new RenderGraph();
            g.nativeRenderPassesEnabled = true;
            return g;
        }

        [Test]
        public void SimpleMergePasses()
        {
            var g = AllocateRenderGraph();

            var buffers = ImportAndCreateBuffers(g);

            // Render something to 0,1
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[1], 1, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render extra bits to 1
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }
            // Render to final buffer
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[1], 1, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(4, passes[0].attachments.size);
            Assert.AreEqual(3, passes[0].numGraphPasses);

            ref var firstAttachment = ref passes[0].attachments[0];
            Assert.AreEqual(RenderBufferLoadAction.Load, firstAttachment.loadAction);

            ref var secondAttachment = ref passes[0].attachments[1];
            Assert.AreEqual(RenderBufferLoadAction.Clear, secondAttachment.loadAction);

            ref var thirdAttachment = ref passes[0].attachments[2];
            Assert.AreEqual(RenderBufferLoadAction.Clear, thirdAttachment.loadAction);

            ref var fourthAttachment = ref passes[0].attachments[3];
            Assert.AreEqual(RenderBufferLoadAction.Load, fourthAttachment.loadAction);
        }

        /*[Test]
        public void MergeNonRenderPasses()
        {
            RenderGraph g = new RenderGraph();

            var buffers = ImportAndCreateBuffers(g);

            // Render something to 0,1
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[1], 1, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render extra bits to 1
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                // This does something like CommandBufffer.SetGlobal or something that doesn't do any rendering
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
                builder.Dispose();
            }
            // Render to final buffer
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[1], 1, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(4, passes[0].attachments.size);
            Assert.AreEqual(3, passes[0].numGraphPasses);
            Assert.AreEqual(2, passes[0].numNativeSubPasses);
        }*/

        [Test]
        public void MergeDepthPassWithNoDepthPass()
        {
            var g = AllocateRenderGraph();
            var buffers = ImportAndCreateBuffers(g);

            // depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }
            // with no depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.EndOfGraph, passes[0].breakAudit.reason);
        }

        [Test]
        public void MergeNoDepthPassWithDepthPass()
        {
            var g = AllocateRenderGraph();
            var buffers = ImportAndCreateBuffers(g);

            // no depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }
            // with depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.EndOfGraph, passes[0].breakAudit.reason);
        }

        [Test]
        public void MergeMultiplePassesDifferentDepth()
        {
            var g = AllocateRenderGraph();
            var buffers = ImportAndCreateBuffers(g);

            // no depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }
            // with depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }
            // with no depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.EndOfGraph, passes[0].breakAudit.reason);
        }

        [Test]
        public void MergeDifferentDepthFormatsBreaksPass()
        {
            var g = AllocateRenderGraph();
            var buffers = ImportAndCreateBuffers(g);

            // depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }
            // with different depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.SetRenderAttachmentDepth(buffers.extraDepthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.DifferentDepthTextures, passes[0].breakAudit.reason);
        }

        [Test]
        public void NonFragmentUseBreaksPass()
        {
            var g = AllocateRenderGraph();

            var buffers = ImportAndCreateBuffers(g);

            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.UseTexture(buffers.extraBuffers[0], AccessFlags.Read);
                builder.SetRenderAttachment(buffers.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.NextPassReadsTexture, passes[0].breakAudit.reason);
        }


        [Test]
        public void NonRasterBreaksPass()
        {
            var g = AllocateRenderGraph();

            var buffers = ImportAndCreateBuffers(g);

            // No depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Compute touches extraBuffers[0]
            {
                var builder = g.AddComputePass<RenderGraphTestPassData>("ComputePass", out var passData);
                builder.UseTexture(buffers.extraBuffers[0], AccessFlags.ReadWrite);
                builder.SetRenderFunc((RenderGraphTestPassData data, ComputeGraphContext context) => { });
                builder.Dispose();
            }

            // With depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 1, AccessFlags.Read);
                builder.SetRenderAttachment(buffers.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.NonRasterPass, passes[0].breakAudit.reason);
        }

        [Test]
        public void TooManyAttachmentsBreaksPass()
        {
            var g = AllocateRenderGraph();

            var buffers = ImportAndCreateBuffers(g);

            // 8 attachments
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                for (int i = 0; i < 6; i++)
                {
                    builder.SetRenderAttachment(buffers.extraBuffers[i], i, AccessFlags.Write);
                }
                builder.SetRenderAttachment(buffers.backBuffer, 7, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // 2 additional attachments
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                for (int i = 0; i < 2; i++)
                {
                    builder.SetRenderAttachment(buffers.extraBuffers[i + 6], i, AccessFlags.Write);
                }
                builder.SetRenderAttachment(buffers.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.AttachmentLimitReached, passes[0].breakAudit.reason);
        }

        [Test]
        public void NativeSubPassesLimitNotExceeded()
        {
            var g = AllocateRenderGraph();
            var buffers = ImportAndCreateBuffers(g);

            // Native subpasses limit is 8 so go above
            for (int i = 0; i < Rendering.RenderGraphModule.NativeRenderPassCompiler.NativePassCompiler.k_MaxSubpass + 2; i++)
            {
                using var builder = g.AddRasterRenderPass<RenderGraphTestPassData>($"TestPass_{i}", out var passData);
                builder.SetInputAttachment(buffers.extraBuffers[1 - i % 2], 0);

                builder.SetRenderAttachmentDepth(buffers.depthBuffer);
                builder.SetRenderAttachment(buffers.extraBuffers[i % 2], 1);

                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(Rendering.RenderGraphModule.NativeRenderPassCompiler.NativePassCompiler.k_MaxSubpass, passes[0].numGraphPasses);
            Assert.AreEqual(Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.SubPassLimitReached, passes[0].breakAudit.reason);
        }

        [Test]
        public void AllocateFreeInMergedPassesWorks()
        {
            var g = AllocateRenderGraph();

            var buffers = ImportAndCreateBuffers(g);

            // Render something to extra 0
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render extra bits to extra 1, this causes 1 to be allocated in pass 1 which will be the first sub pass of the merged native pass
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[1], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render extra bits to extra 2, this causes 2 to be allocated in pass 2 which will be the second sub pass of the merged native pass
            // It's also the last time extra 1 is used so it gets freed
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[1], 0, AccessFlags.ReadWrite);
                builder.SetRenderAttachment(buffers.extraBuffers[2], 1, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render to final buffer
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[2], 1, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(5, passes[0].attachments.size); //3 extra + color + depth
            Assert.AreEqual(4, passes[0].numGraphPasses);

            // Pass 1 first used = {extra 1}
            List<ResourceHandle> firstUsed = new List<ResourceHandle>();
            ref var pass1Data = ref result.contextData.passData.ElementAt(1);
            foreach (ref readonly var res in pass1Data.FirstUsedResources(result.contextData))
                firstUsed.Add(res);

            Assert.AreEqual(1, firstUsed.Count);
            Assert.AreEqual(buffers.extraBuffers[1].handle.index, firstUsed[0].index);

            // Pass 2 last used = {
            List<ResourceHandle> lastUsed = new List<ResourceHandle>();
            ref var pass2Data = ref result.contextData.passData.ElementAt(2);
            foreach (ref readonly var res in pass2Data.LastUsedResources(result.contextData))
                lastUsed.Add(res);

            Assert.AreEqual(1, lastUsed.Count);
            Assert.AreEqual(buffers.extraBuffers[1].handle.index, lastUsed[0].index);

        }

        [Test]
        public void MemorylessWorks()
        {
            var g = AllocateRenderGraph();

            var buffers = ImportAndCreateBuffers(g);

            // Render something to extra 0
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render to final buffer
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.ReadWrite);
                builder.SetRenderAttachment(buffers.backBuffer, 1, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(3, passes[0].attachments.size); //1 extra + color + depth
            Assert.AreEqual(2, passes[0].numGraphPasses);

            // Pass 0 : first used = {depthBuffer, extraBuffers[0]}
            List<ResourceHandle> firstUsed = new List<ResourceHandle>();
            ref var pass0Data = ref result.contextData.passData.ElementAt(0);
            foreach (ref readonly var res in pass0Data.FirstUsedResources(result.contextData))
                firstUsed.Add(res);

            //Extra buffer 0 should be memoryless
            Assert.AreEqual(2, firstUsed.Count);
            Assert.AreEqual(buffers.extraBuffers[0].handle.index, firstUsed[1].index);
            ref var info = ref result.contextData.UnversionedResourceData(firstUsed[1]);
            Assert.AreEqual(true, info.memoryLess);

            // Pass 1 : last used = {depthBuffer, extraBuffers[0], backBuffer}
            List<ResourceHandle> lastUsed = new List<ResourceHandle>();
            ref var pass1Data = ref result.contextData.passData.ElementAt(1);
            foreach (var res in pass1Data.LastUsedResources(result.contextData))
                lastUsed.Add(res);

            Assert.AreEqual(3, lastUsed.Count);
            Assert.AreEqual(buffers.extraBuffers[0].handle.index, lastUsed[1].index);
        }

        [Test]
        public void InputAttachmentsWork()
        {
            var g = AllocateRenderGraph();

            var buffers = ImportAndCreateBuffers(g);

            // Render something to extra 0,1,2
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[1], 1, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[2], 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render to final buffer using extra 0 as attachment
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.ReadWrite);
                builder.SetRenderAttachment(buffers.backBuffer, 1, AccessFlags.Write);
                builder.SetInputAttachment(buffers.extraBuffers[0], 0, AccessFlags.Read);
                builder.SetInputAttachment(buffers.extraBuffers[1], 1, AccessFlags.Read);
                builder.SetInputAttachment(buffers.extraBuffers[2], 2, AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var nativePasses = result.contextData.GetNativePasses();

            Assert.AreEqual(1, nativePasses.Count);
            Assert.AreEqual(5, nativePasses[0].attachments.size); //3 extra + color + depth
            Assert.AreEqual(2, nativePasses[0].numGraphPasses);

            // Validate attachments
            Assert.AreEqual(buffers.depthBuffer.handle.index, nativePasses[0].attachments[0].handle.index);
            Assert.AreEqual(buffers.extraBuffers[0].handle.index, nativePasses[0].attachments[1].handle.index);
            Assert.AreEqual(buffers.extraBuffers[1].handle.index, nativePasses[0].attachments[2].handle.index);
            Assert.AreEqual(buffers.extraBuffers[2].handle.index, nativePasses[0].attachments[3].handle.index);
            Assert.AreEqual(buffers.backBuffer.handle.index, nativePasses[0].attachments[4].handle.index);

            // Sub Pass 0
            ref var subPass = ref result.contextData.nativeSubPassData.ElementAt(nativePasses[0].firstNativeSubPass);
            Assert.AreEqual(0, subPass.inputs.Length);

            Assert.AreEqual(3, subPass.colorOutputs.Length);
            Assert.AreEqual(1, subPass.colorOutputs[0]);
            Assert.AreEqual(2, subPass.colorOutputs[1]);
            Assert.AreEqual(3, subPass.colorOutputs[2]);

            // Sub Pass 1
            ref var subPass2 = ref result.contextData.nativeSubPassData.ElementAt(nativePasses[0].firstNativeSubPass + 1);
            Assert.AreEqual(3, subPass2.inputs.Length);
            Assert.AreEqual(1, subPass2.inputs[0]);
            Assert.AreEqual(2, subPass2.inputs[1]);
            Assert.AreEqual(3, subPass2.inputs[2]);

            Assert.AreEqual(1, subPass2.colorOutputs.Length);
            Assert.AreEqual(4, subPass2.colorOutputs[0]);
        }

        [Test]
        public void ImportParametersWork()
        {
            var g = AllocateRenderGraph();
            var buffers = ImportAndCreateBuffers(g);

            // Import with parameters
            var backBuffer = BuiltinRenderTextureType.CameraTarget;
            var backBufferHandle = RTHandles.Alloc(backBuffer, "Test Import");

            RenderTargetInfo importInfo = new RenderTargetInfo();
            importInfo.width = 1024;
            importInfo.height = 768;
            importInfo.msaaSamples = 1;
            importInfo.volumeDepth = 1;
            importInfo.format = GraphicsFormat.R16G16B16A16_SFloat;

            ImportResourceParams importResourceParams = new ImportResourceParams();
            importResourceParams.clearOnFirstUse = true;
            importResourceParams.discardOnLastUse = true;
            var importedTexture = g.ImportTexture(backBufferHandle, importInfo, importResourceParams);

            // Render something to importedTexture
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachment(importedTexture, 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Compute does something or other
            {
                var builder = g.AddComputePass<RenderGraphTestPassData>("ComputePass", out var passData);
                builder.UseTexture(buffers.extraBuffers[0], AccessFlags.ReadWrite);
                builder.SetRenderFunc((RenderGraphTestPassData data, ComputeGraphContext context) => { });
                builder.Dispose();
            }

            // Render to final buffer
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(importedTexture, 1, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            // Validate nr pass 0
            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(1, passes[0].attachments.size);
            Assert.AreEqual(1, passes[0].numGraphPasses);

            // Clear on first use
            ref var firstAttachment = ref passes[0].attachments[0];
            Assert.AreEqual(RenderBufferLoadAction.Clear, firstAttachment.loadAction);

            // Validate nr pass 1
            Assert.AreEqual(3, passes[1].attachments.size);
            Assert.AreEqual(1, passes[1].numGraphPasses);

            // Discard on last use
            Assert.AreEqual(RenderBufferStoreAction.DontCare, passes[1].attachments[1].storeAction);
            // Regular imports do a full load/store
            Assert.AreEqual(RenderBufferLoadAction.Load, passes[1].attachments[2].loadAction);
            Assert.AreEqual(RenderBufferStoreAction.Store, passes[1].attachments[2].storeAction);
        }

        [Test]
        public void FencesWork()
        {
            var g = AllocateRenderGraph();
            var buffers = ImportAndCreateBuffers(g);

            { // Pass #1: Render pass writing to backbuffer
                using var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("#1 RenderPass", out _);
                builder.UseTexture(buffers.backBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            { // Pass #2: Async compute pass writing to back buffer
                using var builder = g.AddComputePass<RenderGraphTestPassData>("#2 AsyncComputePass", out _);
                builder.EnableAsyncCompute(true);
                builder.UseTexture(buffers.backBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, ComputeGraphContext context) => { });
            }

            { // Pass #3: Render pass writing to backbuffer
                using var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("#3 RenderPass", out _);
                builder.UseTexture(buffers.backBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            { // Pass #4: Render pass writing to backbuffer
                using var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("#4 RenderPass", out _);
                builder.UseTexture(buffers.backBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passData = result.contextData.passData;

            // #1 waits for nothing, inserts a fence
            Assert.AreEqual(-1, passData[0].waitOnGraphicsFencePassId);
            Assert.True(passData[0].insertGraphicsFence);

            // #2 (async compute) pass waits on #1, inserts a fence
            Assert.AreEqual(0, passData[1].waitOnGraphicsFencePassId);
            Assert.True(passData[1].insertGraphicsFence);

            // #3 waits on #2 (async compute) pass, doesn't insert a fence
            Assert.AreEqual(1, passData[2].waitOnGraphicsFencePassId);
            Assert.False(passData[2].insertGraphicsFence);

            // #4 waits for nothing, doesn't insert a fence
            Assert.AreEqual(-1, passData[3].waitOnGraphicsFencePassId);
            Assert.False(passData[3].insertGraphicsFence);
        }

        [Test]
        public void BuffersWork()
        {
            var g = AllocateRenderGraph();
            var rendertargets = ImportAndCreateBuffers(g);

            var desc = new BufferDesc(1024, 16);
            var buffer = g.CreateBuffer(desc);

            // Render something to extra 0 and write uav
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachmentDepth(rendertargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(rendertargets.extraBuffers[0], 0, AccessFlags.Write);
                builder.UseBufferRandomAccess(buffer, 1, AccessFlags.ReadWrite);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render extra bits to 0 reading from the uav
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.SetRenderAttachmentDepth(rendertargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(rendertargets.extraBuffers[0], 0, AccessFlags.Write);
                builder.UseBuffer(buffer, AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render to final buffer
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.UseTexture(rendertargets.extraBuffers[0]);
                builder.SetRenderAttachment(rendertargets.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(rendertargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            // Validate Pass 0 : uav is first used and created
            ref var pass0Data = ref result.contextData.passData.ElementAt(0);
            var firstUsedList = pass0Data.FirstUsedResources(result.contextData).ToArray();

            Assert.AreEqual(3, firstUsedList.Length);
            Assert.AreEqual(rendertargets.depthBuffer.handle.index, firstUsedList[0].index);
            Assert.AreEqual(RenderGraphResourceType.Texture, firstUsedList[0].type);
            Assert.AreEqual(rendertargets.extraBuffers[0].handle.index, firstUsedList[1].index);
            Assert.AreEqual(RenderGraphResourceType.Texture, firstUsedList[1].type);
            Assert.AreEqual(buffer.handle.index, firstUsedList[2].index);
            Assert.AreEqual(RenderGraphResourceType.Buffer, firstUsedList[2].type);

            var randomAccessList = pass0Data.RandomWriteTextures(result.contextData).ToArray();
            Assert.AreEqual(1, randomAccessList.Length);
            Assert.AreEqual(buffer.handle.index, randomAccessList[0].resource.index);
            Assert.AreEqual(RenderGraphResourceType.Buffer, randomAccessList[0].resource.type);
            Assert.AreEqual(1, randomAccessList[0].index); // we asked for it to be at index 1 in the builder
            Assert.AreEqual(true, randomAccessList[0].preserveCounterValue); // preserve is default

            // Validate Pass 1 : uav buffer is last used and destroyed
            ref var pass1Data = ref result.contextData.passData.ElementAt(1);
            var lastUsedList = pass1Data.LastUsedResources(result.contextData).ToArray();

            Assert.AreEqual(1, lastUsedList.Length);
            Assert.AreEqual(buffer.handle.index, lastUsedList[0].index);
            Assert.AreEqual(RenderGraphResourceType.Buffer, lastUsedList[0].type);

        }

        [Test]
        public void ResolveMSAAImportColor()
        {
            var g = AllocateRenderGraph();
            var buffers = ImportAndCreateBuffers(g);

            // Import with parameters
            // Depth
            var depthBuffer = BuiltinRenderTextureType.Depth;
            var depthBufferHandle = RTHandles.Alloc(depthBuffer, "Test Import Depth");

            RenderTargetInfo importInfoDepth = new RenderTargetInfo();
            importInfoDepth.width = 1024;
            importInfoDepth.height = 768;
            importInfoDepth.msaaSamples = 4;
            importInfoDepth.volumeDepth = 1;
            importInfoDepth.format = GraphicsFormat.D32_SFloat_S8_UInt;

            ImportResourceParams importResourceParams = new ImportResourceParams();
            importResourceParams.clearOnFirstUse = true;
            importResourceParams.discardOnLastUse = true;

            var importedDepth = g.ImportTexture(depthBufferHandle, importInfoDepth, importResourceParams);

            // Color
            var backBuffer = BuiltinRenderTextureType.CameraTarget;
            var backBufferHandle = RTHandles.Alloc(backBuffer, "Test Import Color");

            RenderTargetInfo importInfoColor = new RenderTargetInfo();
            importInfoColor.width = 1024;
            importInfoColor.height = 768;
            importInfoColor.msaaSamples = 4;
            importInfoColor.volumeDepth = 1;
            importInfoColor.format = GraphicsFormat.R16G16B16A16_SFloat;

            var importedColor = g.ImportTexture(backBufferHandle, importInfoColor, importResourceParams);

            // Render something to importedColor and importedDepth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass", out var passData);
                builder.SetRenderAttachmentDepth(importedDepth, AccessFlags.Write);
                builder.SetRenderAttachment(importedColor, 1, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            // Validate nr pass
            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(2, passes[0].attachments.size);
            Assert.AreEqual(1, passes[0].numGraphPasses);

            // Clear on first use
            ref var firstAttachment = ref passes[0].attachments[0];
            Assert.AreEqual(RenderBufferLoadAction.Clear, firstAttachment.loadAction);
            ref var secondAttachment = ref passes[0].attachments[1];
            Assert.AreEqual(RenderBufferLoadAction.Clear, secondAttachment.loadAction);

            // Discard on last use
            Assert.AreEqual(RenderBufferStoreAction.DontCare, passes[0].attachments[0].storeAction);
            // When discarding MSAA color, we only discard the MSAA buffers but keep the resolved texture
            Assert.AreEqual(RenderBufferStoreAction.Resolve, passes[0].attachments[1].storeAction);
        }
    }
}
