using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal class HardwareRayTracingShader : IRayTracingShader
    {
        readonly RayTracingShader m_Shader;
        readonly string m_ShaderDispatchFuncName;

        internal HardwareRayTracingShader(RayTracingShader shader, string dispatchFuncName, GraphicsBuffer unused)
        {
            m_Shader = shader;
            m_ShaderDispatchFuncName = dispatchFuncName;
        }

        public uint3 GetThreadGroupSizes()
        {
            return new uint3(1, 1, 1);
        }

        public void SetAccelerationStructure(CommandBuffer cmd, string name, IRayTracingAccelStruct accelStruct)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));
            Utils.CheckArgIsNotNull(accelStruct, nameof(accelStruct));

            cmd.SetRayTracingShaderPass(m_Shader, "RayTracing");

            var hwAccelStruct = accelStruct as HardwareRayTracingAccelStruct;
            Debug.Assert(hwAccelStruct != null);

            cmd.SetRayTracingAccelerationStructure(m_Shader, Shader.PropertyToID(name+"accelStruct"), hwAccelStruct.accelStruct);
        }

        public void SetIntParam(CommandBuffer cmd, int nameID, int val)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            cmd.SetRayTracingIntParam(m_Shader, nameID, val);
        }

        public void SetFloatParam(CommandBuffer cmd, int nameID, float val)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            cmd.SetRayTracingFloatParam(m_Shader, nameID, val);
        }

        public void SetVectorParam(CommandBuffer cmd, int nameID, Vector4 val)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            cmd.SetRayTracingVectorParam(m_Shader, nameID, val);
        }

        public void SetMatrixParam(CommandBuffer cmd, int nameID, Matrix4x4 val)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            cmd.SetRayTracingMatrixParam(m_Shader, nameID, val);
        }

        public void SetTextureParam(CommandBuffer cmd, int nameID, RenderTargetIdentifier rt)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            cmd.SetRayTracingTextureParam(m_Shader, nameID, rt);
        }

        public void SetBufferParam(CommandBuffer cmd, int nameID, GraphicsBuffer buffer)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));
            Utils.CheckArgIsNotNull(buffer, nameof(buffer));

            cmd.SetRayTracingBufferParam(m_Shader, nameID, buffer);
        }

        public void SetBufferParam(CommandBuffer cmd, int nameID, ComputeBuffer buffer)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));
            Utils.CheckArgIsNotNull(buffer, nameof(buffer));

            cmd.SetRayTracingBufferParam(m_Shader, nameID, buffer);
        }

        public void SetConstantBufferParam(CommandBuffer cmd, int nameID, GraphicsBuffer buffer, int offset, int size)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));
            Utils.CheckArgIsNotNull(buffer, nameof(buffer));

            cmd.SetRayTracingConstantBufferParam(m_Shader, nameID, buffer, offset, size);
        }

        public void SetConstantBufferParam(CommandBuffer cmd, int nameID, ComputeBuffer buffer, int offset, int size)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));
            Utils.CheckArgIsNotNull(buffer, nameof(buffer));

            cmd.SetRayTracingConstantBufferParam(m_Shader, nameID, buffer, offset, size);
        }

        public void Dispatch(CommandBuffer cmd, GraphicsBuffer scratchBuffer, uint width, uint height, uint depth)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            cmd.DispatchRays(m_Shader, m_ShaderDispatchFuncName, width, height, depth, null);
        }

        public void Dispatch(CommandBuffer cmd, GraphicsBuffer scratchBuffer, GraphicsBuffer argsBuffer)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));
            Utils.CheckArgIsNotNull(argsBuffer, nameof(argsBuffer));
            GraphicsBuffer.Target requiredFlags = GraphicsBuffer.Target.IndirectArguments | GraphicsBuffer.Target.Structured;
            Utils.CheckArg((argsBuffer.target & requiredFlags) == requiredFlags, "argsBuffer.target must have both Target.IndirectArguments and Target.Structured set");

            cmd.DispatchRays(m_Shader, m_ShaderDispatchFuncName, argsBuffer, 0);
        }
        public ulong GetTraceScratchBufferRequiredSizeInBytes(uint width, uint height, uint depth)
        {
            return 0;
        }

    }
}


