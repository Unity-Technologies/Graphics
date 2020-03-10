using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering
{
    public class ConstantBuffer<CBType> where CBType : struct
    {
        CBType[]        m_Data = new CBType[1]; // Array is required by the ComputeBuffer SetData API
        ComputeBuffer   m_GPUConstantBuffer = null;

        public ref CBType data => ref m_Data[0];

        public ConstantBuffer()
        {
            m_GPUConstantBuffer = new ComputeBuffer(1, UnsafeUtility.SizeOf<CBType>(), ComputeBufferType.Constant);
        }

        public void Commit(CommandBuffer cmd)
        {
            cmd.SetComputeBufferData(m_GPUConstantBuffer, m_Data);
        }

        public void PushGlobal(CommandBuffer cmd, int shaderId, bool commitData = false)
        {
            if (commitData)
                Commit(cmd);
            cmd.SetGlobalConstantBuffer(m_GPUConstantBuffer, shaderId, 0, m_GPUConstantBuffer.stride);
        }

        public void Push(CommandBuffer cmd, ComputeShader cs, int shaderId, bool commitData = false)
        {
            if (commitData)
                Commit(cmd);
            cmd.SetComputeConstantBufferParam(cs, shaderId, m_GPUConstantBuffer, 0, m_GPUConstantBuffer.stride);
        }

        public void Push(CommandBuffer cmd, Material mat, int shaderId, bool commitData = false)
        {
            if (commitData)
                Commit(cmd);
            // This isn't done via command buffer because as long as the buffer itself is not destroyed,
            // the binding stays valid. Only the commit of data needs to go through the command buffer.
            // We do it here anyway for now to simplify user API.
            mat.SetConstantBuffer(shaderId, m_GPUConstantBuffer, 0, m_GPUConstantBuffer.stride);
        }

        public void Release()
        {
            CoreUtils.SafeRelease(m_GPUConstantBuffer);
        }
    }
}
