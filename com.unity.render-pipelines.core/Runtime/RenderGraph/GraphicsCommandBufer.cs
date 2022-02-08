using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using Unity.Collections;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /* Functions safe to call from a graphics context. This is the closes thing to a full command buffer in the rendergraph system.
     * It still does not have functions to do things handled by the rendergraph like binding rendertargets or UAVs.
     */
    public class GraphicsCommandBuffer : ComputeCommandBuffer
    {
        public GraphicsCommandBuffer(CommandBuffer wrapped) : base(wrapped, false) {}

        // Most of the commands below essentially boil down to buffers or some sort of compute shader or rendering and are only valid outside of a renderpass.
        public void GenerateMips(RenderTargetIdentifier rt) => m_wrapped.GenerateMips(rt);
        public void GenerateMips(RenderTexture rt) => m_wrapped.GenerateMips(rt);
        public void ResolveAntiAliasedSurface(RenderTexture rt, RenderTexture target = null) => m_wrapped.ResolveAntiAliasedSurface(rt, target = null);

        public void CopyTexture(RenderTargetIdentifier src, RenderTargetIdentifier dst) => m_wrapped.CopyTexture(src, dst);
        public void CopyTexture(RenderTargetIdentifier src, int srcElement, RenderTargetIdentifier dst, int dstElement) => m_wrapped.CopyTexture(src, srcElement, dst, dstElement);
        public void CopyTexture(RenderTargetIdentifier src, int srcElement, int srcMip, RenderTargetIdentifier dst, int dstElement, int dstMip) => m_wrapped.CopyTexture(src, srcElement, srcMip, dst, dstElement, dstMip);
        public void CopyTexture(RenderTargetIdentifier src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, RenderTargetIdentifier dst, int dstElement, int dstMip, int dstX, int dstY) => m_wrapped.CopyTexture(src, srcElement, srcMip, srcX, srcY, srcWidth, srcHeight, dst, dstElement, dstMip, dstX, dstY);

        public void CopyBuffer(GraphicsBuffer source, GraphicsBuffer dest) => m_wrapped.CopyBuffer(source, dest);

        public void ProcessVTFeedback(RenderTargetIdentifier rt, IntPtr resolver, int slice, int x, int width, int y, int height, int mip) => m_wrapped.ProcessVTFeedback(rt, resolver, slice, x, width, y, height, mip);

        public void ConvertTexture(RenderTargetIdentifier src, RenderTargetIdentifier dst) => m_wrapped.ConvertTexture(src, dst);
        public void ConvertTexture(RenderTargetIdentifier src, int srcElement, RenderTargetIdentifier dst, int dstElement) => m_wrapped.ConvertTexture(src, srcElement, dst, dstElement);

        // Async Readback
        public void WaitAllAsyncReadbackRequests() => m_wrapped.WaitAllAsyncReadbackRequests();
        public void RequestAsyncReadback(ComputeBuffer src, Action<AsyncGPUReadbackRequest> callback) => m_wrapped.RequestAsyncReadback(src, callback);
        public void RequestAsyncReadback(GraphicsBuffer src, Action<AsyncGPUReadbackRequest> callback) => m_wrapped.RequestAsyncReadback(src, callback);
        public void RequestAsyncReadback(ComputeBuffer src, int size, int offset, Action<AsyncGPUReadbackRequest> callback) => m_wrapped.RequestAsyncReadback(src, size, offset, callback);
        public void RequestAsyncReadback(GraphicsBuffer src, int size, int offset, Action<AsyncGPUReadbackRequest> callback) => m_wrapped.RequestAsyncReadback(src, size, offset, callback);
        public void RequestAsyncReadback(Texture src, Action<AsyncGPUReadbackRequest> callback) => m_wrapped.RequestAsyncReadback(src, callback);
        public void RequestAsyncReadback(Texture src, int mipIndex, Action<AsyncGPUReadbackRequest> callback) => m_wrapped.RequestAsyncReadback(src, mipIndex, callback);
        public void RequestAsyncReadback(Texture src, int mipIndex, TextureFormat dstFormat, Action<AsyncGPUReadbackRequest> callback) => m_wrapped.RequestAsyncReadback(src, mipIndex, dstFormat, callback);
        public void RequestAsyncReadback(Texture src, int mipIndex, GraphicsFormat dstFormat, Action<AsyncGPUReadbackRequest> callback) => m_wrapped.RequestAsyncReadback(src, mipIndex, dstFormat, callback);
        public void RequestAsyncReadback(Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, Action<AsyncGPUReadbackRequest> callback) => m_wrapped.RequestAsyncReadback(src, mipIndex, x, width, y, height, z, depth, callback);
        public void RequestAsyncReadback(Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, TextureFormat dstFormat, Action<AsyncGPUReadbackRequest> callback) => m_wrapped.RequestAsyncReadback(src, mipIndex, x, width, y, height, z, depth, dstFormat, callback);
        public void RequestAsyncReadback(Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, GraphicsFormat dstFormat, Action<AsyncGPUReadbackRequest> callback) => m_wrapped.RequestAsyncReadback(src, mipIndex, x, width, y, height, z, depth, dstFormat, callback);
        public void RequestAsyncReadbackIntoNativeArray<T>(ref NativeArray<T> output, ComputeBuffer src, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeArray<T>(ref output, src, callback);
        public void RequestAsyncReadbackIntoNativeArray<T>(ref NativeArray<T> output, ComputeBuffer src, int size, int offset, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeArray<T>(ref output, src, size, offset, callback);
        public void RequestAsyncReadbackIntoNativeArray<T>(ref NativeArray<T> output, GraphicsBuffer src, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeArray<T>(ref output, src, callback);
        public void RequestAsyncReadbackIntoNativeArray<T>(ref NativeArray<T> output, GraphicsBuffer src, int size, int offset, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeArray<T>(ref output, src, size, offset, callback);
        public void RequestAsyncReadbackIntoNativeArray<T>(ref NativeArray<T> output, Texture src, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeArray<T>(ref output, src, callback);
        public void RequestAsyncReadbackIntoNativeArray<T>(ref NativeArray<T> output, Texture src, int mipIndex, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeArray<T>(ref output, src, mipIndex, callback);
        public void RequestAsyncReadbackIntoNativeArray<T>(ref NativeArray<T> output, Texture src, int mipIndex, TextureFormat dstFormat, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeArray<T>(ref output, src, mipIndex, dstFormat, callback);
        public void RequestAsyncReadbackIntoNativeArray<T>(ref NativeArray<T> output, Texture src, int mipIndex, GraphicsFormat dstFormat, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeArray<T>(ref output, src, mipIndex, dstFormat, callback);
        public void RequestAsyncReadbackIntoNativeArray<T>(ref NativeArray<T> output, Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeArray<T>(ref output, src, mipIndex, x, width, y, height, z, depth, callback);
        public void RequestAsyncReadbackIntoNativeArray<T>(ref NativeArray<T> output, Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, TextureFormat dstFormat, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeArray<T>(ref output, src, mipIndex, x, width, y, height, z, depth, dstFormat, callback);
        public void RequestAsyncReadbackIntoNativeArray<T>(ref NativeArray<T> output, Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, GraphicsFormat dstFormat, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeArray<T>(ref output, src, mipIndex, x, width, y, height, z, depth, dstFormat, callback);
        public void RequestAsyncReadbackIntoNativeSlice<T>(ref NativeSlice<T> output, ComputeBuffer src, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeSlice<T>(ref output, src, callback);
        public void RequestAsyncReadbackIntoNativeSlice<T>(ref NativeSlice<T> output, ComputeBuffer src, int size, int offset, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeSlice<T>(ref output, src, size, offset, callback);
        public void RequestAsyncReadbackIntoNativeSlice<T>(ref NativeSlice<T> output, GraphicsBuffer src, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeSlice<T>(ref output, src, callback);
        public void RequestAsyncReadbackIntoNativeSlice<T>(ref NativeSlice<T> output, GraphicsBuffer src, int size, int offset, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeSlice<T>(ref output, src, size, offset, callback);
        public void RequestAsyncReadbackIntoNativeSlice<T>(ref NativeSlice<T> output, Texture src, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeSlice<T>(ref output, src, callback);
        public void RequestAsyncReadbackIntoNativeSlice<T>(ref NativeSlice<T> output, Texture src, int mipIndex, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeSlice<T>(ref output, src, mipIndex, callback);
        public void RequestAsyncReadbackIntoNativeSlice<T>(ref NativeSlice<T> output, Texture src, int mipIndex, TextureFormat dstFormat, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeSlice<T>(ref output, src, mipIndex, dstFormat, callback);
        public void RequestAsyncReadbackIntoNativeSlice<T>(ref NativeSlice<T> output, Texture src, int mipIndex, GraphicsFormat dstFormat, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeSlice<T>(ref output, src, mipIndex, dstFormat, callback);
        public void RequestAsyncReadbackIntoNativeSlice<T>(ref NativeSlice<T> output, Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeSlice<T>(ref output, src, mipIndex, x, width, y, height, z, depth, callback);
        public void RequestAsyncReadbackIntoNativeSlice<T>(ref NativeSlice<T> output, Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, TextureFormat dstFormat, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeSlice<T>(ref output, src, mipIndex, x, width, y, height, z, depth, dstFormat, callback);
        public void RequestAsyncReadbackIntoNativeSlice<T>(ref NativeSlice<T> output, Texture src, int mipIndex, int x, int width, int y, int height, int z, int depth, GraphicsFormat dstFormat, Action<AsyncGPUReadbackRequest> callback) where T : struct => m_wrapped.RequestAsyncReadbackIntoNativeSlice<T>(ref output, src, mipIndex, x, width, y, height, z, depth, dstFormat, callback);
    }
}
