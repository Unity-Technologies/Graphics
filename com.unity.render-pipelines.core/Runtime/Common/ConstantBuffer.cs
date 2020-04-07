using System;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Constant Buffer management class.
    /// </summary>
    /// <typeparam name="CBType">Type of the structure that represent the constant buffer data.</typeparam>
    public class ConstantBuffer<CBType> where CBType : struct
    {
        CBType[]                        m_Data = new CBType[1]; // Array is required by the ComputeBuffer SetData API
        ComputeBuffer                   m_GPUConstantBuffer = null;
        static ConstantBuffer<CBType>   m_TypedConstantBuffer;

        ConstantBuffer()
        {
            m_GPUConstantBuffer = new ComputeBuffer(1, UnsafeUtility.SizeOf<CBType>(), ComputeBufferType.Constant);
        }

        void UpdateDataInternal(CommandBuffer cmd, in CBType data)
        {
            m_Data[0] = data;
            cmd.SetComputeBufferData(m_GPUConstantBuffer, m_Data);
        }

        void SetGlobalInternal(CommandBuffer cmd, int shaderId)
        {
            cmd.SetGlobalConstantBuffer(m_GPUConstantBuffer, shaderId, 0, m_GPUConstantBuffer.stride);
        }

        void SetInternal(CommandBuffer cmd, ComputeShader cs, int shaderId)
        {
            cmd.SetComputeConstantBufferParam(cs, shaderId, m_GPUConstantBuffer, 0, m_GPUConstantBuffer.stride);
        }

        void SetInternal(Material mat, int shaderId)
        {
            // This isn't done via command buffer because as long as the buffer itself is not destroyed,
            // the binding stays valid. Only the commit of data needs to go through the command buffer.
            // We do it here anyway for now to simplify user API.
            mat.SetConstantBuffer(shaderId, m_GPUConstantBuffer, 0, m_GPUConstantBuffer.stride);
        }

        void ReleaseInternal()
        {
            CoreUtils.SafeRelease(m_GPUConstantBuffer);
        }

        /// <summary>
        /// Allocates GPU resources for this type of constant buffer.
        /// This needs to be called once before using the constant buffer.
        /// </summary>
        public static void Allocate()
        {
            if (m_TypedConstantBuffer != null)
                throw new InvalidOperationException($"Constant Buffer {m_TypedConstantBuffer.GetType()} was already allocated");

            m_TypedConstantBuffer = new ConstantBuffer<CBType>();
        }

        /// <summary>
        /// Release GPU resources for this type of constant buffer.
        /// </summary>
        public static void Release()
        {
            if (m_TypedConstantBuffer == null)
                throw new InvalidOperationException($"Constant Buffer of type {typeof(CBType)} was never allocated");

            m_TypedConstantBuffer.ReleaseInternal();
            m_TypedConstantBuffer = null;
        }

        /// <summary>
        /// Update the GPU data of the constant buffer and bind it globally.
        /// </summary>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="data">Input data of the constant buffer.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public static void PushGlobal(CommandBuffer cmd, in CBType data, int shaderId)
        {
            if (m_TypedConstantBuffer == null)
                throw new InvalidOperationException($"Constant Buffer of type {typeof(CBType)} was never allocated");

            m_TypedConstantBuffer.UpdateDataInternal(cmd, data);
            m_TypedConstantBuffer.SetGlobalInternal(cmd, shaderId);
        }

        /// <summary>
        /// Update the GPU data of the constant buffer and bind it to a compute shader.
        /// </summary>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="data">Input data of the constant buffer.</param>
        /// <param name="cs">Compute shader to which the constant buffer should be bound.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public static void Push(CommandBuffer cmd, in CBType data, ComputeShader cs, int shaderId)
        {
            if (m_TypedConstantBuffer == null)
                throw new InvalidOperationException($"Constant Buffer of type {typeof(CBType)} was never allocated");

            m_TypedConstantBuffer.UpdateDataInternal(cmd, data);
            m_TypedConstantBuffer.SetInternal(cmd, cs, shaderId);
        }

        /// <summary>
        /// Update the GPU data of the constant buffer and bind it to a material.
        /// </summary>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="data">Input data of the constant buffer.</param>
        /// <param name="mat">Material to which the constant buffer should be bound.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public static void Push(CommandBuffer cmd, in CBType data, Material mat, int shaderId)
        {
            if (m_TypedConstantBuffer == null)
                throw new InvalidOperationException($"Constant Buffer of type {typeof(CBType)} was never allocated");

            m_TypedConstantBuffer.UpdateDataInternal(cmd, data);
            m_TypedConstantBuffer.SetInternal(mat, shaderId);
        }

        /// <summary>
        /// Update the GPU data of the constant buffer.
        /// </summary>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="data">Input data of the constant buffer.</param>
        public static void UpdateData(CommandBuffer cmd, in CBType data)
        {
            if (m_TypedConstantBuffer == null)
                throw new InvalidOperationException($"Constant Buffer of type {typeof(CBType)} was never allocated");

            m_TypedConstantBuffer.UpdateDataInternal(cmd, data);
        }

        /// <summary>
        /// Bind the constant buffer globally.
        /// </summary>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public static void SetGlobal(CommandBuffer cmd, int shaderId)
        {
            if (m_TypedConstantBuffer == null)
                throw new InvalidOperationException($"Constant Buffer of type {typeof(CBType)} was never allocated");

            m_TypedConstantBuffer.SetGlobalInternal(cmd, shaderId);
        }

        /// <summary>
        /// Bind the constant buffer to a compute shader.
        /// </summary>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="cs">Compute shader to which the constant buffer should be bound.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public static void Set(CommandBuffer cmd, ComputeShader cs, int shaderId)
        {
            if (m_TypedConstantBuffer == null)
                throw new InvalidOperationException($"Constant Buffer of type {typeof(CBType)} was never allocated");

            m_TypedConstantBuffer.SetInternal(cmd, cs, shaderId);
        }

        /// <summary>
        /// Bind the constant buffer to a material.
        /// </summary>
        /// <param name="mat">Material to which the constant buffer should be bound.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public static void Set(Material mat, int shaderId)
        {
            if (m_TypedConstantBuffer == null)
                throw new InvalidOperationException($"Constant Buffer of type {typeof(CBType)} was never allocated");

            m_TypedConstantBuffer.SetInternal(mat, shaderId);
        }
    }
}
