using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Internal;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /* Safe to call from within a native render pass. */
    public class RasterCommandBuffer : BaseCommandBuffer
    {
        public RasterCommandBuffer(CommandBuffer wrapped) : base(wrapped) { }

        // Rasterization commands
        public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, [DefaultValue("0")] int submeshIndex, [DefaultValue("-1")] int shaderPass, [DefaultValue("null")] MaterialPropertyBlock properties) => m_wrapped.DrawMesh(mesh, matrix, material, submeshIndex, shaderPass, properties);
        [ExcludeFromDocs]
        public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex, int shaderPass) => m_wrapped.DrawMesh(mesh, matrix, material, submeshIndex, shaderPass);
        [ExcludeFromDocs]
        public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int submeshIndex) => m_wrapped.DrawMesh(mesh, matrix, material, submeshIndex);
        [ExcludeFromDocs]
        public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material) => m_wrapped.DrawMesh(mesh, matrix, material);
        public void DrawRenderer(Renderer renderer, Material material, [DefaultValue("0")] int submeshIndex, [DefaultValue("-1")] int shaderPass) => m_wrapped.DrawRenderer(renderer, material, submeshIndex, shaderPass);
        [ExcludeFromDocs]
        public void DrawRenderer(Renderer renderer, Material material, int submeshIndex) => m_wrapped.DrawRenderer(renderer, material, submeshIndex);
        [ExcludeFromDocs]
        public void DrawRenderer(Renderer renderer, Material material) => m_wrapped.DrawRenderer(renderer, material);
        public void DrawRendererList(UnityEngine.Rendering.RendererUtils.RendererList rendererList) => m_wrapped.DrawRendererList(rendererList);
        public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount, [DefaultValue("1")] int instanceCount, [DefaultValue("null")] MaterialPropertyBlock properties) => m_wrapped.DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount, properties);
        [ExcludeFromDocs]
        public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount, int instanceCount) => m_wrapped.DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount);
        [ExcludeFromDocs]
        public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int vertexCount) => m_wrapped.DrawProcedural(matrix, material, shaderPass, topology, vertexCount);
        public void DrawProcedural(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int indexCount, int instanceCount, MaterialPropertyBlock properties) => m_wrapped.DrawProcedural(indexBuffer, matrix, material, shaderPass, topology, indexCount, instanceCount, properties);
        public void DrawProcedural(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int indexCount, int instanceCount) => m_wrapped.DrawProcedural(indexBuffer, matrix, material, shaderPass, topology, indexCount, instanceCount);
        public void DrawProcedural(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, int indexCount) => m_wrapped.DrawProcedural(indexBuffer, matrix, material, shaderPass, topology, indexCount);
        public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties) => m_wrapped.DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties);
        public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset) => m_wrapped.DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs, argsOffset);
        public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs) => m_wrapped.DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs);
        public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties) => m_wrapped.DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties);
        public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset) => m_wrapped.DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs, argsOffset);
        public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, ComputeBuffer bufferWithArgs) => m_wrapped.DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs);
        public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties) => m_wrapped.DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties);
        public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset) => m_wrapped.DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs, argsOffset);
        public void DrawProceduralIndirect(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs) => m_wrapped.DrawProceduralIndirect(matrix, material, shaderPass, topology, bufferWithArgs);
        public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties) => m_wrapped.DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs, argsOffset, properties);
        public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset) => m_wrapped.DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs, argsOffset);
        public void DrawProceduralIndirect(GraphicsBuffer indexBuffer, Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology, GraphicsBuffer bufferWithArgs) => m_wrapped.DrawProceduralIndirect(indexBuffer, matrix, material, shaderPass, topology, bufferWithArgs);
        public void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties) => m_wrapped.DrawMeshInstanced(mesh, submeshIndex, material, shaderPass, matrices, count, properties);
        public void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices, int count) => m_wrapped.DrawMeshInstanced(mesh, submeshIndex, material, shaderPass, matrices, count);
        public void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices) => m_wrapped.DrawMeshInstanced(mesh, submeshIndex, material, shaderPass, matrices);
        public void DrawMeshInstancedProcedural(Mesh mesh, int submeshIndex, Material material, int shaderPass, int count, MaterialPropertyBlock properties = null) => m_wrapped.DrawMeshInstancedProcedural(mesh, submeshIndex, material, shaderPass, count, properties = null);
        public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties) => m_wrapped.DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs, argsOffset, properties);
        public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, ComputeBuffer bufferWithArgs, int argsOffset) => m_wrapped.DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs, argsOffset);
        public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, ComputeBuffer bufferWithArgs) => m_wrapped.DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs);
        public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, GraphicsBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties) => m_wrapped.DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs, argsOffset, properties);
        public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, GraphicsBuffer bufferWithArgs, int argsOffset) => m_wrapped.DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs, argsOffset);
        public void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, int shaderPass, GraphicsBuffer bufferWithArgs) => m_wrapped.DrawMeshInstancedIndirect(mesh, submeshIndex, material, shaderPass, bufferWithArgs);
        public void DrawOcclusionMesh(RectInt normalizedCamViewport) => m_wrapped.DrawOcclusionMesh(normalizedCamViewport);

        public void SetInstanceMultiplier(uint multiplier) => m_wrapped.SetInstanceMultiplier(multiplier);
    }
}
