using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal class ComputeRayTracingShader : IRayTracingShader
    {
        readonly ComputeShader m_Shader;
        readonly int m_KernelIndex;
        readonly int m_ComputeIndirectDispatchDimsKernelIndex;
        uint3 m_ThreadGroupSizes;

        // Temp buffer containing the dispatch dimensions
        // For standard dispatches, it contains the dims counted in work groups.
        // For indirect dispatches, it contains the dims counted in threads.

        readonly GraphicsBuffer m_DispatchBuffer;

        internal ComputeRayTracingShader(ComputeShader shader, string dispatchFuncName, GraphicsBuffer dispatchBuffer)
        {
            m_Shader = shader;
            m_KernelIndex = m_Shader.FindKernel(dispatchFuncName);
            m_ComputeIndirectDispatchDimsKernelIndex = m_Shader.FindKernel("ComputeIndirectDispatchDims");
            Debug.Assert(m_Shader.IsSupported(m_KernelIndex), $"Invalid compute shader [{shader.name}], please check that your shader code is compiling.");

            m_Shader.GetKernelThreadGroupSizes(m_KernelIndex,
                out m_ThreadGroupSizes.x, out m_ThreadGroupSizes.y, out m_ThreadGroupSizes.z);
            m_DispatchBuffer = dispatchBuffer;
        }

        public uint3 GetThreadGroupSizes()
        {
            return m_ThreadGroupSizes;
        }

        public void SetAccelerationStructure(CommandBuffer cmd, string name, IRayTracingAccelStruct accelStruct)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            var computeAccelStruct = accelStruct as ComputeRayTracingAccelStruct;
            Assert.IsNotNull(computeAccelStruct);

            computeAccelStruct.Bind(cmd, name, this);
        }

        public void SetIntParam(CommandBuffer cmd, int nameID, int val)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            cmd.SetComputeIntParam(m_Shader, nameID, val);
        }

        public void SetFloatParam(CommandBuffer cmd, int nameID, float val)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            cmd.SetComputeFloatParam(m_Shader, nameID, val);
        }

        public void SetVectorParam(CommandBuffer cmd, int nameID, Vector4 val)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            cmd.SetComputeVectorParam(m_Shader, nameID, val);
        }

        public void SetMatrixParam(CommandBuffer cmd, int nameID, Matrix4x4 val)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            cmd.SetComputeMatrixParam(m_Shader, nameID, val);
        }

        public void SetTextureParam(CommandBuffer cmd, int nameID, RenderTargetIdentifier rt)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            cmd.SetComputeTextureParam(m_Shader, m_KernelIndex, nameID, rt);
        }

        public void SetBufferParam(CommandBuffer cmd, int nameID, GraphicsBuffer buffer)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            cmd.SetComputeBufferParam(m_Shader, m_KernelIndex, nameID, buffer);
        }

        public void SetBufferParam(CommandBuffer cmd, int nameID, ComputeBuffer buffer)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            cmd.SetComputeBufferParam(m_Shader, m_KernelIndex, nameID, buffer);
        }

        public void SetConstantBufferParam(CommandBuffer cmd, int nameID, GraphicsBuffer buffer, int offset, int size)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));
            Utils.CheckArgIsNotNull(buffer, nameof(buffer));

            cmd.SetComputeConstantBufferParam(m_Shader, nameID, buffer, offset, size);
        }

        public void SetConstantBufferParam(CommandBuffer cmd, int nameID, ComputeBuffer buffer, int offset, int size)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));
            Utils.CheckArgIsNotNull(buffer, nameof(buffer));

            cmd.SetComputeConstantBufferParam(m_Shader, nameID, buffer, offset, size);
        }

        public void Dispatch(CommandBuffer cmd, GraphicsBuffer scratchBuffer, uint width, uint height, uint depth)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            var requiredScratchSize = GetTraceScratchBufferRequiredSizeInBytes(width, height, depth);
            if (requiredScratchSize > 0)
            {
                Utils.CheckArg(scratchBuffer != null && ((ulong)(scratchBuffer.count * scratchBuffer.stride) >= requiredScratchSize), "scratchBuffer size is too small");
                Utils.CheckArg(scratchBuffer.stride == 4, "scratchBuffer stride must be 4");
                Utils.CheckArg(scratchBuffer.target == RayTracingHelper.ScratchBufferTarget, "scratchBuffer.target must have Target.Structured set");
            }

            cmd.SetComputeBufferParam(m_Shader, m_KernelIndex, SID._UnifiedRT_Stack, scratchBuffer);
            cmd.SetBufferData(m_DispatchBuffer, new uint[] { width, height, depth });
            SetBufferParam(cmd, SID._UnifiedRT_DispatchDims, m_DispatchBuffer);

            uint workgroupsX = (uint)GraphicsHelpers.DivUp((int)width, m_ThreadGroupSizes.x);
            uint workgroupsY = (uint)GraphicsHelpers.DivUp((int)height, m_ThreadGroupSizes.y);
            uint workgroupsZ = (uint)GraphicsHelpers.DivUp((int)depth, m_ThreadGroupSizes.z);
            cmd.DispatchCompute(m_Shader, m_KernelIndex, (int)workgroupsX, (int)workgroupsY, (int)workgroupsZ);
        }

        public void Dispatch(CommandBuffer cmd, GraphicsBuffer scratchBuffer, GraphicsBuffer argsBuffer)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));
            GraphicsBuffer.Target requiredFlags = GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured;
            Utils.CheckArg((argsBuffer.target & requiredFlags) == requiredFlags, "argsBuffer.target must have both Target.IndirectArguments and Target.Structured set");

            SetIndirectDispatchDimensions(cmd, argsBuffer);
            DispatchIndirect(cmd, scratchBuffer, argsBuffer);
        }

        internal void SetIndirectDispatchDimensions(CommandBuffer cmd, GraphicsBuffer argsBuffer)
        {
            cmd.SetComputeBufferParam(m_Shader, m_ComputeIndirectDispatchDimsKernelIndex, SID._UnifiedRT_DispatchDims, argsBuffer);
            cmd.SetComputeBufferParam(m_Shader, m_ComputeIndirectDispatchDimsKernelIndex, SID._UnifiedRT_DispatchDimsInWorkgroups, m_DispatchBuffer);
            cmd.DispatchCompute(m_Shader, m_ComputeIndirectDispatchDimsKernelIndex, 1, 1, 1);
        }

        internal void DispatchIndirect(CommandBuffer cmd, GraphicsBuffer scratchBuffer, GraphicsBuffer argsBuffer)
        {
            cmd.SetComputeBufferParam(m_Shader, m_KernelIndex, SID._UnifiedRT_Stack, scratchBuffer);
            cmd.SetComputeBufferParam(m_Shader, m_KernelIndex, SID._UnifiedRT_DispatchDims, argsBuffer);
            cmd.DispatchCompute(m_Shader, m_KernelIndex, m_DispatchBuffer, 0);
        }

        public ulong GetTraceScratchBufferRequiredSizeInBytes(uint width, uint height, uint depth)
        {
            uint rayCount = width * height * depth;
            return (RadeonRays.RadeonRaysAPI.GetTraceMemoryRequirements(rayCount) * 4);
        }
    }
}


