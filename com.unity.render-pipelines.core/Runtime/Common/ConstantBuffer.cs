using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Constant Buffer management class.
    /// </summary>
    public class ConstantBuffer
    {
        static List<ConstantBufferBase> m_RegisteredConstantBuffers = new List<ConstantBufferBase>();

        /// <summary>
        /// Update the GPU data of the constant buffer and bind it globally.
        /// </summary>
        /// <typeparam name="CBType">The type of structure representing the constant buffer data.</typeparam>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="data">Input data of the constant buffer.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public static void PushGlobal<CBType>(CommandBuffer cmd, in CBType data, int shaderId) where CBType : struct
        {
            var cb = ConstantBufferSingleton<CBType>.instance;

            cb.UpdateData(cmd, data);
            cb.SetGlobal(cmd, shaderId);
        }

        /// <summary>
        /// Update the GPU data of the constant buffer and bind it to a compute shader.
        /// </summary>
        /// <typeparam name="CBType">The type of structure representing the constant buffer data.</typeparam>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="data">Input data of the constant buffer.</param>
        /// <param name="cs">Compute shader to which the constant buffer should be bound.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public static void Push<CBType>(CommandBuffer cmd, in CBType data, ComputeShader cs, int shaderId) where CBType : struct
        {
            var cb = ConstantBufferSingleton<CBType>.instance;

            cb.UpdateData(cmd, data);
            cb.Set(cmd, cs, shaderId);
        }

        /// <summary>
        /// Update the GPU data of the constant buffer and bind it to a material.
        /// </summary>
        /// <typeparam name="CBType">The type of structure representing the constant buffer data.</typeparam>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="data">Input data of the constant buffer.</param>
        /// <param name="mat">Material to which the constant buffer should be bound.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public static void Push<CBType>(CommandBuffer cmd, in CBType data, Material mat, int shaderId) where CBType : struct
        {
            var cb = ConstantBufferSingleton<CBType>.instance;

            cb.UpdateData(cmd, data);
            cb.Set(mat, shaderId);
        }

        /// <summary>
        /// Update the GPU data of the constant buffer.
        /// </summary>
        /// <typeparam name="CBType">The type of structure representing the constant buffer data.</typeparam>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="data">Input data of the constant buffer.</param>
        public static void UpdateData<CBType>(CommandBuffer cmd, in CBType data) where CBType : struct
        {
            var cb = ConstantBufferSingleton<CBType>.instance;

            cb.UpdateData(cmd, data);
        }

        /// <summary>
        /// Bind the constant buffer globally.
        /// </summary>
        /// <typeparam name="CBType">The type of structure representing the constant buffer data.</typeparam>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public static void SetGlobal<CBType>(CommandBuffer cmd, int shaderId) where CBType : struct
        {
            var cb = ConstantBufferSingleton<CBType>.instance;

            cb.SetGlobal(cmd, shaderId);
        }

        /// <summary>
        /// Bind the constant buffer to a compute shader.
        /// </summary>
        /// <typeparam name="CBType">The type of structure representing the constant buffer data.</typeparam>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="cs">Compute shader to which the constant buffer should be bound.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public static void Set<CBType>(CommandBuffer cmd, ComputeShader cs, int shaderId) where CBType : struct
        {
            var cb = ConstantBufferSingleton<CBType>.instance;

            cb.Set(cmd, cs, shaderId);
        }

        /// <summary>
        /// Bind the constant buffer to a material.
        /// </summary>
        /// <typeparam name="CBType">The type of structure representing the constant buffer data.</typeparam>
        /// <param name="mat">Material to which the constant buffer should be bound.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public static void Set<CBType>(Material mat, int shaderId) where CBType : struct
        {
            var cb = ConstantBufferSingleton<CBType>.instance;

            cb.Set(mat, shaderId);
        }

        /// <summary>
        /// Release all currently allocated singleton constant buffers.
        /// This needs to be called before shutting down the application.
        /// </summary>
        public static void ReleaseAll()
        {
            foreach (var cb in m_RegisteredConstantBuffers)
                cb.Release();

            m_RegisteredConstantBuffers.Clear();
        }

        internal static void Register(ConstantBufferBase cb)
        {
            m_RegisteredConstantBuffers.Add(cb);
        }
    }

    /// <summary>
    /// The base class of Constant Buffer.
    /// </summary>
    public abstract class ConstantBufferBase
    {
        /// <summary>
        /// Release the constant buffer.
        /// </summary>
        public abstract void Release();
    }


    /// <summary>
    /// An instance of a constant buffer.
    /// </summary>
    /// <typeparam name="CBType">The type of structure representing the constant buffer data.</typeparam>
    public class ConstantBuffer<CBType> : ConstantBufferBase where CBType : struct
    {
        // Used to track all global bindings used by this CB type.
        HashSet<int> m_GlobalBindings = new HashSet<int>();
        // Array is required by the ComputeBuffer SetData API
        CBType[] m_Data = new CBType[1];


        ComputeBuffer m_GPUConstantBuffer = null;

        /// <summary>
        /// Constant Buffer constructor.
        /// </summary>
        public ConstantBuffer()
        {
            m_GPUConstantBuffer = new ComputeBuffer(1, UnsafeUtility.SizeOf<CBType>(), ComputeBufferType.Constant);
        }

        /// <summary>
        /// Update the GPU data of the constant buffer.
        /// </summary>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="data">Input data of the constant buffer.</param>
        public void UpdateData(CommandBuffer cmd, in CBType data)
        {
            m_Data[0] = data;
#if UNITY_2021_1_OR_NEWER
            cmd.SetBufferData(m_GPUConstantBuffer, m_Data);
#else
            cmd.SetComputeBufferData(m_GPUConstantBuffer, m_Data);
#endif
        }

        /// <summary>
        /// Bind the constant buffer globally.
        /// </summary>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public void SetGlobal(CommandBuffer cmd, int shaderId)
        {
            m_GlobalBindings.Add(shaderId);
            cmd.SetGlobalConstantBuffer(m_GPUConstantBuffer, shaderId, 0, m_GPUConstantBuffer.stride);
        }

        /// <summary>
        /// Bind the constant buffer to a compute shader.
        /// </summary>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="cs">Compute shader to which the constant buffer should be bound.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public void Set(CommandBuffer cmd, ComputeShader cs, int shaderId)
        {
            cmd.SetComputeConstantBufferParam(cs, shaderId, m_GPUConstantBuffer, 0, m_GPUConstantBuffer.stride);
        }

        /// <summary>
        /// Bind the constant buffer to a material.
        /// </summary>
        /// <param name="mat">Material to which the constant buffer should be bound.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public void Set(Material mat, int shaderId)
        {
            // This isn't done via command buffer because as long as the buffer itself is not destroyed,
            // the binding stays valid. Only the commit of data needs to go through the command buffer.
            // We do it here anyway for now to simplify user API.
            mat.SetConstantBuffer(shaderId, m_GPUConstantBuffer, 0, m_GPUConstantBuffer.stride);
        }

        /// <summary>
        /// Update the GPU data of the constant buffer and bind it globally.
        /// </summary>
        /// <param name="cmd">Command Buffer used to execute the graphic commands.</param>
        /// <param name="data">Input data of the constant buffer.</param>
        /// <param name="shaderId">Shader porperty id to bind the constant buffer to.</param>
        public void PushGlobal(CommandBuffer cmd, in CBType data, int shaderId)
        {
            UpdateData(cmd, data);
            SetGlobal(cmd, shaderId);
        }

        /// <summary>
        /// Release the constant buffers.
        /// </summary>
        public override void Release()
        {
            // Depending on the device, globally bound buffers can leave stale "valid" shader ids pointing to a destroyed buffer.
            // In DX11 it does not cause issues but on Vulkan this will result in skipped drawcalls (even if the buffer is not actually accessed in the shader).
            // To avoid this kind of issues, it's good practice to "unbind" all globally bound buffers upon destruction.
            foreach (int shaderId in m_GlobalBindings)
                Shader.SetGlobalConstantBuffer(shaderId, (ComputeBuffer)null, 0, 0);
            m_GlobalBindings.Clear();

            CoreUtils.SafeRelease(m_GPUConstantBuffer);
        }
    }

    class ConstantBufferSingleton<CBType> : ConstantBuffer<CBType> where CBType : struct
    {
        static ConstantBufferSingleton<CBType> s_Instance = null;
        internal static ConstantBufferSingleton<CBType> instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new ConstantBufferSingleton<CBType>();
                    ConstantBuffer.Register(s_Instance);
                }
                return s_Instance;
            }
            set
            {
                s_Instance = value;
            }
        }

        public override void Release()
        {
            base.Release();
            s_Instance = null;
        }
    }
}
