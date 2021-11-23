using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        [GenerateHLSL(PackingRules.Exact, false)]
        struct ParticleDescriptor
        {
            public Vector3 position;
            public float size;
            public Vector3 color;
            public float padding;
            public Vector3 velocity;
            public float weight;
        }

        [GenerateHLSL(PackingRules.Exact, false)]
        struct ParticleAABB
        {
            public Vector3 minV;
            public Vector3 maxV;
        }

        ComputeBuffer m_ParticleBuffer0;
        ComputeBuffer m_ParticleBuffer1;
        GraphicsBuffer m_AABBBuffer;
        ComputeShader m_ParticleSystemCS;
        Material m_ParticleMaterial;

        readonly int m_MaxParticleCount = 1000000;
        int m_ParticleSystemFrame;

        void InitializeParticleSystem()
        {
            // Allocate the buffers
            m_ParticleBuffer0 = new ComputeBuffer(m_MaxParticleCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ParticleDescriptor)));
            m_ParticleBuffer1 = new ComputeBuffer(m_MaxParticleCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ParticleDescriptor)));
            m_AABBBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, m_MaxParticleCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ParticleAABB)));
            m_ParticleSystemCS = m_Asset.renderPipelineResources.shaders.particleSystemCS;
            m_ParticleSystemFrame = 0;
            m_ParticleMaterial = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.rayTracingParticlePS);

        }

        void ReleaseParticleSystem()
        {
            CoreUtils.Destroy(m_ParticleMaterial);
            CoreUtils.SafeRelease(m_ParticleBuffer0);
            CoreUtils.SafeRelease(m_ParticleBuffer1);
            m_AABBBuffer.Release();
        }

        void UpdateParticleSystem(CommandBuffer cmd)
        {
            int fullTileSize = (m_MaxParticleCount + 127) / 128;

            // Initialize the particle system if needed
            if (m_ParticleSystemFrame == 0)
            {
                cmd.SetComputeBufferParam(m_ParticleSystemCS, 0, "_ParticleBufferOut", m_ParticleBuffer0);
                cmd.DispatchCompute(m_ParticleSystemCS, 0, fullTileSize, 1, 1);
            }

            // Define the input and output buffers
            ComputeBuffer bIn = ((m_ParticleSystemFrame & 1) == 0 ? m_ParticleBuffer0 : m_ParticleBuffer1);
            ComputeBuffer bOut = ((m_ParticleSystemFrame & 1) == 0 ? m_ParticleBuffer1 : m_ParticleBuffer0);

            // Update the particle system
            cmd.SetComputeFloatParam(m_ParticleSystemCS, "_DeltaTime", Time.deltaTime);
            cmd.SetComputeBufferParam(m_ParticleSystemCS, 1, "_ParticleBufferIn", bIn);
            cmd.SetComputeBufferParam(m_ParticleSystemCS, 1, "_ParticleBufferOut", bOut);
            cmd.DispatchCompute(m_ParticleSystemCS, 1, fullTileSize, 1, 1);

            // Update the AABBs
            cmd.SetComputeBufferParam(m_ParticleSystemCS, 2, "_ParticleBufferIn", bOut);
            cmd.SetComputeBufferParam(m_ParticleSystemCS, 2, "_AABBBuffer", m_AABBBuffer);
            cmd.DispatchCompute(m_ParticleSystemCS, 2, fullTileSize, 1, 1);

            // Make sure to assign to the blablal
            Shader.SetGlobalBuffer("_AABBBuffer", m_AABBBuffer);
            Shader.SetGlobalBuffer("_ParticleBuffer", bOut);

            // Reverse the order for next frame
            m_ParticleSystemFrame++;
        }
    }
}
