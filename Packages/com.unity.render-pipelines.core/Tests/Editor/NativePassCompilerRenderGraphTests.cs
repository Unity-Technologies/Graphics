using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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
        };

        TestBuffers ImportAndCreateBuffers(RenderGraph g)
        {
            TestBuffers result = new TestBuffers();
            var backBuffer = BuiltinRenderTextureType.CameraTarget;
            var backBufferHandle = RTHandles.Alloc(backBuffer, "Backbuffer Color");
            var depthBuffer = BuiltinRenderTextureType.Depth;
            var depthBufferHandle = RTHandles.Alloc(depthBuffer, "Backbuffer Depth");

            RenderTargetInfo importInfo = new RenderTargetInfo();
            RenderTargetInfo importInfoDepth = new RenderTargetInfo();
            importInfo.width = 1024;
            importInfo.height = 768;
            importInfo.volumeDepth = 1;
            importInfo.msaaSamples = 1;
            importInfo.format = GraphicsFormat.R16G16B16A16_SFloat;

            importInfoDepth = importInfo;
            importInfoDepth.format = GraphicsFormat.D32_SFloat_S8_UInt;

            result.backBuffer = g.ImportTexture(backBufferHandle, importInfo);
            result.depthBuffer = g.ImportTexture(depthBufferHandle, importInfoDepth);

            for (int i = 0; i < result.extraBuffers.Length; i++)
            {
                result.extraBuffers[i] = g.CreateTexture(SimpleTextureDesc("ExtraBuffer" + i, 1024, 768, 1));
            }

            return result;
        }

        RenderGraph AllocateRenderGraph()
        {
            RenderGraph g = new RenderGraph();
            g.NativeRenderPassesEnabled = true;
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
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[1], 1, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render extra bits to 1
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
        }
            // Render to final buffer
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[1], 1, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.backBuffer, 2, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph();
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
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[1], 1, IBaseRenderGraphBuilder.AccessFlags.Write);
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
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[1], 1, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.backBuffer, 2, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph();
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(4, passes[0].attachments.size);
            Assert.AreEqual(3, passes[0].numGraphPasses);
            Assert.AreEqual(2, passes[0].numNativeSubPasses);
        }*/

        [Test]
        public void DepthUseMismatch()
        {
            var g = AllocateRenderGraph();

            var buffers = ImportAndCreateBuffers(g);

            // No depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }
            // With depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.backBuffer, 2, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph();
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.DepthBufferUseMismatch, passes[0].breakAudit.reason);
        }

        [Test]
        public void NonFragmentUseBreaksPass()
        {
            var g = AllocateRenderGraph();

            var buffers = ImportAndCreateBuffers(g);

            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTexture(buffers.extraBuffers[0], IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTextureFragment(buffers.backBuffer, 2, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph();
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.NextPassReadsTexture, passes[0].breakAudit.reason);
        }


        [Test]
        public void NonRasterBreaksPass()
        {
            var g = AllocateRenderGraph();

            var buffers = ImportAndCreateBuffers(g);

            // No depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Compute touches extraBuffers[0]
            {
                var builder = g.AddComputePass<RenderGraphTestPassData>("LowLevelPass", out var passData);
                builder.UseTexture(buffers.extraBuffers[0], IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.SetRenderFunc((RenderGraphTestPassData data, ComputeGraphContext context) => { });
                builder.Dispose();
            }

            // With depth
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[0], 1, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTextureFragment(buffers.backBuffer, 2, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph();
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.NonRasterPass, passes[0].breakAudit.reason);
        }

        [Test]
        public void TooManyAttachmentsBreaksPass()
        {
            var g = AllocateRenderGraph();

            var buffers = ImportAndCreateBuffers(g);

            // 8 attachments
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                for (int i = 0; i < 6; i++)
                {
                    builder.UseTextureFragment(buffers.extraBuffers[i], i, IBaseRenderGraphBuilder.AccessFlags.Write);
                }
                builder.UseTextureFragment(buffers.backBuffer, 7, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // 2 additional attachments
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                for (int i = 0; i < 2; i++)
                {
                    builder.UseTextureFragment(buffers.extraBuffers[i+6], i, IBaseRenderGraphBuilder.AccessFlags.Write);
                }
                builder.UseTextureFragment(buffers.backBuffer, 2, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph();
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(2, passes.Count);
            Assert.AreEqual(Experimental.Rendering.RenderGraphModule.NativeRenderPassCompiler.PassBreakReason.AttachmentLimitReached, passes[0].breakAudit.reason);
        }

        [Test]
        public void AllocateFreeInMergedPassesWorks()
        {
            var g = AllocateRenderGraph();

            var buffers = ImportAndCreateBuffers(g);

            // Render something to extra 0
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass0", out var passData);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render extra bits to extra 1, this causes 1 to be allocated in pass 1 which will be the first sub pass of the merged native pass
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass1", out var passData);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[1], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render extra bits to extra 2, this causes 2 to be allocated in pass 2 which will be the second sub pass of the merged native pass
            // It's also the last time extra 1 is used so it gets freed
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[1], 0, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.UseTextureFragment(buffers.extraBuffers[2], 1, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render to final buffer
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[2], 1, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.backBuffer, 2, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph();
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(5, passes[0].attachments.size); //3 extra + color + depth
            Assert.AreEqual(4, passes[0].numGraphPasses);

            // Pass 1 first used = {extra 1}
            List<ResourceHandle> firstUsed = new List<ResourceHandle>();
            foreach (var res in result.contextData.passData[1].FirstUsedResources(result.contextData)) firstUsed.Add(res);

            Assert.AreEqual(1, firstUsed.Count);
            Assert.AreEqual(buffers.extraBuffers[1].handle.index, firstUsed[0].index);

            // Pass 2 last used = {
            List<ResourceHandle> lastUsed = new List<ResourceHandle>();
            foreach (var res in result.contextData.passData[2].LastUsedResources(result.contextData)) lastUsed.Add(res);

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
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render to final buffer
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.UseTextureFragment(buffers.backBuffer, 1, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph();
            var passes = result.contextData.GetNativePasses();

            Assert.AreEqual(1, passes.Count);
            Assert.AreEqual(3, passes[0].attachments.size); //1 extra + color + depth
            Assert.AreEqual(2, passes[0].numGraphPasses);

            // Pass 0 : first used = {depthBuffer, extraBuffers[0]}
            List<ResourceHandle> firstUsed = new List<ResourceHandle>();
            foreach (var res in result.contextData.passData[0].FirstUsedResources(result.contextData)) firstUsed.Add(res);

            //Extra buffer 0 should be memoryless
            Assert.AreEqual(2, firstUsed.Count);
            Assert.AreEqual(buffers.extraBuffers[0].handle.index, firstUsed[1].index);
            ref var info = ref result.contextData.UnversionedResourceData(firstUsed[1]);
            Assert.AreEqual(true, info.memoryLess);

            // Pass 1 : last used = {depthBuffer, extraBuffers[0], backBuffer}
            List<ResourceHandle> lastUsed = new List<ResourceHandle>();
            foreach (var res in result.contextData.passData[1].LastUsedResources(result.contextData)) lastUsed.Add(res);

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
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[1], 1, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.extraBuffers[2], 2, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Render to final buffer using extra 0 as attachment
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.UseTextureFragmentDepth(buffers.depthBuffer, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.UseTextureFragment(buffers.backBuffer, 1, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragmentInput(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTextureFragmentInput(buffers.extraBuffers[1], 1, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTextureFragmentInput(buffers.extraBuffers[2], 2, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph();
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
            ref var subPass = ref result.contextData.nativeSubPassData[nativePasses[0].firstNativeSubPass];
            Assert.AreEqual(0, subPass.inputs.Length);

            Assert.AreEqual(3, subPass.colorOutputs.Length);
            Assert.AreEqual(1, subPass.colorOutputs[0]);
            Assert.AreEqual(2, subPass.colorOutputs[1]);
            Assert.AreEqual(3, subPass.colorOutputs[2]);

            // Sub Pass 1
            ref var subPass2 = ref result.contextData.nativeSubPassData[nativePasses[0].firstNativeSubPass+1];
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
                builder.UseTextureFragment(importedTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            // Compute does something or other
            {
                var builder = g.AddComputePass<RenderGraphTestPassData>("LowLevelPass", out var passData);
                builder.UseTexture(buffers.extraBuffers[0], IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.SetRenderFunc((RenderGraphTestPassData data, ComputeGraphContext context) => { });
                builder.Dispose();
            }

            // Render to final buffer
            {
                var builder = g.AddRasterRenderPass<RenderGraphTestPassData>("TestPass2", out var passData);
                builder.UseTextureFragment(buffers.extraBuffers[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(importedTexture, 1, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(buffers.backBuffer, 2, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.SetRenderFunc((RenderGraphTestPassData data, RasterGraphContext context) => { });
                builder.Dispose();
            }

            var result = g.CompileNativeRenderGraph();
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

    }
}
