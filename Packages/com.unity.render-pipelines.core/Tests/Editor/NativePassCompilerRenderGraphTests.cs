using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler;

namespace UnityEngine.Rendering.Tests
{
    class NativePassCompilerRenderGraphTests
    {
        static Recorder gcAllocRecorder = Recorder.Get("GC.Alloc");

        class RenderGraphTestPassData
        {
            public TextureHandle[] textures = new TextureHandle[8];
            public BufferHandle[] buffers = new BufferHandle[8];
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
            public TextureHandle extraTextureBottomLeft;
            public TextureHandle extraTextureTopLeft;
        };

        TestRenderTargets ImportAndCreateRenderTargets(RenderGraph g)
        {
            TestRenderTargets result = new TestRenderTargets();
            var backBuffer = BuiltinRenderTextureType.CameraTarget;
            var backBufferHandle = RTHandles.Alloc(backBuffer, "Backbuffer Color");
            var depthBuffer = BuiltinRenderTextureType.Depth;
            var depthBufferHandle = RTHandles.Alloc(depthBuffer, "Backbuffer Depth");
            var extraDepthBufferHandle = RTHandles.Alloc(depthBuffer, "Extra Depth Buffer");
            var extraDepthBufferBottomLeftHandle = RTHandles.Alloc(depthBuffer, "Extra Depth Buffer Bottom Left");
            var extraTextureTopLeftHandle = RTHandles.Alloc(backBuffer, "ExtraTextureTopLeft");
            var extraTextureBottomLeftHandle = RTHandles.Alloc(backBuffer, "ExtraTextureBottomLeft");

            ImportResourceParams importParams = new ImportResourceParams();
            importParams.textureUVOrigin = TextureUVOrigin.TopLeft;

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

            importInfoDepth.format = GraphicsFormat.D24_UNorm;
            result.extraDepthBuffer = g.ImportTexture(extraDepthBufferHandle, importInfoDepth, importParams);

            importParams.textureUVOrigin = TextureUVOrigin.BottomLeft;
            result.extraDepthBufferBottomLeft = g.ImportTexture(extraDepthBufferBottomLeftHandle, importInfoDepth, importParams);

            for (int i = 0; i < result.extraTextures.Length; i++)
            {
                result.extraTextures[i] = g.CreateTexture(SimpleTextureDesc("ExtraTexture" + i, 1024, 768, 1));
            }

            importParams.textureUVOrigin = TextureUVOrigin.TopLeft;
            result.extraTextureTopLeft = g.ImportTexture(extraTextureTopLeftHandle, importInfo, importParams);
            importParams.textureUVOrigin = TextureUVOrigin.BottomLeft;
            result.extraTextureBottomLeft = g.ImportTexture(extraTextureBottomLeftHandle, importInfo, importParams);

            return result;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            NativePassCompiler.s_ForceGenerateAuditsForTests = true;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            NativePassCompiler.s_ForceGenerateAuditsForTests = false;
        }

        RenderGraph AllocateRenderGraph()
        {
            RenderGraph g = new RenderGraph();
            return g;
        }

        [Test]
        public void SimpleMergePasses()
        {
            var g = AllocateRenderGraph();

            var renderTargets = ImportAndCreateRenderTargets(g);

            // Render something to 0,1
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[1], 1, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Render extra bits to 1
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
            // Render to final buffer
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[1], 1, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
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
                builder.SetRenderAttachment(buffers.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraTextures[1], 1, AccessFlags.Write);
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
                builder.SetRenderAttachment(buffers.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraTextures[1], 1, AccessFlags.Write);
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
            var renderTargets = ImportAndCreateRenderTargets(g);

            // depth
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // with no depth
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
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
            var renderTargets = ImportAndCreateRenderTargets(g);

            // no depth
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // with depth
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
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
            var renderTargets = ImportAndCreateRenderTargets(g);

            // no depth
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // with depth
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // with no depth
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
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
            var renderTargets = ImportAndCreateRenderTargets(g);

            // depth
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // with different depth
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.extraDepthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.DifferentDepthTextures, passes[0].breakAudit.reason);
        }

        [Test]
        public void MergePassWithWriteAllPass()
        {
            var g = AllocateRenderGraph();

            var renderTargets = ImportAndCreateRenderTargets(g);

            // Render something to depth
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
            // Merge pass, render with WriteAll in extra 0
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.WriteAll);
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(2, passes[0].numGraphPasses);

            // Validate attachments
            Assert.AreEqual(renderTargets.depthBuffer.handle.index, passes[0].attachments[0].handle.index);
            Assert.AreEqual(renderTargets.extraTextures[0].handle.index, passes[0].attachments[1].handle.index);

            ref var depthAttachment = ref passes[0].attachments[0];
            Assert.AreEqual(RenderBufferLoadAction.Load, depthAttachment.loadAction);

