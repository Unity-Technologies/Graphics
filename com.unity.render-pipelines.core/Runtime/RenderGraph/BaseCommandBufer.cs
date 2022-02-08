using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Internal;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /* This contains functions which are ok to call from all rendergraph commandbuffer types */
    public class BaseCommandBuffer
    {
        protected CommandBuffer m_wrapped;

        public BaseCommandBuffer(CommandBuffer wrapped)
        {
            m_wrapped.Clear();
            m_wrapped = wrapped;
        }

        public string name => m_wrapped.name;
        public int sizeInBytes => m_wrapped.sizeInBytes;

        // Global shader variable setup
        public void SetGlobalFloat(string name, float value) => m_wrapped.SetGlobalFloat(name, value);
        public void SetGlobalInt(string name, int value) => m_wrapped.SetGlobalInt(name, value);
        public void SetGlobalInteger(string name, int value) => m_wrapped.SetGlobalInteger(name, value);
        public void SetGlobalVector(string name, Vector4 value) => m_wrapped.SetGlobalVector(name, value);
        public void SetGlobalColor(string name, Color value) => m_wrapped.SetGlobalColor(name, value);
        public void SetGlobalMatrix(string name, Matrix4x4 value) => m_wrapped.SetGlobalMatrix(name, value);
        public void SetGlobalFloatArray(string propertyName, List<float> values) => m_wrapped.SetGlobalFloatArray(propertyName, values);
        public void SetGlobalFloatArray(int nameID, List<float> values) => m_wrapped.SetGlobalFloatArray(nameID, values);
        public void SetGlobalFloatArray(string propertyName, float[] values) => m_wrapped.SetGlobalFloatArray(propertyName, values);
        public void SetGlobalVectorArray(string propertyName, List<Vector4> values) => m_wrapped.SetGlobalVectorArray(propertyName, values);
        public void SetGlobalVectorArray(int nameID, List<Vector4> values) => m_wrapped.SetGlobalVectorArray(nameID, values);
        public void SetGlobalVectorArray(string propertyName, Vector4[] values) => m_wrapped.SetGlobalVectorArray(propertyName, values);
        public void SetGlobalMatrixArray(string propertyName, List<Matrix4x4> values) => m_wrapped.SetGlobalMatrixArray(propertyName, values);
        public void SetGlobalMatrixArray(int nameID, List<Matrix4x4> values) => m_wrapped.SetGlobalMatrixArray(nameID, values);
        public void SetGlobalMatrixArray(string propertyName, Matrix4x4[] values) => m_wrapped.SetGlobalMatrixArray(propertyName, values);
        public void SetGlobalTexture(string name, RenderTargetIdentifier value) => m_wrapped.SetGlobalTexture(name, value);
        public void SetGlobalTexture(int nameID, RenderTargetIdentifier value) => m_wrapped.SetGlobalTexture(nameID, value);
        public void SetGlobalTexture(string name, RenderTargetIdentifier value, RenderTextureSubElement element) => m_wrapped.SetGlobalTexture(name, value, element);
        public void SetGlobalTexture(int nameID, RenderTargetIdentifier value, RenderTextureSubElement element) => m_wrapped.SetGlobalTexture(nameID, value, element);
        public void SetGlobalBuffer(string name, ComputeBuffer value) => m_wrapped.SetGlobalBuffer(name, value);
        public void SetGlobalBuffer(int nameID, ComputeBuffer value) => m_wrapped.SetGlobalBuffer(nameID, value);
        public void SetGlobalBuffer(string name, GraphicsBuffer value) => m_wrapped.SetGlobalBuffer(name, value);
        public void SetGlobalBuffer(int nameID, GraphicsBuffer value) => m_wrapped.SetGlobalBuffer(nameID, value);
        public void SetGlobalConstantBuffer(ComputeBuffer buffer, int nameID, int offset, int size) => m_wrapped.SetGlobalConstantBuffer(buffer, nameID, offset, size);
        public void SetGlobalConstantBuffer(ComputeBuffer buffer, string name, int offset, int size) => m_wrapped.SetGlobalConstantBuffer(buffer, name, offset, size);
        public void SetGlobalConstantBuffer(GraphicsBuffer buffer, int nameID, int offset, int size) => m_wrapped.SetGlobalConstantBuffer(buffer, nameID, offset, size);
        public void SetGlobalConstantBuffer(GraphicsBuffer buffer, string name, int offset, int size) => m_wrapped.SetGlobalConstantBuffer(buffer, name, offset, size);

        public void SetGlobalFloat(int nameID, float value) => m_wrapped.SetGlobalFloat(nameID, value);
        public void SetGlobalInt(int nameID, int value) => m_wrapped.SetGlobalInt(nameID, value);
        public void SetGlobalInteger(int nameID, int value) => m_wrapped.SetGlobalInteger(nameID, value);
        public void SetGlobalVector(int nameID, Vector4 value) => m_wrapped.SetGlobalVector(nameID, value);
        public void SetGlobalColor(int nameID, Color value) => m_wrapped.SetGlobalColor(nameID, value);
        public void SetGlobalMatrix(int nameID, Matrix4x4 value) => m_wrapped.SetGlobalMatrix(nameID, value);
        public void EnableShaderKeyword(string keyword) => m_wrapped.EnableShaderKeyword(keyword);

        public void SetViewMatrix(Matrix4x4 view) => m_wrapped.SetViewMatrix(view);
        public void SetProjectionMatrix(Matrix4x4 proj) => m_wrapped.SetProjectionMatrix(proj);
        public void SetViewProjectionMatrices(Matrix4x4 view, Matrix4x4 proj) => m_wrapped.SetViewProjectionMatrices(view, proj);
        public void SetGlobalFloatArray(int nameID, float[] values) => m_wrapped.SetGlobalFloatArray(nameID, values);
        public void SetGlobalVectorArray(int nameID, Vector4[] values) => m_wrapped.SetGlobalVectorArray( nameID, values);
        public void SetGlobalMatrixArray(int nameID, Matrix4x4[] values) => m_wrapped.SetGlobalMatrixArray(nameID, values);
        public void SetLateLatchProjectionMatrices(Matrix4x4[] projectionMat) => m_wrapped.SetLateLatchProjectionMatrices(projectionMat);
        public void MarkLateLatchMatrixShaderPropertyID(CameraLateLatchMatrixType matrixPropertyType, int shaderPropertyID) => m_wrapped.MarkLateLatchMatrixShaderPropertyID(matrixPropertyType, shaderPropertyID);
        public void UnmarkLateLatchMatrix(CameraLateLatchMatrixType matrixPropertyType) => m_wrapped.UnmarkLateLatchMatrix(matrixPropertyType);

        // Keywords
        public void EnableKeyword(in GlobalKeyword keyword) => m_wrapped.EnableKeyword(keyword);
        public void EnableKeyword(Material material, in LocalKeyword keyword) => m_wrapped.EnableKeyword(material, keyword);
        public void EnableKeyword(ComputeShader computeShader, in LocalKeyword keyword) => m_wrapped.EnableKeyword(computeShader, keyword);
        public void DisableShaderKeyword(string keyword) => m_wrapped.DisableShaderKeyword(keyword);
        public void DisableKeyword(in GlobalKeyword keyword) => m_wrapped.DisableKeyword(keyword);
        public void DisableKeyword(Material material, in LocalKeyword keyword) => m_wrapped.DisableKeyword(material, keyword);
        public void DisableKeyword(ComputeShader computeShader, in LocalKeyword keyword) => m_wrapped.DisableKeyword(computeShader, keyword);
        public void SetKeyword(in GlobalKeyword keyword, bool value) => m_wrapped.SetKeyword(keyword, value);
        public void SetKeyword(Material material, in LocalKeyword keyword, bool value) => m_wrapped.SetKeyword(material, keyword, value);
        public void SetKeyword(ComputeShader computeShader, in LocalKeyword keyword, bool value) => m_wrapped.SetKeyword(computeShader, keyword, value);

        /*Blit is bad it modifies rendertargets internally essentially deprecated in a SRP context and only there for legacy 
        public void Blit(Texture source, RenderTargetIdentifier dest) => m_wrapped.Blit(source, dest);
        public void Blit(Texture source, RenderTargetIdentifier dest, Vector2 scale, Vector2 offset) => m_wrapped.Blit(source, dest, scale, offset);
        public void Blit(Texture source, RenderTargetIdentifier dest, Material mat) => m_wrapped.Blit(source, dest, mat);
        public void Blit(Texture source, RenderTargetIdentifier dest, Material mat, int pass) => m_wrapped.Blit(source, dest, mat, pass);
        public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest) => m_wrapped.Blit(source, dest);
        public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, Vector2 scale, Vector2 offset) => m_wrapped.Blit(source, dest, scale, offset);
        public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, Material mat) => m_wrapped.Blit(source, dest, mat);
        public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, Material mat, int pass) => m_wrapped.Blit(source, dest, mat, pass);
        public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, int sourceDepthSlice, int destDepthSlice) => m_wrapped.Blit(source, dest, sourceDepthSlice, destDepthSlice);
        public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, Vector2 scale, Vector2 offset, int sourceDepthSlice, int destDepthSlice) => m_wrapped.Blit(source, dest, scale, offset, sourceDepthSlice, destDepthSlice);
        public void Blit(RenderTargetIdentifier source, RenderTargetIdentifier dest, Material mat, int pass, int destDepthSlice) => m_wrapped.Blit(source, dest, mat, pass, destDepthSlice);*/

        // UAV binding, similar to rendertarget binding, handled by graph
        /*public void SetRandomWriteTarget(int index, RenderTargetIdentifier rt) => m_wrapped.SetRandomWriteTarget(index, rt);
        public void SetRandomWriteTarget(int index, ComputeBuffer buffer, bool preserveCounterValue) => m_wrapped.SetRandomWriteTarget(index, buffer, preserveCounterValue);
        public void SetRandomWriteTarget(int index, ComputeBuffer buffer) => m_wrapped.SetRandomWriteTarget(index, buffer);
        public void SetRandomWriteTarget(int index, GraphicsBuffer buffer, bool preserveCounterValue) => m_wrapped.SetRandomWriteTarget(index, buffer, preserveCounterValue);
        public void SetRandomWriteTarget(int index, GraphicsBuffer buffer) => m_wrapped.SetRandomWriteTarget(index, buffer);
        public void ClearRandomWriteTargets() => m_wrapped.ClearRandomWriteTargets();*/

        // Misc bits
        public void SetShadowSamplingMode(UnityEngine.Rendering.RenderTargetIdentifier shadowmap, ShadowSamplingMode mode) => m_wrapped.SetShadowSamplingMode(shadowmap, mode);
        public void SetSinglePassStereo(SinglePassStereoMode mode) => m_wrapped.SetSinglePassStereo(mode);
        public void IssuePluginEvent(IntPtr callback, int eventID) => m_wrapped.IssuePluginEvent(callback, eventID);
        public void IssuePluginEventAndData(IntPtr callback, int eventID, IntPtr data) => m_wrapped.IssuePluginEventAndData(callback, eventID, data);
        public void IssuePluginCustomBlit(IntPtr callback, uint command, UnityEngine.Rendering.RenderTargetIdentifier source, UnityEngine.Rendering.RenderTargetIdentifier dest, uint commandParam, uint commandFlags) => m_wrapped.IssuePluginCustomBlit(callback, command, source, dest, commandParam, commandFlags);
        public void IssuePluginCustomTextureUpdateV2(IntPtr callback, Texture targetTexture, uint userData) => m_wrapped.IssuePluginCustomTextureUpdateV2(callback, targetTexture, userData);

        /* unity sets the following vulkan dynamic state so these can be modified in renderpasses
        VK_DYNAMIC_STATE_VIEWPORT,
        VK_DYNAMIC_STATE_SCISSOR,
        VK_DYNAMIC_STATE_STENCIL_REFERENCE,
        VK_DYNAMIC_STATE_DEPTH_BIAS (optional)
        */
        public void SetInvertCulling(bool invertCulling) => m_wrapped.SetInvertCulling(invertCulling);
        public void EnableScissorRect(Rect scissor) => m_wrapped.EnableScissorRect(scissor);
        public void DisableScissorRect() => m_wrapped.DisableScissorRect();
        public void SetViewport(Rect pixelRect) => m_wrapped.SetViewport(pixelRect);
        public void SetGlobalDepthBias(float bias, float slopeBias) => m_wrapped.SetGlobalDepthBias(bias, slopeBias);

        /*
        Temporary rendertargets are handled by the graph and not exposed in the graph specific command buffers
        extern public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, GraphicsFormat format, int antiAliasing, bool enableRandomWrite, RenderTextureMemoryless memorylessMode, bool useDynamicScale);
        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, GraphicsFormat format, int antiAliasing, bool enableRandomWrite, RenderTextureMemoryless memorylessMode)
        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, GraphicsFormat format, int antiAliasing, bool enableRandomWrite)
        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, GraphicsFormat format, int antiAliasing)
        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, GraphicsFormat format)
        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, bool enableRandomWrite, RenderTextureMemoryless memorylessMode, bool useDynamicScale)
        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, bool enableRandomWrite, RenderTextureMemoryless memorylessMode)
        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, bool enableRandomWrite)
        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing)
        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite)
        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter, RenderTextureFormat format)
        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer, FilterMode filter)
        public void GetTemporaryRT(int nameID, int width, int height, int depthBuffer)
        public void GetTemporaryRT(int nameID, int width, int height)
        public void GetTemporaryRT(int nameID, RenderTextureDescriptor desc, FilterMode filter)
        public void GetTemporaryRT(int nameID, RenderTextureDescriptor desc)
        extern public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter, GraphicsFormat format, int antiAliasing, bool enableRandomWrite, bool useDynamicScale);
        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter, GraphicsFormat format, int antiAliasing, bool enableRandomWrite)
        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter, GraphicsFormat format, int antiAliasing)
        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter, GraphicsFormat format)
        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing, bool enableRandomWrite)
        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite, int antiAliasing)
        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter, RenderTextureFormat format, RenderTextureReadWrite readWrite)
        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter, RenderTextureFormat format)
        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer, FilterMode filter)
        public void GetTemporaryRTArray(int nameID, int width, int height, int slices, int depthBuffer)
        public void GetTemporaryRTArray(int nameID, int width, int height, int slices)
        extern public void ReleaseTemporaryRT(int nameID);
        public void SetRenderTarget(RenderTargetIdentifier rt)
        public void SetRenderTarget(RenderTargetIdentifier rt, RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction)
        public void SetRenderTarget(RenderTargetIdentifier rt,
            RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction)
        public void SetRenderTarget(RenderTargetIdentifier rt, int mipLevel)
        public void SetRenderTarget(RenderTargetIdentifier rt, int mipLevel, CubemapFace cubemapFace)
        public void SetRenderTarget(RenderTargetIdentifier rt, int mipLevel, CubemapFace cubemapFace, int depthSlice)
        public void SetRenderTarget(RenderTargetIdentifier color, RenderTargetIdentifier depth)
        public void SetRenderTarget(RenderTargetIdentifier color, RenderTargetIdentifier depth, int mipLevel)
        public void SetRenderTarget(RenderTargetIdentifier color, RenderTargetIdentifier depth, int mipLevel, CubemapFace cubemapFace)
        public void SetRenderTarget(RenderTargetIdentifier color, RenderTargetIdentifier depth, int mipLevel, CubemapFace cubemapFace, int depthSlice)
        public void SetRenderTarget(RenderTargetIdentifier color, RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depth, RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction)
        public void SetRenderTarget(RenderTargetIdentifier[] colors, Rendering.RenderTargetIdentifier depth)
        public void SetRenderTarget(RenderTargetIdentifier[] colors, Rendering.RenderTargetIdentifier depth, int mipLevel, CubemapFace cubemapFace, int depthSlice)
        public void SetRenderTarget(RenderTargetBinding binding, int mipLevel, CubemapFace cubemapFace, int depthSlice)
        public void SetRenderTarget(RenderTargetBinding binding)
        */

        /*
        Rendertarget clears are handled by the graph and not exposed in the graph specific command buffers
        extern public void ClearRenderTarget(RTClearFlags clearFlags, Color backgroundColor, float depth, uint stencil);
        public void ClearRenderTarget(bool clearDepth, bool clearColor, Color backgroundColor)
        public void ClearRenderTarget(bool clearDepth, bool clearColor, Color backgroundColor, float depth)
        */

        // Performance Samplers
        public void BeginSample(string name) => m_wrapped.BeginSample(name);
        public void EndSample(string name) => m_wrapped.EndSample(name);
        public void BeginSample(Profiling.CustomSampler sampler) => m_wrapped.BeginSample(sampler);
        public void EndSample(Profiling.CustomSampler sampler) => m_wrapped.EndSample(sampler);

        // This doesn't even do anything on the GPU :-D
        public void IncrementUpdateCount(UnityEngine.Rendering.RenderTargetIdentifier dest) => m_wrapped.IncrementUpdateCount(dest);



    }
}
