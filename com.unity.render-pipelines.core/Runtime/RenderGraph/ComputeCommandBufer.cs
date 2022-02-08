using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /* Safe to call from an async compute context */
    public class ComputeCommandBuffer : BaseCommandBuffer
    {
        public ComputeCommandBuffer(CommandBuffer wrapped, bool isAsync) : base(wrapped) {
            if (isAsync) m_wrapped.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);
        }

        // Compute parameter setup
        public void SetComputeFloatParam(ComputeShader computeShader, string name, float val) => m_wrapped.SetComputeFloatParam(computeShader, name, val);
        public void SetComputeIntParam(ComputeShader computeShader, string name, int val) => m_wrapped.SetComputeFloatParam(computeShader, name, val);
        public void SetComputeVectorArrayParam(ComputeShader computeShader, string name, Vector4[] values) => m_wrapped.SetComputeVectorArrayParam(computeShader, name, values);
        public void SetComputeMatrixParam(ComputeShader computeShader, string name, Matrix4x4 val) => m_wrapped.SetComputeMatrixParam(computeShader, name, val);
        public void SetComputeMatrixArrayParam(ComputeShader computeShader, string name, Matrix4x4[] values) => m_wrapped.SetComputeMatrixArrayParam(computeShader, name, values);
        public void SetComputeFloatParams(ComputeShader computeShader, string name, params float[] values) => m_wrapped.SetComputeFloatParams(computeShader, name, values);
        public void SetComputeFloatParams(ComputeShader computeShader, int nameID, params float[] values) => m_wrapped.SetComputeFloatParams(computeShader, nameID, values);
        public void SetComputeIntParams(ComputeShader computeShader, string name, params int[] values) => m_wrapped.SetComputeIntParams(computeShader, name, values);
        public void SetComputeIntParams(ComputeShader computeShader, int nameID, params int[] values) => m_wrapped.SetComputeIntParams(computeShader, nameID, values);
        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, string name, RenderTargetIdentifier rt) => m_wrapped.SetComputeTextureParam(computeShader, kernelIndex, name, rt);
        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, int nameID, RenderTargetIdentifier rt) => m_wrapped.SetComputeTextureParam(computeShader, kernelIndex, nameID, rt);
        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, string name, RenderTargetIdentifier rt, int mipLevel) => m_wrapped.SetComputeTextureParam(computeShader, kernelIndex, name, rt, mipLevel);
        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, int nameID, RenderTargetIdentifier rt, int mipLevel) => m_wrapped.SetComputeTextureParam(computeShader, kernelIndex, nameID, rt, mipLevel);
        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, string name, RenderTargetIdentifier rt, int mipLevel, RenderTextureSubElement element) => m_wrapped.SetComputeTextureParam(computeShader, kernelIndex, name, rt, mipLevel, element);
        public void SetComputeTextureParam(ComputeShader computeShader, int kernelIndex, int nameID, RenderTargetIdentifier rt, int mipLevel, RenderTextureSubElement element) => m_wrapped.SetComputeTextureParam(computeShader, kernelIndex, nameID, rt, mipLevel, element);
        public void SetComputeBufferParam(ComputeShader computeShader, int kernelIndex, int nameID, ComputeBuffer buffer) => m_wrapped.SetComputeBufferParam(computeShader, kernelIndex, nameID, buffer);
        public void SetComputeBufferParam(ComputeShader computeShader, int kernelIndex, string name, ComputeBuffer buffer) => m_wrapped.SetComputeBufferParam(computeShader, kernelIndex, name, buffer);
        public void SetComputeBufferParam(ComputeShader computeShader, int kernelIndex, int nameID, GraphicsBuffer buffer) => m_wrapped.SetComputeBufferParam(computeShader, kernelIndex, nameID, buffer);
        public void SetComputeBufferParam(ComputeShader computeShader, int kernelIndex, string name, GraphicsBuffer buffer) => m_wrapped.SetComputeBufferParam(computeShader, kernelIndex, name, buffer);
        public void SetComputeConstantBufferParam(ComputeShader computeShader, int nameID, ComputeBuffer buffer, int offset, int size) => m_wrapped.SetComputeConstantBufferParam(computeShader, nameID, buffer, offset, size);
        public void SetComputeConstantBufferParam(ComputeShader computeShader, string name, ComputeBuffer buffer, int offset, int size) => m_wrapped.SetComputeConstantBufferParam(computeShader, name, buffer, offset, size);
        public void SetComputeConstantBufferParam(ComputeShader computeShader, int nameID, GraphicsBuffer buffer, int offset, int size) => m_wrapped.SetComputeConstantBufferParam(computeShader, nameID, buffer, offset, size);
        public void SetComputeConstantBufferParam(ComputeShader computeShader, string name, GraphicsBuffer buffer, int offset, int size) => m_wrapped.SetComputeConstantBufferParam(computeShader, name, buffer, offset, size);
        public void SetComputeFloatParam([NotNull] ComputeShader computeShader, int nameID, float val) => m_wrapped.SetComputeFloatParam(computeShader, nameID, val);
        public void SetComputeIntParam([NotNull] ComputeShader computeShader, int nameID, int val) => m_wrapped.SetComputeIntParam(computeShader, nameID, val);
        public void SetComputeVectorParam([NotNull] ComputeShader computeShader, int nameID, Vector4 val) => m_wrapped.SetComputeVectorParam(computeShader, nameID, val);
        public void SetComputeVectorArrayParam([NotNull] ComputeShader computeShader, int nameID, Vector4[] values) => m_wrapped.SetComputeVectorArrayParam(computeShader, nameID, values);
        public void SetComputeMatrixParam([NotNull] ComputeShader computeShader, int nameID, Matrix4x4 val) => m_wrapped.SetComputeMatrixParam(computeShader, nameID, val);
        public void SetComputeMatrixArrayParam([NotNull] ComputeShader computeShader, int nameID, Matrix4x4[] values) => m_wrapped.SetComputeMatrixArrayParam(computeShader, nameID, values);

        // Execute a compute shader.
        public void DispatchCompute(ComputeShader computeShader, int kernelIndex, int threadGroupsX, int threadGroupsY, int threadGroupsZ) => m_wrapped.DispatchCompute(computeShader, kernelIndex, threadGroupsX, threadGroupsY, threadGroupsZ);
        public void DispatchCompute(ComputeShader computeShader, int kernelIndex, ComputeBuffer indirectBuffer, uint argsOffset) => m_wrapped.DispatchCompute(computeShader, kernelIndex, indirectBuffer, argsOffset);
        public void DispatchCompute(ComputeShader computeShader, int kernelIndex, GraphicsBuffer indirectBuffer, uint argsOffset) => m_wrapped.DispatchCompute(computeShader, kernelIndex, indirectBuffer, argsOffset);

        // Raytracing functions
        public void BuildRayTracingAccelerationStructure(RayTracingAccelerationStructure accelerationStructure) => m_wrapped.BuildRayTracingAccelerationStructure(accelerationStructure);
        public void BuildRayTracingAccelerationStructure(RayTracingAccelerationStructure accelerationStructure, Vector3 relativeOrigin) => m_wrapped.BuildRayTracingAccelerationStructure(accelerationStructure, relativeOrigin);
        public void SetRayTracingAccelerationStructure(RayTracingShader rayTracingShader, string name, RayTracingAccelerationStructure rayTracingAccelerationStructure) => m_wrapped.SetRayTracingAccelerationStructure(rayTracingShader, name, rayTracingAccelerationStructure);
        public void SetRayTracingAccelerationStructure(RayTracingShader rayTracingShader, int nameID, RayTracingAccelerationStructure rayTracingAccelerationStructure) => m_wrapped.SetRayTracingAccelerationStructure(rayTracingShader, nameID, rayTracingAccelerationStructure);
        public void SetRayTracingBufferParam(RayTracingShader rayTracingShader, string name, ComputeBuffer buffer) => m_wrapped.SetRayTracingBufferParam(rayTracingShader, name, buffer);
        public void SetRayTracingBufferParam(RayTracingShader rayTracingShader, int nameID, ComputeBuffer buffer) => m_wrapped.SetRayTracingBufferParam(rayTracingShader, nameID, buffer);
        public void SetRayTracingConstantBufferParam(RayTracingShader rayTracingShader, int nameID, ComputeBuffer buffer, int offset, int size) => m_wrapped.SetRayTracingConstantBufferParam(rayTracingShader, nameID, buffer, offset, size);
        public void SetRayTracingConstantBufferParam(RayTracingShader rayTracingShader, string name, ComputeBuffer buffer, int offset, int size) => m_wrapped.SetRayTracingConstantBufferParam(rayTracingShader, name, buffer, offset, size);
        public void SetRayTracingConstantBufferParam(RayTracingShader rayTracingShader, int nameID, GraphicsBuffer buffer, int offset, int size) => m_wrapped.SetRayTracingConstantBufferParam(rayTracingShader, nameID, buffer, offset, size);
        public void SetRayTracingConstantBufferParam(RayTracingShader rayTracingShader, string name, GraphicsBuffer buffer, int offset, int size) => m_wrapped.SetRayTracingConstantBufferParam(rayTracingShader, name, buffer, offset, size);
        public void SetRayTracingTextureParam(RayTracingShader rayTracingShader, string name, RenderTargetIdentifier rt) => m_wrapped.SetRayTracingTextureParam(rayTracingShader, name, rt);
        public void SetRayTracingTextureParam(RayTracingShader rayTracingShader, int nameID, RenderTargetIdentifier rt) => m_wrapped.SetRayTracingTextureParam(rayTracingShader, nameID, rt);
        public void SetRayTracingFloatParam(RayTracingShader rayTracingShader, string name, float val) => m_wrapped.SetRayTracingFloatParam(rayTracingShader, name, val);
        public void SetRayTracingFloatParam(RayTracingShader rayTracingShader, int nameID, float val) => m_wrapped.SetRayTracingFloatParam(rayTracingShader, nameID, val);
        public void SetRayTracingFloatParams(RayTracingShader rayTracingShader, string name, params float[] values) => m_wrapped.SetRayTracingFloatParams(rayTracingShader, name, values);
        public void SetRayTracingFloatParams(RayTracingShader rayTracingShader, int nameID, params float[] values) => m_wrapped.SetRayTracingFloatParams(rayTracingShader, nameID, values);
        public void SetRayTracingIntParam(RayTracingShader rayTracingShader, string name, int val) => m_wrapped.SetRayTracingIntParam(rayTracingShader, name, val);
        public void SetRayTracingIntParam(RayTracingShader rayTracingShader, int nameID, int val) => m_wrapped.SetRayTracingIntParam(rayTracingShader, nameID, val);
        public void SetRayTracingIntParams(RayTracingShader rayTracingShader, string name, params int[] values) => m_wrapped.SetRayTracingIntParams(rayTracingShader, name, values);
        public void SetRayTracingIntParams(RayTracingShader rayTracingShader, int nameID, params int[] values) => m_wrapped.SetRayTracingIntParams(rayTracingShader, nameID, values);
        public void SetRayTracingVectorParam(RayTracingShader rayTracingShader, string name, Vector4 val) => m_wrapped.SetRayTracingVectorParam(rayTracingShader, name, val);
        public void SetRayTracingVectorParam(RayTracingShader rayTracingShader, int nameID, Vector4 val) => m_wrapped.SetRayTracingVectorParam(rayTracingShader, nameID, val);
        public void SetRayTracingVectorArrayParam(RayTracingShader rayTracingShader, string name, params Vector4[] values) => m_wrapped.SetRayTracingVectorArrayParam(rayTracingShader, name, values);
        public void SetRayTracingVectorArrayParam(RayTracingShader rayTracingShader, int nameID, params Vector4[] values) => m_wrapped.SetRayTracingVectorArrayParam(rayTracingShader, nameID, values);
        public void SetRayTracingMatrixParam(RayTracingShader rayTracingShader, string name, Matrix4x4 val) => m_wrapped.SetRayTracingMatrixParam(rayTracingShader, name, val);
        public void SetRayTracingMatrixParam(RayTracingShader rayTracingShader, int nameID, Matrix4x4 val) => m_wrapped.SetRayTracingMatrixParam(rayTracingShader, nameID, val);
        public void SetRayTracingMatrixArrayParam(RayTracingShader rayTracingShader, string name, params Matrix4x4[] values) => m_wrapped.SetRayTracingMatrixArrayParam(rayTracingShader, name, values);
        public void SetRayTracingMatrixArrayParam(RayTracingShader rayTracingShader, int nameID, params Matrix4x4[] values) => m_wrapped.SetRayTracingMatrixArrayParam(rayTracingShader, nameID, values);
        public void DispatchRays(RayTracingShader rayTracingShader, string rayGenName, UInt32 width, UInt32 height, UInt32 depth, Camera camera = null) => m_wrapped.DispatchRays(rayTracingShader, rayGenName, width, height, depth, camera = null);
        public void SetRayTracingShaderPass([NotNull] RayTracingShader rayTracingShader, string passName) => m_wrapped.SetRayTracingShaderPass(rayTracingShader, passName);

        // Set buffer data essentially a buffer copy on the gpu, valid from compute queue
        public void SetBufferData(ComputeBuffer buffer, Array data) => m_wrapped.SetBufferData(buffer, data);
        public void SetBufferData<T>(ComputeBuffer buffer, List<T> data) where T : struct => m_wrapped.SetBufferData<T>(buffer, data);
        public void SetBufferData<T>(ComputeBuffer buffer, Unity.Collections.NativeArray<T> data) where T : struct => m_wrapped.SetBufferData<T>(buffer, data);
        public void SetBufferData(ComputeBuffer buffer, Array data, int managedBufferStartIndex, int graphicsBufferStartIndex, int count) => m_wrapped.SetBufferData(buffer, data, managedBufferStartIndex, graphicsBufferStartIndex, count);
        public void SetBufferData<T>(ComputeBuffer buffer, List<T> data, int managedBufferStartIndex, int graphicsBufferStartIndex, int count) where T : struct => m_wrapped.SetBufferData<T>(buffer, data, managedBufferStartIndex, graphicsBufferStartIndex, count);
        public void SetBufferData<T>(ComputeBuffer buffer, Unity.Collections.NativeArray<T> data, int nativeBufferStartIndex, int graphicsBufferStartIndex, int count) where T : struct => m_wrapped.SetBufferData<T>(buffer, data, nativeBufferStartIndex, graphicsBufferStartIndex, count);
        public void SetBufferCounterValue(ComputeBuffer buffer, uint counterValue) => m_wrapped.SetBufferCounterValue(buffer, counterValue);

        // Make it extra confusing for users by having two buffer types
        public void SetBufferData(GraphicsBuffer buffer, Array data) => m_wrapped.SetBufferData(buffer, data);
        public void SetBufferData<T>(GraphicsBuffer buffer, List<T> data) where T : struct => m_wrapped.SetBufferData<T>(buffer, data);
        public void SetBufferData<T>(GraphicsBuffer buffer, Unity.Collections.NativeArray<T> data) where T : struct => m_wrapped.SetBufferData<T>(buffer, data);
        public void SetBufferData(GraphicsBuffer buffer, Array data, int managedBufferStartIndex, int graphicsBufferStartIndex, int count) => m_wrapped.SetBufferData(buffer, data, managedBufferStartIndex, graphicsBufferStartIndex, count);
        public void SetBufferData<T>(GraphicsBuffer buffer, List<T> data, int managedBufferStartIndex, int graphicsBufferStartIndex, int count) where T : struct => m_wrapped.SetBufferData<T>(buffer, data, managedBufferStartIndex, graphicsBufferStartIndex, count);
        public void SetBufferData<T>(GraphicsBuffer buffer, Unity.Collections.NativeArray<T> data, int nativeBufferStartIndex, int graphicsBufferStartIndex, int count) where T : struct => m_wrapped.SetBufferData<T>(buffer, data, nativeBufferStartIndex, graphicsBufferStartIndex, count);
        public void SetBufferCounterValue(GraphicsBuffer buffer, uint counterValue) => m_wrapped.SetBufferCounterValue(buffer, counterValue);

        // Counter copy, essentially a copybuffer on GPU it seems
        public void CopyCounterValue(ComputeBuffer src, ComputeBuffer dst, uint dstOffsetBytes) => m_wrapped.CopyCounterValue(src, dst, dstOffsetBytes);
        public void CopyCounterValue(GraphicsBuffer src, ComputeBuffer dst, uint dstOffsetBytes) => m_wrapped.CopyCounterValue(src, dst, dstOffsetBytes);
        public void CopyCounterValue(ComputeBuffer src, GraphicsBuffer dst, uint dstOffsetBytes) => m_wrapped.CopyCounterValue(src, dst, dstOffsetBytes);
        public void CopyCounterValue(GraphicsBuffer src, GraphicsBuffer dst, uint dstOffsetBytes) => m_wrapped.CopyCounterValue(src, dst, dstOffsetBytes);
    }
}
