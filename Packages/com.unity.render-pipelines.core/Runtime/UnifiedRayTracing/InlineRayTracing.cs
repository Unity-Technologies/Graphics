#if INCLUDE_UNIFIED_RAYTRACING_COMPUTE_BACKEND || UNITY_EDITOR
#define INCLUDE_COMPUTE_BACKEND
#endif
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.UnifiedRayTracing
{

    internal static class InlineRayTracing
    {

        public static void SetTraceScratchBuffer(CommandBuffer cmd, ComputeShader shader, int kernelIndex, GraphicsBuffer traceScratchBuffer)
        {
            if (traceScratchBuffer != null)
                cmd.SetComputeBufferParam(shader, kernelIndex, SID._UnifiedRT_Stack, traceScratchBuffer);
        }

        public static void SetAccelerationStructure(CommandBuffer cmd, ComputeShader shader, int kernelIndex, string name, IRayTracingAccelStruct accelStruct)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));
            Utils.CheckArgIsNotNull(shader, nameof(shader));
            Utils.CheckArgIsNotNull(accelStruct, nameof(accelStruct));

            if (accelStruct is HardwareRayTracingAccelStruct hwAccelStruct)
            {
                cmd.SetRayTracingAccelerationStructure(shader, kernelIndex, Shader.PropertyToID(name + "accelStruct"), hwAccelStruct.accelStruct);
            }
            else if (accelStruct is ComputeRayTracingAccelStruct computeAccelStruct)
            {
                computeAccelStruct.Bind(cmd, shader, kernelIndex, name);
            }
            else
                Utils.CheckArg(false, $"Unsupported {nameof(IRayTracingAccelStruct)} implementation: '{accelStruct.GetType().Name}'.");
        }

        public static void SetKeywords(CommandBuffer cmd, ComputeShader computeShader, BackendShaderKeywords shaderKeywords, RayTracingBackend backend)
        {
            cmd.SetKeyword(computeShader, shaderKeywords.m_ComputeBackendKeyword, backend == RayTracingBackend.Compute);
            cmd.SetKeyword(computeShader, shaderKeywords.m_HardwareBackendKeyword, backend == RayTracingBackend.Hardware);
        }

        public static void SetKeywords(ComputeShader computeShader, BackendShaderKeywords shaderKeywords, RayTracingBackend backend)
        {
            computeShader.SetKeyword(shaderKeywords.m_ComputeBackendKeyword, backend == RayTracingBackend.Compute);
            computeShader.SetKeyword(shaderKeywords.m_HardwareBackendKeyword, backend == RayTracingBackend.Hardware);
        }

    }

    internal class BackendShaderKeywords
    {

        public BackendShaderKeywords(ComputeShader shader)
        {
            m_ComputeBackendKeyword = new LocalKeyword(shader, "UNIFIED_RT_BACKEND_COMPUTE");
            m_HardwareBackendKeyword = new LocalKeyword(shader, "UNIFIED_RT_BACKEND_HARDWARE");
        }

        internal LocalKeyword m_ComputeBackendKeyword;
        internal LocalKeyword m_HardwareBackendKeyword;
    }
}
