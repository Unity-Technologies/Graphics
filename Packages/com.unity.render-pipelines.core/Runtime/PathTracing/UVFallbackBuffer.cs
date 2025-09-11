using System;
using System.IO;
using Unity.Mathematics;
using UnityEngine.PathTracing.Core;
using UnityEngine.PathTracing.Lightmapping;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Sampling;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.PathTracing.Integration
{
    internal static class UVFallbackBufferBuilderShaderIDs
    {
        public static readonly int VertexBuffer = Shader.PropertyToID("g_VertexBuffer");
        public static readonly int Width = Shader.PropertyToID("g_Width");
        public static readonly int Height = Shader.PropertyToID("g_Height");
        public static readonly int WidthScale = Shader.PropertyToID("g_WidthScale");
        public static readonly int HeightScale = Shader.PropertyToID("g_HeightScale");

        public static readonly int UvFallback = Shader.PropertyToID("g_UvFallback");
        public static readonly int InstanceWidth = Shader.PropertyToID("g_InstanceWidth");
        public static readonly int InstanceHeight = Shader.PropertyToID("g_InstanceHeight");
        public static readonly int InstanceOffsetX = Shader.PropertyToID("g_InstanceOffsetX");
        public static readonly int InstanceOffsetY = Shader.PropertyToID("g_InstanceOffsetY");
        public static readonly int ChunkOffsetX = Shader.PropertyToID("g_ChunkOffsetX");
        public static readonly int ChunkOffsetY = Shader.PropertyToID("g_ChunkOffsetY");
        public static readonly int ChunkSize = Shader.PropertyToID("g_ChunkSize");
        public static readonly int InstanceWidthScale = Shader.PropertyToID("g_InstanceWidthScale");
        public static readonly int InstanceHeightScale = Shader.PropertyToID("g_InstanceHeightScale");
    }

    internal class UVFallbackBufferBuilder : IDisposable
    {
        private GraphicsBuffer _vertexBuffer;
        private Material _uvFallbackBufferMaterial;

        public void Dispose()
        {
            _vertexBuffer?.Dispose();
            _vertexBuffer = null;
        }

        public void Prepare(Material uvFallbackBufferMaterial)
        {
            _uvFallbackBufferMaterial = uvFallbackBufferMaterial;
        }

        public void Build(
            CommandBuffer cmd,
            RenderTexture uvFallbackRT,
            int width,
            int height,
            float widthScale,
            float heightScale,
            Mesh uvMesh)
        {
            cmd.BeginSample("Build UVFallbackBuffer");

            Debug.Assert((UInt64)width * (UInt64)height < uint.MaxValue);
            Debug.Assert(uvFallbackRT.format == RenderTextureFormat.RGFloat);
            Debug.Assert(uvFallbackRT.depth > 0);

            // Clear the fallback buffer to -1, indicating no valid texels
            cmd.SetRenderTarget(uvFallbackRT);
            cmd.ClearRenderTarget(false, true, new Color(-1.0f, -1.0f, -1.0f, -1.0f));

            // Expand vertices so we can do conservative rasterization
            var originalVertices = uvMesh.vertices;
            var originalIndices = uvMesh.triangles;
            var vertices = new Vector2[originalIndices.Length];
            for (int i = 0; i < originalIndices.Length; i++)
            {
                vertices[i] = originalVertices[originalIndices[i]];
            }

            // Reallocate the vertex buffer if necessary, write the vertices
            if (_vertexBuffer == null || _vertexBuffer.count < vertices.Length)
            {
                _vertexBuffer?.Dispose();
                _vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, vertices.Length, 2 * sizeof(float));
            }
            cmd.SetBufferData(_vertexBuffer, vertices);

            // Build the fallback buffer using conservative rasterization
            cmd.SetGlobalBuffer(UVFallbackBufferBuilderShaderIDs.VertexBuffer, _vertexBuffer);
            cmd.SetGlobalInteger(UVFallbackBufferBuilderShaderIDs.Width, width);
            cmd.SetGlobalInteger(UVFallbackBufferBuilderShaderIDs.Height, height);
            cmd.SetGlobalFloat(UVFallbackBufferBuilderShaderIDs.WidthScale, widthScale);
            cmd.SetGlobalFloat(UVFallbackBufferBuilderShaderIDs.HeightScale, heightScale);
            cmd.DrawProcedural(Matrix4x4.identity, _uvFallbackBufferMaterial, 0, MeshTopology.Triangles, (int)uvMesh.GetTotalIndexCount());

            cmd.EndSample("Build UVFallbackBuffer");

            GraphicsHelpers.Flush(cmd);
        }
    }

    internal class UVFallbackBuffer : IDisposable
    {
        public RenderTexture UVFallbackRT;
        public float WidthScale;
        public float HeightScale;
        public int Width => UVFallbackRT.width;
        public int Height => UVFallbackRT.height;

        public void Dispose()
        {
            UVFallbackRT?.Release();
            if (UVFallbackRT != null)
                CoreUtils.Destroy(UVFallbackRT);
            UVFallbackRT = null;
        }

        public bool Build(
            CommandBuffer commandBuffer,
            UVFallbackBufferBuilder builder,
            int width,
            int height,
            UVMesh uvMesh)
        {
            if (width == 0 || height == 0)
                return false;
            // Assume that the largest edge of UV bounds can fit into the texture,
            // and calculate the scale factor for each dimension to achieve this.
            // This is accounting for the aspect ratio of the UV bounds.
            var uvAspectRatio = uvMesh.UVAspectRatio;
            if (uvAspectRatio >= 1.0f) // width >= height
            {
                WidthScale = 1.0f;
                HeightScale = (width / uvAspectRatio) / height;
            }
            else // width < height
            {
                WidthScale = (height * uvAspectRatio) / width;
                HeightScale = 1.0f;
            }

            // If we find that the scaled UV bounds still don't fit into the texture,
            // then we uniformly scale down the bounds to fit.
            // This is accounting for the aspect ratio of the texture.
            if (HeightScale > 1)
            {
                WidthScale /= HeightScale;
                HeightScale = 1.0f;
            }
            if (WidthScale > 1)
            {
                HeightScale /= WidthScale;
                WidthScale = 1.0f;
            }

            UVFallbackRT = new RenderTexture(width, height, 24, RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear)
            {
                name = "UVFallbackRT",
                hideFlags = HideFlags.HideAndDontSave,
                enableRandomWrite = true,
                useMipMap = false,
                autoGenerateMips = false,
            };
            UVFallbackRT.Create();

            builder.Build(commandBuffer, UVFallbackRT, width, height, WidthScale, HeightScale, uvMesh.Mesh);
            return true;
        }

        public void Bind(CommandBuffer cmd, IRayTracingShader shader, Vector2Int instanceOffset)
        {
            shader.SetTextureParam(cmd, UVFallbackBufferBuilderShaderIDs.UvFallback, UVFallbackRT);
            shader.SetIntParam(cmd, UVFallbackBufferBuilderShaderIDs.InstanceWidth, UVFallbackRT.width);
            shader.SetIntParam(cmd, UVFallbackBufferBuilderShaderIDs.InstanceHeight, UVFallbackRT.height);
            shader.SetIntParam(cmd, UVFallbackBufferBuilderShaderIDs.InstanceOffsetX, instanceOffset.x);
            shader.SetIntParam(cmd, UVFallbackBufferBuilderShaderIDs.InstanceOffsetY, instanceOffset.y);
            shader.SetFloatParam(cmd, UVFallbackBufferBuilderShaderIDs.InstanceWidthScale, WidthScale);
            shader.SetFloatParam(cmd, UVFallbackBufferBuilderShaderIDs.InstanceHeightScale, HeightScale);
        }

        public void BindChunked(CommandBuffer cmd, IRayTracingShader shader, Vector2Int instanceOffset, uint2 chunkOffset, uint chunkSize)
        {
            Bind(cmd, shader, instanceOffset);
            shader.SetIntParam(cmd, UVFallbackBufferBuilderShaderIDs.ChunkOffsetX, (int)chunkOffset.x);
            shader.SetIntParam(cmd, UVFallbackBufferBuilderShaderIDs.ChunkOffsetY, (int)chunkOffset.y);
            shader.SetIntParam(cmd, UVFallbackBufferBuilderShaderIDs.ChunkSize, (int)chunkSize);
        }

        public void Bind(CommandBuffer cmd, ComputeShader shader, int kernelIndex, Vector2Int instanceOffset)
        {
            cmd.SetComputeTextureParam(shader, kernelIndex, UVFallbackBufferBuilderShaderIDs.UvFallback, UVFallbackRT);
            cmd.SetComputeIntParam(shader, UVFallbackBufferBuilderShaderIDs.InstanceWidth, UVFallbackRT.width);
            cmd.SetComputeIntParam(shader, UVFallbackBufferBuilderShaderIDs.InstanceHeight, UVFallbackRT.height);
            cmd.SetComputeIntParam(shader, UVFallbackBufferBuilderShaderIDs.InstanceOffsetX, instanceOffset.x);
            cmd.SetComputeIntParam(shader, UVFallbackBufferBuilderShaderIDs.InstanceOffsetY, instanceOffset.y);
            cmd.SetComputeFloatParam(shader, UVFallbackBufferBuilderShaderIDs.InstanceWidthScale, WidthScale);
            cmd.SetComputeFloatParam(shader, UVFallbackBufferBuilderShaderIDs.InstanceHeightScale, HeightScale);
        }
    }
}
