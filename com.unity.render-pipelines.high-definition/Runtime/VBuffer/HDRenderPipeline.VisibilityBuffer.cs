using System.Collections.Generic;
using UnityEngine.VFX;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {


        internal struct VBufferOutput
        {
            public TextureHandle vBuffer0;
            public TextureHandle vBuffer1;
            public TextureHandle materialDepthBuffer;
            public TextureHandle depthBuffer;
        }

        class VBufferPassData
        {
            public int clusterCount;
            public Material renderVisibilityMaterial;
            public TextureHandle tempColorBuffer;
            public TextureHandle vbuffer0;
            public TextureHandle vbuffer1;
            public TextureHandle materialDepthBuffer;
            public TextureHandle depthBuffer;
            public FrameSettings frameSettings;
        }


        VBufferOutput RenderVBuffer(RenderGraph renderGraph, CullingResults cullingResults, HDCamera hdCamera, TextureHandle tempColorBuffer)
        {
            VBufferOutput vBufferOutput = new VBufferOutput();
            if (InstanceVDataB == null || CompactedVB == null || CompactedIB == null) return vBufferOutput;

            // These flags are still required in SRP or the engine won't compute previous model matrices...
            // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
            hdCamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

            using (var builder = renderGraph.AddRenderPass<VBufferPassData>("VBuffer Prepass", out var passData, ProfilingSampler.Get(HDProfileId.VBufferPrepass)))
            {
                builder.AllowRendererListCulling(false);

                passData.clusterCount = InstanceVDataB.count;
                passData.renderVisibilityMaterial = m_VisibilityBufferMaterial;
                passData.tempColorBuffer = builder.WriteTexture(tempColorBuffer);
                passData.vbuffer0 = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R32_UInt, clearBuffer = true, enableRandomWrite = true, name = "VBuffer 0" }));
                passData.vbuffer1 = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16_UInt, clearBuffer = true, enableRandomWrite = true, name = "VBuffer 1" }));
                passData.materialDepthBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R32_SFloat, clearBuffer = true, enableRandomWrite = true, name = "Material Buffer" }));

                passData.depthBuffer = CreateDepthBuffer(renderGraph, true, hdCamera.msaaSamples);

                builder.UseDepthBuffer(passData.depthBuffer, DepthAccess.ReadWrite);
                builder.UseColorBuffer(passData.vbuffer0, 0);
                builder.UseColorBuffer(passData.vbuffer1, 1);
                builder.UseColorBuffer(passData.materialDepthBuffer, 2);

                passData.frameSettings = hdCamera.frameSettings;

                builder.SetRenderFunc(
                    (VBufferPassData data, RenderGraphContext context) =>
                    {
                        context.cmd.SetGlobalBuffer("_CompactedVertexBuffer", CompactedVB);
                        context.cmd.SetGlobalBuffer("_CompactedIndexBuffer", CompactedIB);
                        context.cmd.SetGlobalBuffer("_InstanceVDataBuffer", InstanceVDataB);
                        context.cmd.DrawProcedural(Matrix4x4.identity, data.renderVisibilityMaterial, 0, MeshTopology.Triangles, VisibilityBufferConstants.s_ClusterSizeInIndices, data.clusterCount);
                    });

                vBufferOutput.vBuffer0 = passData.vbuffer0;
                vBufferOutput.vBuffer1 = passData.vbuffer1;
                vBufferOutput.materialDepthBuffer = passData.materialDepthBuffer;
                vBufferOutput.depthBuffer = passData.depthBuffer;

                PushFullScreenDebugTexture(renderGraph, vBufferOutput.vBuffer0, FullScreenDebugMode.VBufferTriangleId, GraphicsFormat.R32_UInt);
                PushFullScreenDebugTexture(renderGraph, vBufferOutput.vBuffer1, FullScreenDebugMode.VBufferGeometryId, GraphicsFormat.R16_UInt);
                PushFullScreenDebugTexture(renderGraph, vBufferOutput.materialDepthBuffer, FullScreenDebugMode.VBufferMaterialId, GraphicsFormat.R32_SFloat);
            }
            return vBufferOutput;
        }

        class VBufferLightingPassData
        {
            public TextureHandle colorBuffer;
            public TextureHandle vbuffer0;
            public TextureHandle vbuffer1;
            public TextureHandle materialDepthBuffer;
            public TextureHandle depthBuffer;
            public ComputeBufferHandle vertexBuffer;
            public ComputeBufferHandle indexBuffer;
        }

        TextureHandle RenderVBufferLighting(RenderGraph renderGraph, CullingResults cullingResults, HDCamera hdCamera, VBufferOutput vBufferOutput, TextureHandle colorBuffer)
        {
            if (InstanceVDataB == null || CompactedVB == null || CompactedIB == null) return colorBuffer;

            using (var builder = renderGraph.AddRenderPass<VBufferLightingPassData>("VBuffer Lighting", out var passData, ProfilingSampler.Get(HDProfileId.VBufferLighting)))
            {
                builder.AllowRendererListCulling(false);

                passData.colorBuffer = builder.WriteTexture(colorBuffer);
                passData.vbuffer0 = builder.ReadTexture(vBufferOutput.vBuffer0);
                passData.vbuffer1 = builder.ReadTexture(vBufferOutput.vBuffer1);
                passData.materialDepthBuffer = builder.UseDepthBuffer(vBufferOutput.materialDepthBuffer, DepthAccess.Read);
                passData.depthBuffer = builder.ReadTexture(vBufferOutput.materialDepthBuffer);

                builder.UseDepthBuffer(passData.materialDepthBuffer, DepthAccess.ReadWrite);
                builder.UseColorBuffer(colorBuffer, 0);

                builder.SetRenderFunc(
                    (VBufferLightingPassData data, RenderGraphContext context) =>
                    {
                        context.cmd.SetGlobalBuffer("_CompactedVertexBuffer", CompactedVB);
                        context.cmd.SetGlobalBuffer("_CompactedIndexBuffer", CompactedIB);
                        context.cmd.SetGlobalBuffer("_InstanceVDataBuffer", InstanceVDataB);
                        context.cmd.SetGlobalTexture("_VBuffer0", data.vbuffer0);
                        context.cmd.SetGlobalTexture("_VBuffer1", data.vbuffer1);

                        foreach (var material in materials.Keys)
                        {
                            var passIdx = -1;
                            for (int i = 0; i < material.passCount; ++i)
                            {
                                if (material.GetPassName(i).IndexOf("VBufferLighting") >= 0)
                                {
                                    passIdx = i;
                                    break;
                                }
                            }
                            if (passIdx == -1) continue;

                            HDUtils.DrawFullScreen(context.cmd, material, colorBuffer, shaderPassId: passIdx);
                        }
                    });

                PushFullScreenDebugTexture(renderGraph, colorBuffer, FullScreenDebugMode.VBufferLightingDebug);
            }
            return colorBuffer;
        }
    }
}
