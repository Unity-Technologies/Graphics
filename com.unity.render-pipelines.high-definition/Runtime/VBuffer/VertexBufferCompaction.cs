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
        ComputeBuffer CompactedVB = null;
        ComputeBuffer CompactedIB = null;

        [GenerateHLSL(needAccessors = false)]
        internal struct CompactVertex
        {
            public float posX, posY, posZ;
            public uint uv;
            public uint N;
            public uint T;
        }

        void InitVBuffer()
        {
            CompactedVB = null;
            CompactedIB = null;
        }

        void DisposeVBufferStuff()
        {
            CoreUtils.SafeRelease(CompactedIB);
            CoreUtils.SafeRelease(CompactedVB);
        }

        int GetFormatByteCount(VertexAttributeFormat format)
        {
            switch (format)
            {
                case VertexAttributeFormat.Float32: return 4;
                case VertexAttributeFormat.Float16: return 2;
                case VertexAttributeFormat.UNorm8: return 1;
                case VertexAttributeFormat.SNorm8: return 1;
                case VertexAttributeFormat.UNorm16: return 2;
                case VertexAttributeFormat.SNorm16: return 2;
                case VertexAttributeFormat.UInt8: return 1;
                case VertexAttributeFormat.SInt8: return 1;
                case VertexAttributeFormat.UInt16: return 2;
                case VertexAttributeFormat.SInt16: return 2;
                case VertexAttributeFormat.UInt32: return 4;
                case VertexAttributeFormat.SInt32: return 4;
            }
            return 4;
        }

        void AddMeshToCompactedBuffer(ref uint ibStart, ref uint vbStart, Mesh mesh)
        {
            var ib = mesh.GetIndexBuffer();
            var cs = defaultResources.shaders.vbCompactionCS;
            var kernel = cs.FindKernel("VBCompactionKernel");
            mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

            int posStream = mesh.GetVertexAttributeStream(VertexAttribute.Position);
            int posStreamStride = mesh.GetVertexBufferStride(posStream);
            int posOffset = mesh.GetVertexAttributeOffset(VertexAttribute.Position);
            var posVBStream = mesh.GetVertexBuffer(posStream);
            var posFormat = mesh.GetVertexAttributeFormat(VertexAttribute.Position);
            int posBytes = GetFormatByteCount(mesh.GetVertexAttributeFormat(VertexAttribute.Position)) * mesh.GetVertexAttributeDimension(VertexAttribute.Position);

            int uvStream = mesh.GetVertexAttributeStream(VertexAttribute.TexCoord0);
            int uvStreamStride = mesh.GetVertexBufferStride(uvStream);
            int uvOffset = mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord0);
            var uvVBStream = mesh.GetVertexBuffer(uvStream);
            int uvBytes = GetFormatByteCount(mesh.GetVertexAttributeFormat(VertexAttribute.TexCoord0)) * mesh.GetVertexAttributeDimension(VertexAttribute.TexCoord0);

            int normalStream = mesh.GetVertexAttributeStream(VertexAttribute.Normal);
            int normalStreamStride = mesh.GetVertexBufferStride(normalStream);
            int normalOffset = mesh.GetVertexAttributeOffset(VertexAttribute.Normal);
            var normalVBStream = mesh.GetVertexBuffer(normalStream);
            int normalBytes = GetFormatByteCount(mesh.GetVertexAttributeFormat(VertexAttribute.Normal)) * mesh.GetVertexAttributeDimension(VertexAttribute.Normal);

            int tangentStream = mesh.GetVertexAttributeStream(VertexAttribute.Tangent);
            int tangentStreamStride = mesh.GetVertexBufferStride(tangentStream);
            int tangentOffset = mesh.GetVertexAttributeOffset(VertexAttribute.Tangent);
            var tangentVBStream = mesh.GetVertexBuffer(tangentStream);
            int tangentBytes = GetFormatByteCount(mesh.GetVertexAttributeFormat(VertexAttribute.Tangent)) * mesh.GetVertexAttributeDimension(VertexAttribute.Tangent);

            Vector4 uvCompactionParam = new Vector4(uvOffset, mesh.vertexCount, uvStreamStride, vbStart);
            Vector4 normalCompactionParam = new Vector4(normalOffset, mesh.vertexCount, normalStreamStride, vbStart);
            Vector4 posCompactionParam = new Vector4(posOffset, mesh.vertexCount, posStreamStride, vbStart);
            Vector4 tangentCompactionParam = new Vector4(tangentOffset, mesh.vertexCount, tangentStreamStride, vbStart);

            cs.SetVector(HDShaderIDs._UVCompactionParams, uvCompactionParam);
            cs.SetVector(HDShaderIDs._NormalCompactionParams, normalCompactionParam);
            cs.SetVector(HDShaderIDs._PosCompactionParams, posCompactionParam);
            cs.SetVector(HDShaderIDs._TangentCompactionParams, tangentCompactionParam);

            cs.SetBuffer(kernel, HDShaderIDs._InputUVVB, uvVBStream);
            cs.SetBuffer(kernel, HDShaderIDs._InputNormalVB, normalVBStream);
            cs.SetBuffer(kernel, HDShaderIDs._InputPosVB, posVBStream);
            cs.SetBuffer(kernel, HDShaderIDs._InputTangentVB, tangentVBStream);

            cs.SetBuffer(kernel, HDShaderIDs._OutputVB, CompactedVB);

            int dispatchSize = HDUtils.DivRoundUp(mesh.vertexCount, 64);

            cs.Dispatch(kernel, dispatchSize, 1, 1);

            if (mesh.indexFormat == IndexFormat.UInt16)
                kernel = cs.FindKernel("IBCompactionKernelUINT16");
            else
                kernel = cs.FindKernel("IBCompactionKernelUINT32");

            cs.SetBuffer(kernel, HDShaderIDs._InputIB, mesh.GetIndexBuffer());
            cs.SetBuffer(kernel, HDShaderIDs._OutputIB, CompactedIB);
            for (int i = 0; i < mesh.subMeshCount; ++i)
            {
                uint indexCount = mesh.GetIndexCount(i);

                var indexContent = mesh.GetIndices(i);

                Vector4 ibCompactionParams = new Vector4(indexCount, ibStart, vbStart, mesh.GetIndexStart(i));
                cs.SetVector(HDShaderIDs._IBCompactionParams, ibCompactionParams);

                dispatchSize = HDUtils.DivRoundUp((int)indexCount, 64);
                cs.Dispatch(kernel, dispatchSize, 1, 1);

                ibStart += indexCount;
            }


            vbStart += (uint)mesh.vertexCount;
        }

        void CompactAllTheThings()
        {
            int geometryId = 0;
            Dictionary<Mesh, int> meshes = new Dictionary<Mesh, int>();
            HashSet<Material> materials = new HashSet<Material>();
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

            // Grab all the renderers from the scene
            var rendererArray = UnityEngine.GameObject.FindObjectsOfType<MeshRenderer>();
            for (var i = 0; i < rendererArray.Length; i++)
            {
                // Fetch the current renderer
                MeshRenderer currentRenderer = rendererArray[i];

                // If it is not active skip it
                if (currentRenderer.enabled == false) continue;

                // Grab the current game object
                GameObject gameObject = currentRenderer.gameObject;

                if (gameObject.TryGetComponent<ReflectionProbe>(out reflectionProbe)) continue;

                currentRenderer.TryGetComponent(out MeshFilter meshFilter);
                if (meshFilter == null || meshFilter.sharedMesh == null) continue;

                int index = 0;
                if(!meshes.TryGetValue(meshFilter.sharedMesh, out index))
                {
                    meshes.Add(meshFilter.sharedMesh, geometryId);
                    index = geometryId;
                    geometryId++;
                }

                // Get the current value of the material properties in the renderer.
                currentRenderer.GetPropertyBlock(propBlock);

                // Assign our new value.
                propBlock.SetInt("_GeometryId", index);

                // Apply the edited values to the renderer.
                currentRenderer.SetPropertyBlock(propBlock);

                foreach (var mat in currentRenderer.sharedMaterials)
                    materials.Add(mat);
            }

            // Assign indices to materials
            int materialIdx = 0;
            foreach(var material in materials)
            {
                material.SetInt("_MaterialId", materialIdx);
                materialIdx++;
            }

            // Assign Ids to geometries
            for (var i = 0; i < rendererArray.Length; i++)
            {
                // Fetch the current renderer
                MeshRenderer currentRenderer = rendererArray[i];

            }

            int vertexCount = 0;
            int indexCount = 0;
            foreach (var mesh in meshes)
            {
                vertexCount += mesh.Key.vertexCount;
                indexCount += mesh.Key.GetIndexBuffer().count;
            }

            int currVBCount = CompactedVB == null ? 0 : CompactedVB.count;
            if (vertexCount != currVBCount)
            {
                if (CompactedVB != null && CompactedIB != null)
                {
                    CoreUtils.SafeRelease(CompactedIB);
                    CoreUtils.SafeRelease(CompactedVB);
                }

                var stride = System.Runtime.InteropServices.Marshal.SizeOf<CompactVertex>();
                CompactedVB = new ComputeBuffer(vertexCount, stride);
                CompactedIB = new ComputeBuffer(indexCount, sizeof(int));
            }
            uint vbStart = 0;
            uint ibStart = 0;

            foreach (var mesh in meshes)
            {
                AddMeshToCompactedBuffer(ref vbStart, ref ibStart, mesh.Key);
            }
        }

        internal struct VBufferOutput
        {
            public TextureHandle vbuffer0;
            public TextureHandle vbuffer1;
            public TextureHandle materialDepthBuffer;
            public TextureHandle depthBuffer;
        }

        class VBufferPassData
        {
            public TextureHandle tempColorBuffer;
            public TextureHandle vbuffer0;
            public TextureHandle vbuffer1;
            public TextureHandle materialDepthBuffer;
            public TextureHandle depthBuffer;
            public FrameSettings frameSettings;
            public RendererList rendererList;
        }

        VBufferOutput RenderVBuffer(RenderGraph renderGraph, CullingResults cullingResults, HDCamera hdCamera, TextureHandle tempColorBuffer)
        {
            VBufferOutput vBufferOutput = new VBufferOutput();

            // These flags are still required in SRP or the engine won't compute previous model matrices...
            // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
            hdCamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

            using (var builder = renderGraph.AddRenderPass<VBufferPassData>("VBuffer Prepass", out var passData, ProfilingSampler.Get(HDProfileId.VBufferPrepass)))
            {
                builder.AllowRendererListCulling(false);

                passData.tempColorBuffer = builder.WriteTexture(tempColorBuffer);
                passData.vbuffer0 = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R32_UInt, enableRandomWrite = true, name = "VBuffer 0" }));
                /*
                passData.vbuffer1 = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16_UInt, enableRandomWrite = true, name = "VBuffer 1" }));
                passData.materialDepthBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Material Buffer" }));
                */
                passData.depthBuffer = CreateDepthBuffer(renderGraph, hdCamera.clearDepth, hdCamera.msaaSamples);

                builder.UseDepthBuffer(passData.depthBuffer, DepthAccess.ReadWrite);
                builder.UseColorBuffer(passData.vbuffer0, 0);
                /*
                builder.UseColorBuffer(passData.vbuffer1, 1);
                builder.UseColorBuffer(passData.materialDepthBuffer, 2);
                */

                passData.frameSettings = hdCamera.frameSettings;
                var opaqueRenderList = CreateOpaqueRendererListDesc(
                    cullingResults, hdCamera.camera, m_VBufferNames,
                    renderQueueRange: HDRenderQueue.k_RenderQueue_AllOpaque,
                    stateBlock: m_AlphaToMaskBlock,
                    excludeObjectMotionVectors: true);
                var renderList = renderGraph.CreateRendererList(opaqueRenderList);
                passData.rendererList = builder.UseRendererList(renderList);

                builder.SetRenderFunc(
                    (DrawRendererListPassData data, RenderGraphContext context) =>
                    {
                        DrawOpaqueRendererList(context.renderContext, context.cmd, data.frameSettings, data.rendererList);
                    });

                vBufferOutput.vbuffer0 = passData.vbuffer0;
                vBufferOutput.vbuffer1 = passData.vbuffer1;
                vBufferOutput.materialDepthBuffer = passData.materialDepthBuffer;
                vBufferOutput.depthBuffer = passData.depthBuffer;
            }
            return vBufferOutput;
        }
    }
}
