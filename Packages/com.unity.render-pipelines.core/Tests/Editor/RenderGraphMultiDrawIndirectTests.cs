using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Rendering.Tests;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Tests
{
    [InitializeOnLoad]
    class RenderGraphMultiDrawIndirectTestsOnLoad
    {
        static RenderGraphMultiDrawIndirectTestsOnLoad()
        {
            ConditionalIgnoreAttribute.AddConditionalIgnoreMapping("IgnoreNoMultiDrawIndirect", !SystemInfo.supportsMultiDrawIndirect);
            ConditionalIgnoreAttribute.AddConditionalIgnoreMapping("IgnoreMultiDrawIndirect", SystemInfo.supportsMultiDrawIndirect);
            ConditionalIgnoreAttribute.AddConditionalIgnoreMapping("IgnoreNoMultiDrawIndirectCountBuffer", !SystemInfo.supportsMultiDrawIndirect || !SystemInfo.supportsMultiDrawIndirectCountBuffer);
            ConditionalIgnoreAttribute.AddConditionalIgnoreMapping("IgnoreMultiDrawIndirectCountBuffer", SystemInfo.supportsMultiDrawIndirectCountBuffer);
        }
    }

    class RenderGraphMultiDrawIndirectTests : RenderGraphTestsCore
    {
        internal enum PassKind
        {
            Raster,
            Unsafe
        }

        internal enum IndexMode
        {
            NonIndexed,
            Indexed
        }

        const int kDrawCount = 4;
        const int kLiveDrawCount = 3;
        const int kVerticesPerDraw = 6;
        const int kTargetSize = kDrawCount;
        const GraphicsFormat kTargetFormat = GraphicsFormat.R8G8B8A8_UNorm;

        static readonly int k_BandCountID = Shader.PropertyToID("_BandCount");

        class DrawPassData
        {
            public TextureHandle target;
            public BufferHandle argsBuffer;
            public BufferHandle indexBuffer;
            public BufferHandle countBuffer;
            public Material material;
            public uint drawCount;
        }

        class ReadbackPassData
        {
            public TextureHandle target;
            public NativeArray<byte> pixels;
        }

        bool m_ReadbackDone;

        [Test]
        public void DrawProceduralIndirect_SingleDraw_RendersOneSubDraw([Values] PassKind passKind, [Values] IndexMode indexMode)
        {
            var pixels = RenderAndReadback(passKind, indexMode, 1, null);
            using (pixels)
                AssertSubDrawsRendered(pixels, 1);
        }

        [Test, ConditionalIgnore("IgnoreNoMultiDrawIndirect", "Multi draw indirect is not supported by this device.")]
        public void DrawProceduralIndirect_MultiDraw_RendersAllSubDraws([Values] PassKind passKind, [Values] IndexMode indexMode)
        {
            var pixels = RenderAndReadback(passKind, indexMode, kDrawCount, null);
            using (pixels)
                AssertSubDrawsRendered(pixels, kDrawCount);
        }

        [Test, ConditionalIgnore("IgnoreNoMultiDrawIndirectCountBuffer", "Multi draw indirect count buffers are not supported by this device.")]
        public void DrawProceduralIndirect_MultiDrawWithCountBuffer_ClampsToCount([Values] PassKind passKind, [Values] IndexMode indexMode)
        {
            var pixels = RenderAndReadback(passKind, indexMode, kDrawCount, kLiveDrawCount);
            using (pixels)
                AssertSubDrawsRendered(pixels, kLiveDrawCount);
        }

        [Test, ConditionalIgnore("IgnoreMultiDrawIndirect", "The device supports multi draw indirect, the unsupported device guard cannot fire.")]
        public void DrawProceduralIndirect_MultiDraw_ThrowsWhenDeviceDoesNotSupportMultiDrawIndirect()
        {
            var material = LoadTestMaterial();
            var argsBuffer = CreateArgsBuffer(IndexMode.NonIndexed);
            var indexedArgsBuffer = CreateArgsBuffer(IndexMode.Indexed);
            var indexBuffer = CreateIndexBuffer();
            var countBuffer = CreateCountBuffer(kLiveDrawCount);
            int renderFuncCount = 0;
            bool recorded = false;

            try
            {
                m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
                {
                    // The camera can render more than once, only record the passes once.
                    if (recorded)
                        return;
                    recorded = true;

                    var target = CreateTarget();

                    using (var builder = m_RenderGraph.AddRasterRenderPass<DrawPassData>("MDI Guard Raster", out var passData))
                    {
                        builder.AllowPassCulling(false);
                        builder.SetRenderAttachment(target, 0, AccessFlags.Write);
                        passData.material = material;

                        builder.SetRenderFunc((DrawPassData data, RasterGraphContext ctx) =>
                        {
                            renderFuncCount++;

                            Assert.Throws<InvalidOperationException>(() => ctx.cmd.DrawProceduralIndirect(
                                Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, argsBuffer, 0, kDrawCount));
                            Assert.Throws<InvalidOperationException>(() => ctx.cmd.DrawProceduralIndirect(
                                indexBuffer, Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, indexedArgsBuffer, 0, kDrawCount));

                            Assert.Throws<InvalidOperationException>(() => ctx.cmd.DrawProceduralIndirect(
                                Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, argsBuffer, 0, drawCount: 1, countBuffer: countBuffer));
                            Assert.Throws<InvalidOperationException>(() => ctx.cmd.DrawProceduralIndirect(
                                indexBuffer, Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, indexedArgsBuffer, 0, drawCount: 1, countBuffer: countBuffer));

                            // A single draw does not need multi draw indirect support, the new parameters must not
                            // change that.
                            Assert.DoesNotThrow(() => ctx.cmd.DrawProceduralIndirect(
                                Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, argsBuffer, 0, 1));
                        });
                    }

                    using (var builder = m_RenderGraph.AddUnsafePass<DrawPassData>("MDI Guard Unsafe", out var passData))
                    {
                        builder.AllowPassCulling(false);
                        builder.UseTexture(target, AccessFlags.Write);
                        passData.target = target;
                        passData.material = material;

                        builder.SetRenderFunc((DrawPassData data, UnsafeGraphContext ctx) =>
                        {
                            renderFuncCount++;

                            ctx.cmd.SetRenderTarget(data.target);

                            Assert.Throws<InvalidOperationException>(() => ctx.cmd.DrawProceduralIndirect(
                                Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, argsBuffer, 0, kDrawCount));
                            Assert.Throws<InvalidOperationException>(() => ctx.cmd.DrawProceduralIndirect(
                                indexBuffer, Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, indexedArgsBuffer, 0, kDrawCount));

                            Assert.Throws<InvalidOperationException>(() => ctx.cmd.DrawProceduralIndirect(
                                Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, argsBuffer, 0, drawCount: 1, countBuffer: countBuffer));
                            Assert.Throws<InvalidOperationException>(() => ctx.cmd.DrawProceduralIndirect(
                                indexBuffer, Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, indexedArgsBuffer, 0, drawCount: 1, countBuffer: countBuffer));

                            Assert.DoesNotThrow(() => ctx.cmd.DrawProceduralIndirect(
                                Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, argsBuffer, 0, 1));
                        });
                    }
                };

                m_Camera.Render();
            }
            finally
            {
                m_RenderGraphTestPipeline.recordRenderGraphBody = null;
                CoreUtils.Destroy(material);
                argsBuffer.Release();
                indexedArgsBuffer.Release();
                indexBuffer.Release();
                countBuffer.Release();
            }

            Assert.IsTrue(recorded, "The render graph body never ran, nothing was recorded.");
            Assert.AreEqual(2, renderFuncCount, "The render functions did not run, nothing was actually asserted.");
        }

        // Records a draw and its readback in a single frame and returns the content of the render target.
        // The returned array is owned by the caller.
        NativeArray<byte> RenderAndReadback(PassKind passKind, IndexMode indexMode, uint drawCount, int? liveDrawCount)
        {
            var material = LoadTestMaterial();
            var argsBuffer = CreateArgsBuffer(indexMode);
            var indexBuffer = indexMode == IndexMode.Indexed ? CreateIndexBuffer() : null;
            var countBuffer = liveDrawCount.HasValue ? CreateCountBuffer(liveDrawCount.Value) : null;

            var pixels = new NativeArray<byte>(kTargetSize * kTargetSize * 4, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            bool recorded = false;
            m_ReadbackDone = false;

            try
            {
                m_RenderGraphTestPipeline.recordRenderGraphBody = (context, camera, cmd) =>
                {
                    // The camera can render more than once, only issue the draw and its readback once.
                    if (recorded)
                        return;
                    recorded = true;

                    var target = CreateTarget();

                    RecordDrawPass(passKind, target, material, argsBuffer, indexBuffer, countBuffer, drawCount);
                    RecordReadbackPass(target, pixels);
                };

                m_Camera.Render();
                AsyncGPUReadback.WaitAllRequests();

                Assert.IsTrue(recorded, "The render graph body never ran, nothing was rendered.");
                Assert.IsTrue(m_ReadbackDone, "The async readback did not complete, the pixels are meaningless.");

                return pixels;
            }
            catch
            {
                pixels.Dispose();
                throw;
            }
            finally
            {
                m_RenderGraphTestPipeline.recordRenderGraphBody = null;
                CoreUtils.Destroy(material);
                argsBuffer.Release();
                indexBuffer?.Release();
                countBuffer?.Release();
            }
        }

        void RecordDrawPass(PassKind passKind, TextureHandle target, Material material, GraphicsBuffer argsBuffer,
            GraphicsBuffer indexBuffer, GraphicsBuffer countBuffer, uint drawCount)
        {
            if (passKind == PassKind.Raster)
            {
                using (var builder = m_RenderGraph.AddRasterRenderPass<DrawPassData>("MDI Draw Raster", out var passData))
                {
                    builder.SetRenderAttachment(target, 0, AccessFlags.Write);
                    SetupDrawPassData(builder, passData, target, material, argsBuffer, indexBuffer, countBuffer, drawCount);

                    builder.SetRenderFunc((DrawPassData data, RasterGraphContext ctx) =>
                    {
                        GraphicsBuffer args = data.argsBuffer;
                        GraphicsBuffer indices = data.indexBuffer;
                        // Invalid handles resolve to null, which is what the API expects when there is no count buffer.
                        GraphicsBuffer count = data.countBuffer;

                        if (indices != null)
                            ctx.cmd.DrawProceduralIndirect(indices, Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, args, 0, data.drawCount, count);
                        else
                            ctx.cmd.DrawProceduralIndirect(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, args, 0, data.drawCount, count);
                    });
                }
            }
            else
            {
                using (var builder = m_RenderGraph.AddUnsafePass<DrawPassData>("MDI Draw Unsafe", out var passData))
                {
                    builder.UseTexture(target, AccessFlags.Write);
                    SetupDrawPassData(builder, passData, target, material, argsBuffer, indexBuffer, countBuffer, drawCount);

                    builder.SetRenderFunc((DrawPassData data, UnsafeGraphContext ctx) =>
                    {
                        GraphicsBuffer args = data.argsBuffer;
                        GraphicsBuffer indices = data.indexBuffer;
                        GraphicsBuffer count = data.countBuffer;

                        ctx.cmd.SetRenderTarget(data.target);
                        ctx.cmd.ClearRenderTarget(false, true, Color.clear);

                        if (indices != null)
                            ctx.cmd.DrawProceduralIndirect(indices, Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, args, 0, data.drawCount, count);
                        else
                            ctx.cmd.DrawProceduralIndirect(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, args, 0, data.drawCount, count);
                    });
                }
            }
        }

        void SetupDrawPassData<TBuilder>(TBuilder builder, DrawPassData passData, TextureHandle target, Material material,
            GraphicsBuffer argsBuffer, GraphicsBuffer indexBuffer, GraphicsBuffer countBuffer, uint drawCount)
            where TBuilder : IBaseRenderGraphBuilder
        {
            builder.AllowPassCulling(false);

            passData.target = target;
            passData.material = material;
            passData.drawCount = drawCount;
            // Pass data comes from a shared pool and is not reset, the optional handles have to be cleared or a
            // previously recorded pass would leak its index and count buffers into this draw.
            passData.indexBuffer = default;
            passData.countBuffer = default;

            passData.argsBuffer = m_RenderGraph.ImportBuffer(argsBuffer);
            builder.UseBuffer(passData.argsBuffer, AccessFlags.Read);

            if (indexBuffer != null)
            {
                passData.indexBuffer = m_RenderGraph.ImportBuffer(indexBuffer);
                builder.UseBuffer(passData.indexBuffer, AccessFlags.Read);
            }

            if (countBuffer != null)
            {
                passData.countBuffer = m_RenderGraph.ImportBuffer(countBuffer);
                builder.UseBuffer(passData.countBuffer, AccessFlags.Read);
            }
        }

        void RecordReadbackPass(TextureHandle target, NativeArray<byte> pixels)
        {
            using (var builder = m_RenderGraph.AddUnsafePass<ReadbackPassData>("MDI Readback", out var passData))
            {
                builder.AllowPassCulling(false);
                builder.UseTexture(target, AccessFlags.Read);

                passData.target = target;
                passData.pixels = pixels;

                builder.SetRenderFunc((ReadbackPassData data, UnsafeGraphContext ctx) =>
                {
                    ctx.cmd.RequestAsyncReadbackIntoNativeArray(ref data.pixels, data.target, 0, kTargetFormat, OnReadbackComplete);
                });
            }
        }

        void OnReadbackComplete(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError && request.done)
                m_ReadbackDone = true;
        }

        TextureHandle CreateTarget()
        {
            return m_RenderGraph.CreateTexture(new TextureDesc(kTargetSize, kTargetSize)
            {
                format = kTargetFormat,
                clearBuffer = true,
                clearColor = Color.clear,
                name = "MDI Target"
            });
        }

        static Material LoadTestMaterial()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.unity.render-pipelines.core/Tests/Editor/MultiDrawIndirectProcedural.shader");
            Assert.IsNotNull(shader, "Failed to load MultiDrawIndirectProcedural shader.");

            var material = new Material(shader);
            material.SetFloat(k_BandCountID, kDrawCount);
            return material;
        }

        static GraphicsBuffer CreateArgsBuffer(IndexMode indexMode)
        {
            int argsPerDraw = indexMode == IndexMode.Indexed ? 5 : 4;
            var args = new uint[kDrawCount * argsPerDraw];

            for (int i = 0; i < kDrawCount; i++)
            {
                int offset = i * argsPerDraw;
                args[offset + 0] = kVerticesPerDraw;                    // vertex or index count per instance
                args[offset + 1] = 1;                                   // instance count
                args[offset + 2] = (uint)(i * kVerticesPerDraw);        // start vertex or index location
                args[offset + argsPerDraw - 1] = 0;                     // start instance location
            }

            var buffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, kDrawCount, argsPerDraw * sizeof(uint));
            buffer.SetData(args);
            return buffer;
        }

        // Identity indices, so that SV_VertexID still equals the start index location plus the corner index.
        static GraphicsBuffer CreateIndexBuffer()
        {
            var indices = new uint[kDrawCount * kVerticesPerDraw];
            for (int i = 0; i < indices.Length; i++)
                indices[i] = (uint)i;

            var buffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, indices.Length, sizeof(uint));
            buffer.SetData(indices);
            return buffer;
        }

        static GraphicsBuffer CreateCountBuffer(int liveDrawCount)
        {
            var buffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint));
            buffer.SetData(new uint[] { (uint)liveDrawCount });
            return buffer;
        }

        // Sub draw i fills one band with (i + 1) * 32 in the red channel. The expected values are compared as a set
        // rather than per row, because the render texture origin differs between graphics APIs.
        static void AssertSubDrawsRendered(NativeArray<byte> pixels, int expectedSubDraws)
        {
            var rendered = new List<int>();
            for (int row = 0; row < kTargetSize; row++)
            {
                int red = pixels[row * kTargetSize * 4];
                for (int column = 1; column < kTargetSize; column++)
                    Assert.AreEqual(red, pixels[(row * kTargetSize + column) * 4], $"Row {row} was not filled by a single sub draw.");

                if (red != 0)
                    rendered.Add(red);
            }

            var expected = new List<int>();
            for (int i = 0; i < expectedSubDraws; i++)
                expected.Add((i + 1) * 32);

            CollectionAssert.AreEquivalent(expected, rendered, "The sub draws that rendered do not match the expected ones.");
        }
    }
}