            ref var extraAttachment = ref passes[0].attachments[1];
            Assert.AreEqual(RenderBufferLoadAction.DontCare, extraAttachment.loadAction);
        }

        [Test]
        public void MergeWriteAllPassWithReadPass()
        {
            var g = AllocateRenderGraph();

            var renderTargets = ImportAndCreateRenderTargets(g);

            // Render something to extra 0
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.WriteAll);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
            // Read from extra 0
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetInputAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(2, passes[0].numGraphPasses);

            // Validate attachments
            Assert.AreEqual(renderTargets.depthBuffer.handle.index, passes[0].attachments[0].handle.index);
            Assert.AreEqual(renderTargets.extraTextures[0].handle.index, passes[0].attachments[1].handle.index);

            ref var depthAttachment = ref passes[0].attachments[0];
            Assert.AreEqual(RenderBufferLoadAction.Load, depthAttachment.loadAction);

            ref var extraAttachment = ref passes[0].attachments[1];
            Assert.AreEqual(RenderBufferLoadAction.DontCare, extraAttachment.loadAction);
        }

        [Test]
        public void MergeReadPassWithWriteAllPass()
        {
            var g = AllocateRenderGraph();

            var renderTargets = ImportAndCreateRenderTargets(g);

            // Render something to 0,1
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
            // Render to final buffer
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.WriteAll);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(2, passes[0].numGraphPasses);

            // Validate attachments
            Assert.AreEqual(renderTargets.depthBuffer.handle.index, passes[0].attachments[0].handle.index);
            Assert.AreEqual(renderTargets.extraTextures[0].handle.index, passes[0].attachments[1].handle.index);

            ref var depthAttachment = ref passes[0].attachments[0];
            Assert.AreEqual(RenderBufferLoadAction.Load, depthAttachment.loadAction);

            // LoadAction is Clear because it was not used by another pass before, in other case it should be Load
            ref var extraAttachment = ref passes[0].attachments[1];
            Assert.AreEqual(RenderBufferLoadAction.Clear, extraAttachment.loadAction);
        }

        [Test]
        public void MergeDiscardPassWithWrite()
        {
            var g = AllocateRenderGraph();

            var renderTargets = ImportAndCreateRenderTargets(g);

            // Discard extra 0
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Discard);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
            // Read extra 0
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
            // Write to extra 0
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
            // break pass using another depth, Read extra 0
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.extraDepthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(3, passes[0].numGraphPasses);

            // Validate attachments
            Assert.AreEqual(renderTargets.depthBuffer.handle.index, passes[0].attachments[0].handle.index);
            Assert.AreEqual(renderTargets.extraTextures[0].handle.index, passes[0].attachments[1].handle.index);
            Assert.AreEqual(renderTargets.extraTextures[0].handle.index, passes[1].attachments[1].handle.index);

            ref var depthAttachment = ref passes[0].attachments[0];
            Assert.AreEqual(RenderBufferLoadAction.Load, depthAttachment.loadAction);

            // As we have a discard flag in the pass the LoadAction is DontCare, but as it has also a Write flag it should Store the result for the next pass
            ref var extraAttachmentPass0 = ref passes[0].attachments[1];
            Assert.AreEqual(RenderBufferLoadAction.DontCare, extraAttachmentPass0.loadAction);
            Assert.AreEqual(RenderBufferStoreAction.Store, extraAttachmentPass0.storeAction);

            ref var extraAttachmentPass1 = ref passes[1].attachments[1];
            Assert.AreEqual(RenderBufferLoadAction.Load, extraAttachmentPass1.loadAction);

        }

        [Test]
        public void VerifyMergeStateAfterMergingPasses()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            // First pass, not culled.
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
                builder.Dispose();
            }

            // Second pass, culled.
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(true);
                builder.Dispose();
            }

            // Third pass, not culled.
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.IsTrue(passes != null && passes.Count > 0);
            var firstNativePass = passes[0];

            var firstGraphPass = result.contextData.passData.ElementAt(firstNativePass.firstGraphPass);
            var lastGraphPass = result.contextData.passData.ElementAt(firstNativePass.lastGraphPass);
            var middleGraphPass = result.contextData.passData.ElementAt(firstNativePass.lastGraphPass - 1);

            // Only 2 passes since one have been culled
            Assert.IsTrue(firstNativePass.numGraphPasses == 2);

            // All 3 passes including the culled one. We need to add +1 to obtain the correct passes count
            // e.g lastGraphPass index = 2, firstGraphPass index = 0, so 2 - 0 = 2 passes, but we have 3 passes to consider
            // (index 0, 1 and 2) so we add +1 for the correct count.
            Assert.IsTrue(firstNativePass.lastGraphPass - firstNativePass.firstGraphPass + 1 == 3);

            Assert.IsTrue(firstGraphPass.mergeState == PassMergeState.Begin);
            Assert.IsTrue(lastGraphPass.mergeState == PassMergeState.End);
            Assert.IsTrue(middleGraphPass.mergeState == PassMergeState.None);
        }

        [Test]
        public void NonFragmentSamplingBreaksPass()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.UseTexture(renderTargets.extraTextures[0], AccessFlags.Read);
                builder.SetRenderAttachment(renderTargets.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.NextPassReadsTexture, passes[0].breakAudit.reason);
        }

        [Test]
        public void FragmentAfterSamplingWithInputAttachmentBreaksPass()
        {
            var g = AllocateRenderGraph();
            var buffers = ImportAndCreateRenderTargets(g);

            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.UseTexture(buffers.extraTextures[0], AccessFlags.Read);
                builder.SetRenderAttachment(buffers.extraTextures[1], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetInputAttachment(buffers.extraTextures[1], 1, AccessFlags.Read);
                builder.SetRenderAttachment(buffers.extraTextures[0], 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.NextPassTargetsTexture, passes[0].breakAudit.reason);
        }

        [Test]
        public void FragmentAfterSamplingBreaksPass()
        {
            var g = AllocateRenderGraph();
            var buffers = ImportAndCreateRenderTargets(g);

            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.UseTexture(buffers.extraTextures[0], AccessFlags.Read);
                builder.SetRenderAttachment(buffers.extraTextures[1], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraTextures[2], 1, AccessFlags.Read);
                builder.SetRenderAttachment(buffers.extraTextures[0], 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.NextPassTargetsTexture, passes[0].breakAudit.reason);
        }

        [Test]
        public void NonRasterBreaksPass()
        {
            var g = AllocateRenderGraph();

            var renderTargets = ImportAndCreateRenderTargets(g);

            // No depth
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Compute touches extraTextures[0]
            using (var builder = g.AddComputePass<RenderGraphTestPassData>("ComputePass", out var passData))
            {
                builder.UseTexture(renderTargets.extraTextures[0], AccessFlags.ReadWrite);
                builder.SetRenderFunc((RenderGraphTestPassData data, ComputeGraphContext context) => { });
            }

            // With depth
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 1, AccessFlags.Read);
                builder.SetRenderAttachment(renderTargets.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
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
            var renderTargets = ImportAndCreateRenderTargets(g);

            // 8 attachments
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                for (int i = 0; i < 6; i++)
                {
                    builder.SetRenderAttachment(renderTargets.extraTextures[i], i, AccessFlags.Write);
                }
                builder.SetRenderAttachment(renderTargets.backBuffer, 7, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // 2 additional attachments
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                for (int i = 0; i < 2; i++)
                {
                    builder.SetRenderAttachment(renderTargets.extraTextures[i + 6], i, AccessFlags.Write);
                }
                builder.SetRenderAttachment(renderTargets.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
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
            var renderTargets = ImportAndCreateRenderTargets(g);

            // Native subpasses limit is 8 so go above
            for (int i = 0; i < Rendering.RenderGraphModule.NativeRenderPassCompiler.NativePassCompiler.k_MaxSubpass + 2; i++)
            {
                using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>($"TestPass_{i}", out var passData))
                {
                    builder.SetInputAttachment(renderTargets.extraTextures[1 - i % 2], 0);
                    builder.SetRenderAttachmentDepth(renderTargets.depthBuffer);
                    builder.SetRenderAttachment(renderTargets.extraTextures[i % 2], 1);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }
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

            var renderTargets = ImportAndCreateRenderTargets(g);

            // Render something to extra 0
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Render extra bits to extra 1, this causes 1 to be allocated in pass 1 which will be the first sub pass of the merged native pass
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[1], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Render extra bits to extra 2, this causes 2 to be allocated in pass 2 which will be the second sub pass of the merged native pass
            // It's also the last time extra 1 is used so it gets freed
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[1], 0, AccessFlags.ReadWrite);
                builder.SetRenderAttachment(renderTargets.extraTextures[2], 1, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Render to final buffer
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[2], 1, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
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
            Assert.AreEqual(renderTargets.extraTextures[1].handle.index, firstUsed[0].index);

            // Pass 2 last used = {
            List<ResourceHandle> lastUsed = new List<ResourceHandle>();
            ref var pass2Data = ref result.contextData.passData.ElementAt(2);
            foreach (ref readonly var res in pass2Data.LastUsedResources(result.contextData))
                lastUsed.Add(res);

            Assert.AreEqual(1, lastUsed.Count);
            Assert.AreEqual(renderTargets.extraTextures[1].handle.index, lastUsed[0].index);

        }

        [Test]
        public void MemorylessWorks()
        {
            var g = AllocateRenderGraph();

            var renderTargets = ImportAndCreateRenderTargets(g);

            // Render something to extra 0
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Render to final buffer
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.ReadWrite);
                builder.SetRenderAttachment(renderTargets.backBuffer, 1, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(3, passes[0].attachments.size); //1 extra + color + depth
            Assert.AreEqual(2, passes[0].numGraphPasses);

            // Pass 0 : first used = {depthBuffer, extraTextures[0]}
            List<ResourceHandle> firstUsed = new List<ResourceHandle>();
            ref var pass0Data = ref result.contextData.passData.ElementAt(0);
            foreach (ref readonly var res in pass0Data.FirstUsedResources(result.contextData))
                firstUsed.Add(res);

            //Extra buffer 0 should be memoryless
            Assert.AreEqual(2, firstUsed.Count);
            Assert.AreEqual(renderTargets.extraTextures[0].handle.index, firstUsed[1].index);
            ref var info = ref result.contextData.UnversionedResourceData(firstUsed[1]);
            Assert.AreEqual(SystemInfo.supportsMemorylessTextures, info.memoryLess);

            // Pass 1 : last used = {depthBuffer, extraTextures[0], backBuffer}
            List<ResourceHandle> lastUsed = new List<ResourceHandle>();
            ref var pass1Data = ref result.contextData.passData.ElementAt(1);
            foreach (var res in pass1Data.LastUsedResources(result.contextData))
                lastUsed.Add(res);

            Assert.AreEqual(3, lastUsed.Count);
            Assert.AreEqual(renderTargets.extraTextures[0].handle.index, lastUsed[1].index);
        }

        [Test]
        public void InputAttachmentsWork()
        {
            var g = AllocateRenderGraph();

            var renderTargets = ImportAndCreateRenderTargets(g);

            // Render something to extra 0,1,2
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[1], 1, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[2], 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Render to final buffer using extra 0 as attachment
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.ReadWrite);
                builder.SetRenderAttachment(renderTargets.backBuffer, 1, AccessFlags.Write);
                builder.SetInputAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Read);
                builder.SetInputAttachment(renderTargets.extraTextures[1], 1, AccessFlags.Read);
                builder.SetInputAttachment(renderTargets.extraTextures[2], 2, AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var nativePasses = result.contextData.GetNativePasses();

            Assert.AreEqual(1, nativePasses.Count);
            Assert.AreEqual(5, nativePasses[0].attachments.size); //3 extra + color + depth
            Assert.AreEqual(2, nativePasses[0].numGraphPasses);

            // Validate attachments
            Assert.AreEqual(renderTargets.depthBuffer.handle.index, nativePasses[0].attachments[0].handle.index);
            Assert.AreEqual(renderTargets.extraTextures[0].handle.index, nativePasses[0].attachments[1].handle.index);
            Assert.AreEqual(renderTargets.extraTextures[1].handle.index, nativePasses[0].attachments[2].handle.index);
            Assert.AreEqual(renderTargets.extraTextures[2].handle.index, nativePasses[0].attachments[3].handle.index);
            Assert.AreEqual(renderTargets.backBuffer.handle.index, nativePasses[0].attachments[4].handle.index);

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
            var renderTargets = ImportAndCreateRenderTargets(g);

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
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachment(importedTexture, 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Compute does something or other
            using (var builder = g.AddComputePass<RenderGraphTestPassData>("ComputePass", out var passData))
            {
                builder.UseTexture(renderTargets.extraTextures[0], AccessFlags.ReadWrite);
                builder.SetRenderFunc((RenderGraphTestPassData data, ComputeGraphContext context) => { });
            }

            // Render to final buffer
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.ReadWrite);
                builder.SetRenderAttachment(importedTexture, 1, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
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
            var renderTargets = ImportAndCreateRenderTargets(g);

            // Pass #1: Render pass writing to backbuffer
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("#1 RenderPass", out _))
            {
                builder.UseTexture(renderTargets.backBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Pass #2: Async compute pass writing to back buffer
            using (var builder = g.AddComputePass<RenderGraphTestPassData>("#2 AsyncComputePass", out _))
            {
                builder.EnableAsyncCompute(true);
                builder.UseTexture(renderTargets.backBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, ComputeGraphContext context) => { });
            }

            // Pass #3: Render pass writing to backbuffer
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("#3 RenderPass", out _))
            {
                builder.UseTexture(renderTargets.backBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Pass #4: Render pass writing to backbuffer
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("#4 RenderPass", out _))
            {
                builder.UseTexture(renderTargets.backBuffer, AccessFlags.Write);
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
        public void MaxReadersAndMaxVersionsAreCorrectForBuffers()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            var desc = new BufferDesc(1024, 16);
            var buffer = g.CreateBuffer(desc);
            var buffer2 = g.CreateBuffer(desc);

            // Render something to extra 0 and write uav
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.UseBufferRandomAccess(buffer, 0, AccessFlags.Write);
                builder.UseBufferRandomAccess(buffer2, 1, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Render extra bits to 0 reading from the uav
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.UseBuffer(buffer, AccessFlags.Read);
                builder.UseBufferRandomAccess(buffer2, 1, AccessFlags.ReadWrite);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());

            // The resource with the biggest MaxReaders is buffer2:
            // 1 implicit read (TestPass0) + 1 explicit read (TestPass1) + 1 for the offset.
            Assert.AreEqual(result.contextData.resources.MaxReaders[(int)RenderGraphResourceType.Buffer], 3);

            // The resource with the biggest MaxVersion is buffer2:
            // 1 explicit write (TestPass0) + 1 explicit readwrite (TestPass1) + 1 for the offset
            Assert.AreEqual(result.contextData.resources.MaxVersions[(int)RenderGraphResourceType.Buffer], 3);
        }

        [Test]
        public void MaxReadersAndMaxVersionsAreCorrectForTextures()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            // Render something to extra 0 and write uav
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.UseTexture(renderTargets.extraTextures[0], AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Render extra bits to 0 reading from the uav
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Read);
                builder.UseTexture(renderTargets.extraTextures[0], AccessFlags.ReadWrite);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Render extra bits to 0 reading from the uav
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.AllowPassCulling(false);
                builder.UseTexture(renderTargets.extraTextures[0], AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());

            // Resources with the biggest MaxReaders are extraTextures[0] and depthBuffer (both being equal):
            // 1 implicit read (TestPass0) + 2 explicit read (TestPass1 & TestPass2) + 1 for the offset
            Assert.AreEqual(result.contextData.resources.MaxReaders[(int)RenderGraphResourceType.Texture], 4);

            // The resource with the biggest MaxVersion is extraTextures[0]:
            // 1 explicit write (TestPass0) + 1 explicit read-write (TestPass1) + 1 for the offset
            Assert.AreEqual(result.contextData.resources.MaxVersions[(int)RenderGraphResourceType.Texture], 3);
        }

        [Test]
        public void MaxReadersAndMaxVersionsAreCorrectForBuffersMultiplePasses()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            var desc = new BufferDesc(1024, 16);
            var buffer = g.CreateBuffer(desc);
            var buffer2 = g.CreateBuffer(desc);

            int indexName = 0;

            for (int i = 0; i < 5; ++i)
            {
                // Render something to extra 0 and write uav
                using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass" + indexName++, out var passData))
                {
                    builder.UseBufferRandomAccess(buffer, 0, AccessFlags.Write);
                    builder.UseBufferRandomAccess(buffer2, 1, AccessFlags.Write);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }

                // Render extra bits to 0 reading from the uav
                using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass" + indexName++, out var passData))
                {
                    builder.UseBuffer(buffer, AccessFlags.Read);
                    builder.UseBufferRandomAccess(buffer2, 1, AccessFlags.ReadWrite);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());

            // The resource with the biggest MaxReaders is buffer2:
            // 5 implicit read (TestPass0-2-4-6-8) + 5 explicit read (TestPass1-3-5-7-9) + 1 for the offset.
            Assert.AreEqual(result.contextData.resources.MaxReaders[(int)RenderGraphResourceType.Buffer], 11);

            // The resource with the biggest MaxVersion is buffer2:
            // 5 explicit write (TestPass0-2-4-6-8) + 5 explicit readwrite (TestPass1-3-5-7-9) + 1 for the offset
            Assert.AreEqual(result.contextData.resources.MaxVersions[(int)RenderGraphResourceType.Buffer], 11);
        }

        [Test]
        public void ResourcesData_MaxReadersAndVersionsPerResourceType()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            var desc = new BufferDesc(1024, 16);
            var buffer = g.CreateBuffer(desc);

            int indexName = 0;

            // TEXTURE PASSES: Create 2 versions with different reader counts
            // Texture Pass 0: Create version 1 of extraTextures[0] (ReadWrite = 1 write + 1 implicit read)
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPassTexture" + indexName++, out var passData))
            {
                builder.AllowPassCulling(false);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.ReadWrite);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
            // Texture Pass 1: Create version 2 of extraTextures[0] (ReadWrite = 1 write + 1 explicit read of version 1)
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPassTexture" + indexName++, out var passData))
            {
                builder.AllowPassCulling(false);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.ReadWrite);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // BUFFER PASSES: Create many versions to test higher reader/version counts
            indexName = 0;
            for (int i = 0; i < 5; ++i)
            {
                // Buffer Pass (Write): Creates version N (1 write + 1 implicit read from previous version)
                using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPassBuffer" + indexName++, out var passData))
                {
                    builder.AllowPassCulling(false);
                    builder.UseBufferRandomAccess(buffer, 0, AccessFlags.Write);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }
                // Buffer Pass (ReadWrite): Creates version N+1 (1 write + 1 explicit read from version N)
                using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPassBuffer" + indexName++, out var passData))
                {
                    builder.AllowPassCulling(false);
                    builder.UseBufferRandomAccess(buffer, 0, AccessFlags.ReadWrite);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            // VERIFY: MaxReaders and MaxVersions are calculated PER RESOURCE TYPE
            // TEXTURE TYPE:
            // 2 readwrite operations = 2 versions + 1 offset = 3 versions/readers.
            Assert.AreEqual(result.contextData.resources.MaxReaders[(int)RenderGraphResourceType.Texture], 3);
            Assert.AreEqual(result.contextData.resources.MaxVersions[(int)RenderGraphResourceType.Texture], 3);

            // BUFFER TYPE:
            // 5 write operations + 5 readwrite operations = 10 versions + 1 offset = 11 versions/readers.
            Assert.AreEqual(result.contextData.resources.MaxReaders[(int)RenderGraphResourceType.Buffer], 11);
            Assert.AreEqual(result.contextData.resources.MaxVersions[(int)RenderGraphResourceType.Buffer], 11);

            // VERIFY: Index calculations work correctly with per-type maximums
            // Get the texture handle from the first native pass attachment
            var textureHandle = passes[0].attachments[0].handle;
            Assert.AreEqual(renderTargets.extraTextures[0].handle.index, passes[0].attachments[0].handle.index);

            // Test Index() calculation uses correct MaxVersions for texture type
            int indexExpected = textureHandle.index * result.contextData.resources.MaxVersions[(int)RenderGraphResourceType.Texture] + textureHandle.version;
            Assert.AreEqual(result.contextData.resources.Index(textureHandle), indexExpected);
            Assert.IsTrue(indexExpected < result.contextData.resources.versionedData[(int)RenderGraphResourceType.Texture].Capacity);

            // Test IndexReader() calculation uses correct MaxReaders for texture type
            int indexReaderExpected = indexExpected * result.contextData.resources.MaxReaders[(int)RenderGraphResourceType.Texture] + 0;
            Assert.AreEqual(result.contextData.resources.IndexReader(textureHandle, 0), indexReaderExpected);
            Assert.IsTrue(indexExpected < result.contextData.resources.readerData[(int)RenderGraphResourceType.Texture].Capacity);
        }

        [Test]
        public void BuffersWork()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            var desc = new BufferDesc(1024, 16);
            var buffer = g.CreateBuffer(desc);

            // Render something to extra 0 and write uav
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.UseBufferRandomAccess(buffer, 1, AccessFlags.ReadWrite);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Render extra bits to 0 reading from the uav
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.UseBuffer(buffer, AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // Render to final buffer
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.UseTexture(renderTargets.extraTextures[0]);
                builder.SetRenderAttachment(renderTargets.backBuffer, 2, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            // Validate Pass 0 : uav is first used and created
            ref var pass0Data = ref result.contextData.passData.ElementAt(0);
            var firstUsedList = pass0Data.FirstUsedResources(result.contextData).ToArray();

            Assert.AreEqual(3, firstUsedList.Length);
            Assert.AreEqual(renderTargets.depthBuffer.handle.index, firstUsedList[0].index);
            Assert.AreEqual(RenderGraphResourceType.Texture, firstUsedList[0].type);
            Assert.AreEqual(renderTargets.extraTextures[0].handle.index, firstUsedList[1].index);
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
            var renderTargets = ImportAndCreateRenderTargets(g);

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
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass", out var passData))
            {
                builder.SetRenderAttachmentDepth(importedDepth, AccessFlags.Write);
                builder.SetRenderAttachment(importedColor, 1, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
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

        [Test]
        public void TransientTexturesCantBeReused()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);
            var textureTransientHandle = TextureHandle.nullHandle;

            // Render something to textureTransientHandle, created locally in the pass.
            // No exception and no error(s) should be thrown in the Console.
            {
                using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                {
                    var textDesc = new TextureDesc(Vector2.one, false, false)
                    {
                        width = 1920,
                        height = 1080,
                        format = GraphicsFormat.B10G11R11_UFloatPack32,
                        clearBuffer = true,
                        clearColor = Color.red,
                        name = "Transient Texture"
                    };
                    textureTransientHandle = builder.CreateTransientTexture(textDesc);

                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }

                Assert.IsTrue(g.m_RenderPasses.Count != 0);
                Assert.IsTrue(g.m_RenderPasses[^1].transientResourceList[(int)textureTransientHandle.handle.type].Count != 0);
            }

            // Try to render something to textureTransientHandle, reusing the previous TextureHandle.
            // UseTexture should throw an exception.
            Assert.Throws<ArgumentException>(() =>
            {
                using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                {
                    builder.UseTexture(textureTransientHandle, AccessFlags.Read | AccessFlags.Write);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }
            });
        }

        [Test]
        public void TransientBuffersCantBeReused()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);
            var bufferTransientHandle = BufferHandle.nullHandle;

            // Render something to textureTransientHandle, created locally in the pass.
            // No error(s) should be thrown in the Console.
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                var prefixBuffer0Desc = new BufferDesc(1920 * 1080, 4, GraphicsBuffer.Target.Raw) { name = "prefixBuffer0" };
                bufferTransientHandle = builder.CreateTransientBuffer(prefixBuffer0Desc);

                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });

                Assert.IsTrue(g.m_RenderPasses.Count != 0);
                Assert.IsTrue(g.m_RenderPasses[^1].transientResourceList[(int)bufferTransientHandle.handle.type].Count != 0);
            }

            // Try to render something to textureTransientHandle, reusing the previous TextureHandle.
            // UseTexture should throw an exception.
            Assert.Throws<ArgumentException>(() =>
            {
                using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                {
                    builder.UseBuffer(bufferTransientHandle, AccessFlags.Read | AccessFlags.Write);
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                }
            });
        }

        [Test]
        public void ChangingGlobalStateDisablesCulling()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            // First pass ; culling should be set to false after calling AllowGlobalStateModification.
            {
                using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
                {
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                    builder.AllowPassCulling(true);
                    builder.AllowGlobalStateModification(true);
                }
            }

            // Second pass ; culling should be set to false even if we are setting it to true after calling AllowGlobalStateModification.
            {
                using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
                {
                    builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                    builder.AllowGlobalStateModification(true);
                    builder.AllowPassCulling(true);
                }
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            Assert.IsTrue(passes != null && passes.Count > 0);
            var firstNativePass = passes[0];

            Assert.IsTrue(firstNativePass.numGraphPasses == 2);
        }

        [Test]
        public void DecreaseResourceVersion_WhenAllProducersExceptFirstAreCulled()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            // Bumping version of exteraTexture within RG to 1 as we write to it in first pass
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            // Bumping version of exteraTexture within RG to 2 as we write to it in second pass
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(true);
            }

            // Bumping version of exteraTexture within RG to 3 as we write to it in third pass
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(true);
            }

            // Bumping version of exteraTexture within RG to 4 as we write to it in forth pass
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass3", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(true);
            }

            // First pass is preserved as requested but other passes are culled
            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            // Only the first pass is preserved
            Assert.IsTrue(passes != null && passes.Count == 1 && passes[0].numGraphPasses == 1);
            // extraTextures version has decreased to 1 as it is only used by the first pass
            Assert.AreEqual(result.contextData.UnversionedResourceData(passes[0].attachments[0].handle).latestVersionNumber, 1);
        }

        [Test]
        public void GraphPassesDoesNotAlloc()
        {
            var g = AllocateRenderGraph();
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1_Culled", out var passData))
            {
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(true);
            }
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            // First pass is preserved as requested but second pass is culled
            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            // Second pass has been culled
            Assert.IsTrue(passes != null && passes.Count == 1 && passes[0].numGraphPasses == 2);
            // Goes into possible alloc path
            Assert.IsFalse(passes[0].lastGraphPass - passes[0].firstGraphPass + 1 == passes[0].numGraphPasses);


            ValidateNoGCAllocs(() =>
            {
                passes[0].GraphPasses(result.contextData);
            });

            // From RenderPassCullingTests.cs
            void ValidateNoGCAllocs(Action action)
            {
                // Warmup - this will catch static c'tors etc.
                CountGCAllocs(action);

                // Actual test.
                var count = CountGCAllocs(action);
                if (count != 0)
                    throw new AssertionException($"Expected 0 GC allocations but there were {count}");
            }

            int CountGCAllocs(Action action)
            {
                gcAllocRecorder.FilterToCurrentThread();
                gcAllocRecorder.enabled = true;

                action();

                gcAllocRecorder.enabled = false;
                return gcAllocRecorder.sampleBlockCount;
            }
        }

        [Test]
        public void UpdateSubpassAttachmentIndices_WhenDepthAttachmentIsAdded()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("NoDepth0_Subpass0", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            // Render Pass
            //   attachments: [extraTextures[0]]
            //   subpass 0: color outputs : [0]

            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("NoDepth1_Subpass0", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            // Render Pass
            //   attachments: [extraTextures[0]]
            //   subpass 0: color outputs : [0]

            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("NoDepth2_Subpass1", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[1], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            // Render Pass
            //   attachments: [extraTextures[0], extraTextures[1]]
            //   subpass 0: color outputs : [0]
            //   subpass 1: color outputs : [1]

            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("NoDepth3_Subpass2", out var passData))
            {
                builder.SetInputAttachment(renderTargets.extraTextures[0], 0);
                builder.SetInputAttachment(renderTargets.extraTextures[1], 1);
                builder.SetRenderAttachment(renderTargets.extraTextures[2], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            // Render Pass
            //   attachments: [extraTextures[0], extraTextures[1], extraTextures[2]]
            //   subpass 0: color outputs : [0]
            //   subpass 1: color outputs : [1]
            //   subpass 2: color outputs : [2], inputs : [0, 1]

            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("Depth_Subpass3", out var passData))
            {
                builder.SetInputAttachment(renderTargets.extraTextures[0], 0);
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetRenderAttachment(renderTargets.extraTextures[3], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            // Render Pass
            //   attachments: [depthBuffer, extraTextures[1], extraTextures[2], extraTextures[0], extraTextures[3]]
            //   subpass 0: color outputs : [0 -> 3]
            //   subpass 1: color outputs : [1]
            //   subpass 2: color outputs : [2], inputs : [0 -> 3, 1]
            //   subpass 3: color outputs : [4], inputs : [3]

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            // All graph passes are merged in the same render pass
            Assert.IsTrue(passes != null && passes.Count == 1 && passes[0].numGraphPasses == 5 && passes[0].numNativeSubPasses == 4);

            // Depth is the first attachment
            Assert.IsTrue(passes[0].attachments[0].handle.index == renderTargets.depthBuffer.handle.index);
            Assert.IsTrue(passes[0].attachments[1].handle.index == renderTargets.extraTextures[1].handle.index);
            Assert.IsTrue(passes[0].attachments[2].handle.index == renderTargets.extraTextures[2].handle.index);
            Assert.IsTrue(passes[0].attachments[3].handle.index == renderTargets.extraTextures[0].handle.index);
            Assert.IsTrue(passes[0].attachments[4].handle.index == renderTargets.extraTextures[3].handle.index);

            // Check first subpass is correctly updated
            ref var subPassDesc0 = ref result.contextData.nativeSubPassData.ElementAt(0);
            Assert.IsTrue(subPassDesc0.colorOutputs.Length == 1);
            Assert.IsTrue(subPassDesc0.colorOutputs[0] == 3);

            // Check second subpass is correctly updated
            ref var subPassDesc1 = ref result.contextData.nativeSubPassData.ElementAt(1);
            Assert.IsTrue(subPassDesc1.colorOutputs.Length == 1);
            Assert.IsTrue(subPassDesc1.colorOutputs[0] == 1);

            // Check third subpass is correctly updated
            ref var subPassDesc2 = ref result.contextData.nativeSubPassData.ElementAt(2);
            Assert.IsTrue(subPassDesc2.colorOutputs.Length == 1);
            Assert.IsTrue(subPassDesc2.colorOutputs[0] == 2);
            Assert.IsTrue(subPassDesc2.inputs.Length == 2);
            Assert.IsTrue(subPassDesc2.inputs[0] == 3);
            Assert.IsTrue(subPassDesc2.inputs[1] == 1);

            // Check fourth subpass with depth is correct
            ref var subPassDesc3 = ref result.contextData.nativeSubPassData.ElementAt(3);
            Assert.IsTrue(subPassDesc3.colorOutputs.Length == 1);
            Assert.IsTrue(subPassDesc3.colorOutputs[0] == 4);
            Assert.IsTrue(subPassDesc3.inputs.Length == 1);
            Assert.IsTrue(subPassDesc3.inputs[0] == 3);
        }

        [Test]
        public void MergePasses_WhenSameShadingRateImage()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("Pass0", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[1], 0);
                builder.SetShadingRateImageAttachment(renderTargets.extraTextures[0]);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            // Same attachments, we should merge in the same subpass as Pass0's one
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("Pass1", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[1], 0);
                builder.SetShadingRateImageAttachment(renderTargets.extraTextures[0]);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            // Same shading rate image but different render attachments, we should stay in the same native render pass but in a different subpass
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("Pass2", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[2], 0);
                builder.SetShadingRateImageAttachment(renderTargets.extraTextures[0]);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            // All graph passes are merged in the same render pass
            Assert.IsTrue(passes != null && passes.Count == 1 && passes[0].numGraphPasses == 3 && passes[0].numNativeSubPasses == 2);

            // If no SRI support, we just discard the API call
            if (ShadingRateInfo.supportsPerImageTile)
            {
                var shadingRateImageAttachmentIndex = passes[0].shadingRateImageIndex;
                Assert.IsTrue(shadingRateImageAttachmentIndex == 1); // always after color and depth attachments
                Assert.IsTrue(passes[0].attachments[shadingRateImageAttachmentIndex].handle.index == renderTargets.extraTextures[0].handle.index);
            }
        }

        [Test]
        public void BreakPasses_WhenNoOrDifferentShadingRateImage()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("Pass0", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[1], 0);
                builder.SetShadingRateImageAttachment(renderTargets.extraTextures[0]);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            // Different SRI, we should break into a new native render pass
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("Pass1", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[1], 0);
                builder.SetShadingRateImageAttachment(renderTargets.extraTextures[2]);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            // No SRI, we should break into a new native render pass
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("Pass2", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[1], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            if (ShadingRateInfo.supportsPerImageTile)
            {
                // All graph passes are in different native render passes
                Assert.IsTrue(passes != null && passes.Count == 3);
            }
            else
            {
                // If no SRI support, we just discard the API call, all graph passes are merged together
                Assert.IsTrue(passes != null && passes.Count == 1);
            }
        }

/* // DepthAttachment bug: https://jira.unity3d.com/projects/SRP/issues/SRP-897
        [Test]
        public void UnusedResourceCulling_CullProducer_WhenVersionsAreNotExplicitlyRead()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            // Bumping version of extraTexture within RG to 1 as we write to it in first pass
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(true);
            }

            // Bumping version of extraTexture within RG to 2 as we write to it in second pass
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(true);
            }

            // Bumping version of extraTexture within RG to 3 as we write to it in third pass
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(true);
            }

            // Bumping version of extraTexture within RG to 4 as we write to it in fourth pass
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass3", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false); // Not culled!
            }

            // Bumping version of extraTexture within RG to 5 as we write to it in fifth pass
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass4", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(true);
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            // All passes are only writing different versions of a non-imported texture that is never explicitly read by anyone
            // Only fourth pass is preserved as requested but other passes are culled            
            Assert.IsTrue(passes != null && passes.Count == 1 && passes[0].numGraphPasses == 1);
            // TestPass3 is the first pass needing extraBuffers[0] so this is the pass allocating it
            Assert.IsTrue(result.contextData.passData.ElementAt(3).FirstUsedResources(result.contextData).Length == 1);
            // extraBuffer version has decreased to 4 as it is written by the fourth pass
            Assert.AreEqual(result.contextData.UnversionedResourceData(passes[0].attachments[0].handle).latestVersionNumber, 4);
        }

        [Test]
        public void UnusedResourceCulling_CullProducer_WhenNoneOfItsWrittenResourcesAreExplicitlyRead()
        {
            var g = AllocateRenderGraph();
            var buffers = ImportAndCreateBuffers(g);

            // This pass implicitly reads version 0 of extraBuffers[0] and writes its version 1 that will be implicitly read in the next pass - dependency
            // It also explicitly reads version 0 of extraBuffers[1] and writes its version 1 but none reads it - no side effect
            // It also implicitly reads version 0 of depthBuffer that is imported in RG but don't write it - no side effect
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachment(buffers.extraBuffers[0], 0, AccessFlags.ReadWrite);
                builder.SetRenderAttachment(buffers.extraBuffers[1], 1, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(buffers.depthBuffer, AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // This pass implicitly reads version 1 of extraBuffers[1] and writes its version 2 that none reads - no side effect
            // It also implicitly reads version 0 of extraBuffers[2] and writes its version 1 that will be explicitly read in the next pass - dependency
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachment(buffers.extraBuffers[1], 1, AccessFlags.Write);
                builder.SetRenderAttachment(buffers.extraBuffers[2], 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // This pass explicitly reads version 1 of extraBuffers[2]
            // We explicitly request this pass not to be culled, so it will be preserved
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderAttachment(buffers.extraBuffers[2], 2, AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            // - TestPass2 can NOT be culled
            // - TestPass1 can NOT be culled as it is an explicit dependency of TestPass2 through extraBuffers[2]
            // - TestPass0 can be culled as it is an implicit dependency of TestPass1 through extraBuffers[1] but none reads extraBuffers[1] so we can break this dependency
            Assert.IsTrue(passes != null && passes.Count == 1 && passes[0].numGraphPasses == 2);
            // extraBuffer[1] latest version remains at 2 but none writes version 1, it is okay since none reads it
            Assert.AreEqual(result.contextData.UnversionedResourceData(passes[0].attachments[0].handle).latestVersionNumber, 2);
            // extraBuffer[2] latest version remains at 1
            Assert.AreEqual(result.contextData.UnversionedResourceData(passes[0].attachments[1].handle).latestVersionNumber, 1);
        }
*/

        [Test]
        public void UnusedResourceCulling_DoNotCullProducer_WhenOneOfItsWrittenResourcesIsExplicitlyRead()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            // This pass implicitly reads version 0 of extraBuffers[0] and writes its version 1 that will be explicitly read in the next pass - dependency
            // It also explicitly reads version 0 of extraBuffers[1] and writes its version 1 but none reads it - no side effect
            // It also implicitly reads version 0 of depthBuffer that is imported in RG but don't write it - no side effect
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.ReadWrite);
                builder.SetRenderAttachment(renderTargets.extraTextures[1], 1, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // This pass explicitly reads version 1 of extraBuffers[1] and writes its version 2 that none reads - no side effect
            // It also implicitly reads version 0 of extraBuffers[2] and writes its version 1 that will be explicitly read in the next pass - dependency
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[1], 1, AccessFlags.ReadWrite);
                builder.SetRenderAttachment(renderTargets.extraTextures[2], 2, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // This pass explicitly reads version 1 of extraBuffers[2]
            // We explicitly request this pass not to be culled, so it will be preserved
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[2], 2, AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            // All passes are preserved,
            // - TestPass2 can't be culled
            // - TestPass1 is an explicit dependency of TestPass2 through extraBuffers[2]
            // - TestPass0 is an explicit dependency of TestPass1 through extraBuffers[1]            
            Assert.IsTrue(passes != null && passes.Count == 1 && passes[0].numGraphPasses == 3);
            // depth last version is 0
            Assert.AreEqual(result.contextData.UnversionedResourceData(passes[0].attachments[0].handle).latestVersionNumber, 0);
            // extraBuffer[0] latest version remains at 1
            Assert.AreEqual(result.contextData.UnversionedResourceData(passes[0].attachments[1].handle).latestVersionNumber, 1);
            // extraBuffer[1] latest version remains at 2
            Assert.AreEqual(result.contextData.UnversionedResourceData(passes[0].attachments[2].handle).latestVersionNumber, 2);
            // extraBuffer[2] latest version remains at 1
            Assert.AreEqual(result.contextData.UnversionedResourceData(passes[0].attachments[3].handle).latestVersionNumber, 1);
        }

        [Test]
        public void UnusedResourceCulling_CullProducer_WhenNextVersionOfProducedResourceIsWrittenAll()
        {
            var g = AllocateRenderGraph();
            var renderTargets = ImportAndCreateRenderTargets(g);

            // This pass implicitly reads version 0 of extraBuffers[0] and writes its version 1 that will be explicitly read in the next pass - dependency
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // This pass explicitly reads version 1 of extraBuffers[0] - no side effect
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // This pass implicitly reads version 1 of extraBuffers[0] and writes its version 2 that will not be read in the next pass - no side effect
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }

            // This pass writes all version 3 of extraBuffers[2]
            // We explicitly request this pass not to be culled, so it will be preserved
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass3", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.extraTextures[0], 0, AccessFlags.WriteAll);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.AllowPassCulling(false);
            }

            var result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            var passes = result.contextData.GetNativePasses();

            // Only last pass is preserved
            // - TestPass3 can't be culled
            // - TestPass2 can be culled as it is has no side effect and writes extraBuffers[0] at version 2 that none reads as TestPass3 rewrites all of extraBuffers[0]
            // - TestPass1 can be culled as it is has no side effect and only reads extraBuffers[0]
            // - TestPass0 can be culled as it is an implicit dependency of TestPass1 through extraBuffers[0] but TestPass1 will be culled
            Assert.IsTrue(passes != null && passes.Count == 1 && passes[0].numGraphPasses == 1);
            // extraBuffer[0] latest version remains at 2
            Assert.AreEqual(result.contextData.UnversionedResourceData(passes[0].attachments[0].handle).latestVersionNumber, 3);
        }

        // Test using a texture as both a texture and render attachment, one will require topleft and one bottom left so this should throw.
        [Test]
        public void TextureUVOrigin_CheckInvalidMixedUVOriginUseTextureCompiler()
        {
            var g = AllocateRenderGraph();
            g.renderTextureUVOriginStrategy = RenderTextureUVOriginStrategy.PropagateAttachmentOrientation; // Switch to the mode we want to test.

            var renderTargets = ImportAndCreateRenderTargets(g);

            // Render something to 0 with a TopLeft depth and color attachment and a BottomLeft texture
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.backBuffer, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                builder.SetInputAttachment(renderTargets.extraTextures[0], 0, AccessFlags.Read);
                builder.UseTexture(renderTargets.extraTextures[0], AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
            Assert.Throws<System.InvalidOperationException>(() =>
            {
                NativePassCompiler result = g.CompileNativeRenderGraph(g.ComputeGraphHash());
            });
        }

        // Test mixing bottom left and top left resources in the same renderpass and check we throw.
        [Test]
        public void TextureUVOrigin_CheckInvalidMixedUVOriginDirect_DepthCheck()
        {
            var g = AllocateRenderGraph();
            g.renderTextureUVOriginStrategy = RenderTextureUVOriginStrategy.PropagateAttachmentOrientation; // Switch to the mode we want to test.

            var renderTargets = ImportAndCreateRenderTargets(g);

            // Render something to 0 with a BottomLeft depth attachment and a TopLeft color target
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachment(renderTargets.backBuffer, 0, AccessFlags.Write);
                Assert.Throws<System.InvalidOperationException>(() =>
                {
                    builder.SetRenderAttachmentDepth(renderTargets.extraDepthBufferBottomLeft, AccessFlags.Write);
                });
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
        }

        // Test mixing bottom left and top left resources in the same renderpass and check we throw.
        [Test]
        public void TextureUVOrigin_CheckInvalidMixedUVOriginDirect_ColorCheck()
        {
            var g = AllocateRenderGraph();
            g.renderTextureUVOriginStrategy = RenderTextureUVOriginStrategy.PropagateAttachmentOrientation; // Switch to the mode we want to test.

            var renderTargets = ImportAndCreateRenderTargets(g);

            // Render something to 0 with a BottomLeft depth attachment and a TopLeft color target
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData))
            {
                builder.SetRenderAttachmentDepth(renderTargets.extraDepthBufferBottomLeft, AccessFlags.Write);
                Assert.Throws<System.InvalidOperationException>(() =>
                {
                    builder.SetRenderAttachment(renderTargets.backBuffer, 0, AccessFlags.Write);
                });
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
        }

        // Test mixing bottom left and top left resources in the same renderpass and check we throw.
        [Test]
        public void TextureUVOrigin_CheckInvalidMixedUVOriginDirect_InputCheck()
        {
            var g = AllocateRenderGraph();
            g.renderTextureUVOriginStrategy = RenderTextureUVOriginStrategy.PropagateAttachmentOrientation; // Switch to the mode we want to test.

            var renderTargets = ImportAndCreateRenderTargets(g);

            // Render something to 0 with a TopLeft depth attachment, a TopLeft color target, TopLeft input attachment and a BottomLeft input attachment
            using (var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass02", out var passData))
            {
                builder.SetInputAttachment(renderTargets.extraTextureBottomLeft, 0, AccessFlags.Read);
                Assert.Throws<System.InvalidOperationException>(() =>
                {
                    builder.SetInputAttachment(renderTargets.extraTextureTopLeft, 0, AccessFlags.Read);
                });
                Assert.Throws<System.InvalidOperationException>(() =>
                {
                    builder.SetRenderAttachment(renderTargets.backBuffer, 0, AccessFlags.Write);
                });
                Assert.Throws<System.InvalidOperationException>(() =>
                {
                    builder.SetRenderAttachmentDepth(renderTargets.depthBuffer, AccessFlags.Write);
                });

                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
            }
        }
    }
}